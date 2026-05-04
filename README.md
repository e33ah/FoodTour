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
- Visual Studio 2026 or VS Code
- Nuke global tool (optional)
- Playwright browser binaries for the UI integration test project

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

### Creating Bookings

Bookings can be created in two ways:
- **Via API**: Use the `/api/foodtour/fhwebhooks/newbooking` endpoint with the JSON in `_Dependencies/Sample_Data/` to create bookings
- **Sample Data**: Run the `Bookings.sql` script in `_Dependencies/Sample_Data/` to add some sample bookings directly to the database

You will need the email and purchase code from one of the bookings in the database to use the app and test the booking confirmation flow.

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

### Email Settings

FoodTour sends booking confirmation emails via SMTP using the `EmailSettings` configuration section.

For local development, `EmailSettings` are configured in `FoodTour/appsettings.Development.json`. 
You can use a local SMTP server such as `smtp4dev` to view emails without sending them externally.

If email sending fails (or SMTP is not configured), the booking will still be saved and the user will still be taken to the booking confirmation page, but a warning message will be shown.

### Running Unit Tests

Unit tests are located in the `FoodTour.Test` project. To run them, use the following command:
```
dotnet run --project FoodTour.Test\FoodTour.Test.csproj
```

### Running UI Integration Tests

Browser-driven UI integration tests are located in the `FoodTour.ITest.UI` project. These tests host the MVC application on a real localhost port, create an isolated LocalDB database for each run, apply migrations automatically, seed deterministic test data, and replace the real email sender with a test double so no external emails are sent.

Install the Playwright browser binaries the first time you run the suite:
```
pwsh .\FoodTour.ITest.UI\bin\Debug\net10.0\playwright.ps1 install chromium
```

Then run the UI integration tests with:
```
dotnet run --project FoodTour.ITest.UI\FoodTour.ITest.UI.csproj
```

The UI test database is created with a unique name for each run and deleted during fixture disposal, so local runs should not leave seeded test databases behind.

Default UI test settings live in `FoodTour.ITest.UI\appsettings.UiTests.json`.

By default the UI suite uses LocalDB. To override the JSON defaults for another environment without changing the tests, set the optional environment variables below before running the suite:
```
$env:UiTests__Database__DatabaseNamePrefix = "FoodTourITests_"
$env:UiTests__Database__SqlServerConnectionStringTemplate = "Server=localhost,1433;Database={0};User Id=sa;Password=Your_password123;TrustServerCertificate=True;MultipleActiveResultSets=True"
$env:UiTests__EmailSettings__Host = "smtp4dev"
$env:UiTests__EmailSettings__Port = "25"
```

The SQL Server connection string template must include `{0}` so the fixture can inject a unique database name for each run.

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
The CD pipeline is configured to deploy to the development environment.
The release pipeline is configured to deploy to the production environment.
The project uses GitHub Actions for continuous integration and continuous deployment.
