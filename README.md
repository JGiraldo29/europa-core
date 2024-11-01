# Europa Core
An open-source, end-to-end encrypted file transfer system built with ASP.NET Core.

## Features ヽ(・ω・)ﾉ
- End-to-end encryption using Web Crypto API
- Chunked file uploads for large files
- Rate limiting
- File expiration system
- Configurable storage providers
- MIT License

## Prerequisites __〆(´ω｀)
- .NET 8.0 SDK
- Visual Studio 2022/VS Code/Rider
- Azure Storage Account (or other storage provider)

## Quick Start C= C= C= C= C=┌(;・ω・)┘
1. Clone the repository
2. Create your Program.cs
3. Configure your appsettings.json
4. Add your views.
5. Run the application

## Configuration __〆(´ω｀;)
Basic configuration example:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "my-database-connection;"
  },
  "StorageConfig": {
    "ConnectionString": "my-storage-connection"
  }
}
