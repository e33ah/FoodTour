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

### Database Setup

1. Update the connection string in `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "FoodTourContext": "Server=(localdb)\\mssqllocaldb;Database=FoodTour;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

2. Apply migrations to create the database:
```bash
dotnet ef database update --project FoodTour/FoodTour.csproj
```

### Running the Application

#### Using Visual Studio
1. Open the solution in Visual Studio
2. Set `FoodTour` as the startup project
3. Press F5 to run

#### Using Command Line
```bash
cd FoodTour
dotnet run
```

The application will be available at `https://localhost:5001` (or the port specified in launchSettings.json).

### Running Tests

```bash
dotnet test
```

## CI/CD

To run the build pipeline locally using NUKE, use the following commands based on your operating system:

```bash
# Windows
.\build.cmd 

# Linux/macOS
./build.sh 

# Using Nuke global tool
nuke
```

The project uses GitHub Actions for continuous integration.