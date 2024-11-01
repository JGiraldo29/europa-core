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

namespace Europa.Services
{
    public interface IStorageProvider
    {
        Task CreateContainerIfNotExistsAsync(string containerName);
        Task UploadChunkAsync(string containerName, string blobPath, Stream content);
        Task<bool> ChunkExistsAsync(string containerName, string blobPath);
        Task<Stream> DownloadChunkAsync(string containerName, string blobPath);
        Task DeleteChunkAsync(string containerName, string blobPath);
        Task UploadFinalFileAsync(string containerName, string fileId, Stream content, IDictionary<string, string> metadata);
        Task<bool> FileExistsAsync(string containerName, string fileId);
        Task<Stream> DownloadFileAsync(string containerName, string fileId);
        Task<IDictionary<string, string>> GetFileMetadataAsync(string containerName, string fileId);
        Task UploadFileAsync(string containerName, string fileId, Stream content, IDictionary<string, string> metadata);
        Task DeleteFileAsync(string containerName, string fileId);
    }
}