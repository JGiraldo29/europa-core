# Europa Core ヽ(・ω・)ﾉ
An open-source, end-to-end encrypted file transfer system built with ASP.NET Core.

## Features
- End-to-end encryption using Web Crypto API
- Chunked file uploads for large files
- Rate limiting
- File expiration system
- Configurable storage providers
- MIT License

## Prerequisites
- .NET 8.0 SDK
- Visual Studio 2022/VS Code/Rider
- Azure Storage Account (or other storage provider)

## Quick Start
1. Clone the repository
2. Create your Program.cs
3. Configure your appsettings.json
4. Add your views.
5. Run the application

## Configuration
Basic configuration example:
```json
{
  "Storage": {
    "Provider": "AzureBlob",
    "ConnectionString": "your_connection_string"
  },
  "RateLimiting": {
    "UploadLimit": "100mb",
    "TimeWindow": "1m"
  }
}
