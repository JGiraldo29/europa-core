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

using Microsoft.Extensions.Caching.Distributed;
public class RateLimitingService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RateLimitingService> _logger;

    private static readonly TimeSpan _uploadWindow = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan _downloadWindow = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan _authWindow = TimeSpan.FromMinutes(15);

    public RateLimitingService(IDistributedCache cache, ILogger<RateLimitingService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<bool> ExceedsUploadLimit(string userId)
    {
        int maxUploads = 20;
        return await ExceedsLimit($"upload:{userId}", maxUploads, _uploadWindow);
    }

    public async Task<bool> ExceedsDownloadLimit(string userId)
    {
        int maxDownloads =  100;
        return await ExceedsLimit($"download:{userId}", maxDownloads, _downloadWindow);
    }

    public async Task<bool> ExceedsAuthLimit(string ipAddress)
    {
        return await ExceedsLimit($"auth:{ipAddress}", 5, _authWindow);
    }

    private async Task<bool> ExceedsLimit(string key, int maxAttempts, TimeSpan window)
    {
        try
        {
            var attempts = await _cache.GetStringAsync(key);
            int currentAttempts = string.IsNullOrEmpty(attempts) ? 0 : int.Parse(attempts);

            if (currentAttempts >= maxAttempts)
            {
                _logger.LogWarning("Rate limit exceeded for key: {Key}", key);
                return true;
            }

            currentAttempts++;
            await _cache.SetStringAsync(
                key,
                currentAttempts.ToString(),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = window
                }
            );

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking rate limit for key: {Key}", key);
            return false;
        }
    }
}