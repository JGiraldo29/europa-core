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

using Europa.Models;
using Microsoft.Extensions.Caching.Memory;

namespace Europa.Services
{
    public class ChunkService
    {
        private readonly IMemoryCache _cache;
        private readonly IStorageProvider _storageProvider;
        private readonly string _tempContainerName = "tempuploads";
        private readonly string _finalContainerName = "encryptedfiles";
        private const int MINUTES_UNTIL_CHUNK_CLEANUP = 30;

        public ChunkService(IMemoryCache cache, IStorageProvider storageProvider)
        {
            _cache = cache;
            _storageProvider = storageProvider;
        }

        public async Task<string> InitializeUpload(string fileId, int totalChunks, long totalSize, string iv, string salt, bool isMultiFile, string expirationOption)
        {
            await _storageProvider.CreateContainerIfNotExistsAsync(_tempContainerName);
            var uploadId = Guid.NewGuid().ToString();

            _cache.Set($"upload_{uploadId}", new UploadStatus
            {
                FileId = fileId,
                TotalChunks = totalChunks,
                ReceivedChunks = new HashSet<int>(),
                TotalSize = totalSize,
                ExpiresAt = DateTime.UtcNow.AddMinutes(MINUTES_UNTIL_CHUNK_CLEANUP),
                Iv = iv,
                Salt = salt,
                IsMultiFile = isMultiFile,
                ExpirationOption = expirationOption
            }, TimeSpan.FromMinutes(MINUTES_UNTIL_CHUNK_CLEANUP));

            return uploadId;
        }

        public async Task<bool> UploadChunk(string uploadId, int chunkNumber, byte[] chunkData)
        {
            var uploadStatus = _cache.Get<UploadStatus>($"upload_{uploadId}");
            if (uploadStatus == null) return false;

            string blobPath = $"{uploadId}/{chunkNumber}";

            using (var ms = new MemoryStream(chunkData))
            {
                await _storageProvider.UploadChunkAsync(_tempContainerName, blobPath, ms);
            }

            uploadStatus.ReceivedChunks.Add(chunkNumber);
            _cache.Set($"upload_{uploadId}", uploadStatus, TimeSpan.FromMinutes(MINUTES_UNTIL_CHUNK_CLEANUP));

            return true;
        }

        public async Task<bool> FinalizeUpload(string uploadId, string fileId, int expirationDays)
        {
            var uploadStatus = _cache.Get<UploadStatus>($"upload_{uploadId}");
            if (uploadStatus == null) return false;

            if (uploadStatus.ReceivedChunks.Count != uploadStatus.TotalChunks)
                return false;

            using (var finalStream = new MemoryStream((int)uploadStatus.TotalSize))
            {
                for (int i = 0; i < uploadStatus.TotalChunks; i++)
                {
                    string chunkPath = $"{uploadId}/{i}";

                    if (!await _storageProvider.ChunkExistsAsync(_tempContainerName, chunkPath))
                    {
                        return false;
                    }

                    using (var chunkStream = await _storageProvider.DownloadChunkAsync(_tempContainerName, chunkPath))
                    {
                        await chunkStream.CopyToAsync(finalStream);
                    }
                    await _storageProvider.DeleteChunkAsync(_tempContainerName, chunkPath);
                }

                finalStream.Position = 0;
                var metadata = new Dictionary<string, string>
                {
                    { "expirationDate", DateTime.UtcNow.AddDays(expirationDays).ToString("o") },
                    { "iv", uploadStatus.Iv },
                    { "salt", uploadStatus.Salt },
                    { "isMultiFile", uploadStatus.IsMultiFile.ToString() }
                };

                await _storageProvider.UploadFinalFileAsync(_finalContainerName, fileId, finalStream, metadata);
            }

            _cache.Remove($"upload_{uploadId}");
            return true;
        }
    }
}