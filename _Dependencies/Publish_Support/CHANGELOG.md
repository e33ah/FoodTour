# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.2] - 2026-04-28

### Added

#### Booking Flow
- Partner bookings can now require email confirmation before access, with persisted confirmation state, secure time-limited confirmation links, and a dedicated confirmation page

#### Notifications
- Partner booking confirmation emails are now sent using dedicated templates and view models

### Changed

#### Booking Flow
- Booking lookup feedback now uses clearer alerts, improved accessibility, and more modular client-side message handling
- Booking state handling now redirects cancelled, completed, and confirmed bookings to the appropriate pages instead of surfacing generic model errors
- Past tour dates are now treated as completed when deciding whether to show QR-code flows
- Booking lookup now preserves user input when validation fails and prefers voucher numbers when deriving purchase codes
- The home page booking entry flow now uses `qrToken` as the index parameter instead of `bookingReference`

#### Infrastructure & DevOps
- EF Core and configuration-related NuGet packages were updated to version `10.0.7` across the application, test project, and pipeline project

### Fixed

#### Booking Flow
- The booking confirmation modal no longer reopens repeatedly for the same booking and now reuses a single Bootstrap modal instance

## [1.0.1] - 2026-04-19

### Added

#### Booking Flow
- Optional voucher number support for booking creation, updates, and lookup
- Day-specific supplier opening times in the supplier data model and booking experience
- Supplier opening times that adapt to the looked-up booking's tour date

#### Notifications
- Support for multiple semicolon-separated supplier email addresses
- Strongly-typed Razor email templates for booking confirmations and visit notifications

### Changed

- Supplier opening-times messaging now restores the default display when booking lookup feedback is cleared
- Supplier contact email length restrictions were relaxed to support the updated notification flow
- Email notification rendering is now centralised for more consistent customer, supplier, and admin emails

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

[1.0.2]: https://github.com/e33ah/FoodTour/releases/tag/v1.0.2
[1.0.1]: https://github.com/e33ah/FoodTour/releases/tag/v1.0.1
[1.0.0]: https://github.com/e33ah/FoodTour/releases/tag/v1.0.0
