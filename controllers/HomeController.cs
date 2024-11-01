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
using Europa.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Diagnostics;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IFileService _fileService;
    private const long DEFAULT_MAX_FILE_SIZE = 2L * 1024 * 1024 * 1024;

    public HomeController(
        ILogger<HomeController> logger,
        IFileService fileService)
    {
        _logger = logger;
        _fileService = fileService;
    }

    public IActionResult Index()
    {
        ViewBag.MaxTotalFileSize = DEFAULT_MAX_FILE_SIZE;
        return View();
    }

    [HttpGet("d/{id}")]
    [EnableRateLimiting("download")]
    public async Task<IActionResult> DownloadFile(string id)
    {
        try
        {
            var fileInfo = await _fileService.GetFileInfoAsync(id);
            if (fileInfo == null)
            {
                return NotFound();
            }

            if (fileInfo.ExpirationDate < DateTime.UtcNow)
            {
                return NotFound("This file has expired.");
            }

            return View(fileInfo);
        }
        catch (FileNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while preparing file download");
            return StatusCode(500, "An error occurred while processing your request.");
        }
    }

    [HttpGet("download-file/{fileId}")]
    [EnableRateLimiting("download")]
    public async Task<IActionResult> DownloadFileContent(string fileId)
    {
        try
        {
            var (stream, iv, salt, isMultiFile) = await _fileService.GetFileStreamAsync(fileId);

            // Set encryption-related headers
            Response.Headers.Add("X-IV", Convert.ToBase64String(iv));
            Response.Headers.Add("X-Salt", Convert.ToBase64String(salt));
            Response.Headers.Add("X-Is-Multi-File", isMultiFile.ToString());

            return new FileStreamResult(stream, "application/octet-stream")
            {
                FileDownloadName = fileId,
                EnableRangeProcessing = true
            };
        }
        catch (FileNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while downloading file");
            return StatusCode(500, "An error occurred while processing your request.");
        }
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }
}