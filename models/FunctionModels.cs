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

namespace Europa.Models
{
    public class FileTransfer
    {
        public int Id { get; set; }
        public string FileId { get; set; }
        public string FileName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpirationDate { get; set; }
        public string ShortUrl { get; set; }
    }
    public class UploadStatus
    {
        public string FileId { get; set; }
        public int TotalChunks { get; set; }
        public HashSet<int> ReceivedChunks { get; set; }
        public long TotalSize { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string Iv { get; set; }
        public string Salt { get; set; }
        public bool IsMultiFile { get; set; }
        public string ExpirationOption { get; set; }
    }
}
