# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-04-04 Kilimanjaro

### Added

#### Booking Flow
- Supplier selection and validation on the landing page with enhanced supplier cards (tags and descriptions)
- Tour date selection and validation
- Customer email collection and validation with purchase code requirement
- Two-step booking process: email/code lookup followed by supplier selection
- Booking confirmation page with robust save logic and auto-migrations
- QR code generation on booking confirmation
- Support for pre-filling the booking form via query parameters
- Real-time booking check API with UI feedback on the index page
- External API endpoints for creating and updating bookings, secured with key authentication
- Resilient webhook handling for booking events
- Email confirmation sent to the customer on booking
- Supplier reselection prevented once a booking is confirmed

#### Supplier Features
- Supplier entity and data model with EF Core, including validation constraints
- Supplier visit recording and tracking per booking
- Supplier confirmation code required to record a visit
- Supplier opening times displayed on the booking page
- Supplier email notifications when a visit is recorded
- Admin notification when a visit is recorded
- Guest count selection by the supplier during visit recording
- Supplier visit form restricted to the tour date only
- Tour map link made more prominent in the UI

#### UI & Branding
- Kabincraft branding with logo (PNG) and favicon
- Standardised UI using Bootstrap cards and icons
- Cookie consent banner with persisted user choice
- Privacy policy link in the footer
- Improved clarity of warnings and validation messages
- Line-break support in supplier descriptions and opening times

#### Infrastructure & DevOps
- NUKE build pipeline with CI integration via GitHub Actions
- CD pipeline with FTP deployment and maintenance page support
- Local configuration support for development environments
- Serilog structured logging with UK-localised timestamps
- Data Protection keys persisted in the SQL database
- Configurable support contact details
- Support page
- Release workflow with automated GitHub release creation
- `IsLocalDeployEnabled` flag for controlling auto-migrations per environment

[1.0.0]: https://github.com/e33ah/FoodTour/releases/tag/v1.0.0
