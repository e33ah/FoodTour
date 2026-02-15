# FoodTour

A web application for creating self-guided tasting tours in Weymouth.

## Overview

FoodTour allows users to browse and select from a list of active food suppliers to create their own personalised tasting tour experience. Users can select a max number of suppliers at a time to visit on their self-guided tour.

## Technology Stack

- **Framework**: ASP.NET Core MVC (.NET 10)
- **Language**: C# 14.0
- **Database**: SQL Server with Entity Framework Core
- **Frontend**: Bootstrap 5, Razor Views
- **Build System**: NUKE Build Automation
- **CI/CD**: GitHub Actions

## Getting Started

### Prerequisites

- .NET 10 SDK
- SQL Server (LocalDB or full installation)
- Visual Studio 2022 or VS Code
- Nuke global tool (optional)

### Database Setup

1. Update the connection string in `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "FoodTourContext": "Server=(localdb)\\mssqllocaldb;Database=FoodTour;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

2. Apply migrations to create the database.

This solution uses two EF Core contexts:
- `FoodTourContext` for application data
- `DataProtectionKeyContext` for ASP.NET Core data protection keys

On application start-up, pending migrations are applied automatically for both contexts.

If you prefer to apply migrations manually, use the following commands to apply migrations for both contexts:
```bash
dotnet ef database update --project FoodTour/FoodTour.csproj --context FoodTourContext
dotnet ef database update --project FoodTour/FoodTour.csproj --context DataProtectionKeyContext
```

3. Add Sample Data (optional):
Run the SQL scripts in `_Dependencies/Sample_Data/` against your database to populate it.

### Running the Application

#### Using Visual Studio
1. Open the solution in Visual Studio
2. Set `FoodTour` as the startup project
3. Press F5 to run

#### Using Command Line
```
cd FoodTour
dotnet run
```

The application will be available at `https://localhost:5001` (or the port specified in launchSettings.json).

### Running Unit Tests

Tests are located in the `FoodTour.Tests` project. To run the tests, use the following command:
```
dotnet test
```

## CI/CD

The  project uses NUKE for build automation. The build CI pipeline is set to run as the default.

To run the CI pipeline locally using NUKE, use the following commands based on your operating system:

```
# Windows
.\build.cmd 

# Linux/macOS
./build.sh 

# Using Nuke global tool
nuke
```

To run the CD pipeline locally with the Nuke global tool, add `FtpHost`, `FtpUser`, and `FtpPassword` to user secrets.
Run:
```
nuke Release
```
The project uses GitHub Actions for continuous integration and continuous deployment.