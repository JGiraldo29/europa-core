/*
 * Migairu Europa - End-to-End Encrypted File Transfer
 * Copyright (C) 2024 Migairu Corp.
 * Written by Juan Miguel Giraldo.
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License along
 * with this program; if not, see <https://www.gnu.org/licenses/>.
 */

using Microsoft.Extensions.Caching.Memory;
using System.IO.Compression;
using System.Text.Json;

namespace Europa.Services
{
    public interface IFileService
    {
        Task<FileInfo> GetFileInfoAsync(string fileId);
        Task<(Stream FileStream, byte[] IV, byte[] Salt, bool IsMultiFile)> GetFileStreamAsync(string fileId);
        Task DeleteExpiredFileAsync(string fileId);
        Task<string> SaveZippedFilesAsync(List<IFormFile> files, List<string> ivs, List<string> salts, int expirationDays);
    }

    public class FileService : IFileService
    {
        private readonly IMemoryCache _cache;
        private readonly IStorageProvider _storageProvider;
        private readonly string _containerName = "encryptedfiles";

        public FileService(IMemoryCache cache, IStorageProvider storageProvider)
        {
            _cache = cache;
            _storageProvider = storageProvider;
        }

        public async Task<(Stream FileStream, byte[] IV, byte[] Salt, bool IsMultiFile)> GetFileStreamAsync(string fileId)
        {
            if (!await _storageProvider.FileExistsAsync(_containerName, fileId))
            {
                throw new FileNotFoundException();
            }

            var metadata = await _storageProvider.GetFileMetadataAsync(_containerName, fileId);
            var iv = Convert.FromBase64String(metadata["iv"]);
            var salt = Convert.FromBase64String(metadata["salt"]);
            var isMultiFile = bool.Parse(metadata["isMultiFile"]);

            var stream = await _storageProvider.DownloadFileAsync(_containerName, fileId);
            return (stream, iv, salt, isMultiFile);
        }

        public async Task<string> SaveZippedFilesAsync(List<IFormFile> files, List<string> ivs, List<string> salts, int expirationDays)
        {
            string fileId = Guid.NewGuid().ToString();

            using (var memoryStream = new MemoryStream())
            {
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    for (int i = 0; i < files.Count; i++)
                    {
                        var file = files[i];
                        var entry = archive.CreateEntry(file.FileName, CompressionLevel.Fastest);
                        using (var entryStream = entry.Open())
                        using (var fileStream = file.OpenReadStream())
                        {
                            await fileStream.CopyToAsync(entryStream);
                        }
                    }
                }

                memoryStream.Position = 0;
                var metadata = new Dictionary<string, string>
                {
                    { "expirationDate", DateTime.UtcNow.AddDays(expirationDays).ToString("o") },
                    { "ivs", JsonSerializer.Serialize(ivs) },
                    { "salts", JsonSerializer.Serialize(salts) }
                };

                await _storageProvider.UploadFileAsync(_containerName, fileId, memoryStream, metadata);
            }

            return fileId;
        }

        public async Task<FileInfo> GetFileInfoAsync(string fileId)
        {
            string cacheKey = $"FileInfo_{fileId}";
            if (!_cache.TryGetValue(cacheKey, out FileInfo fileInfo))
            {
                if (!await _storageProvider.FileExistsAsync(_containerName, fileId))
                {
                    throw new FileNotFoundException();
                }

                var metadata = await _storageProvider.GetFileMetadataAsync(_containerName, fileId);

                fileInfo = new FileInfo
                {
                    FileId = fileId,
                    ExpirationDate = DateTime.Parse(metadata["expirationDate"]),
                    IsMultiFile = bool.Parse(metadata["isMultiFile"])
                };

                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(5));

                _cache.Set(cacheKey, fileInfo, cacheEntryOptions);
            }

            return fileInfo;
        }

        public async Task DeleteExpiredFileAsync(string fileId)
        {
            await _storageProvider.DeleteFileAsync(_containerName, fileId);
            _cache.Remove($"FileInfo_{fileId}");
        }
    }
    public class FileInfo
    {
        public string FileId { get; set; }
        public DateTime ExpirationDate { get; set; }
        public bool IsMultiFile { get; set; }
    }
}