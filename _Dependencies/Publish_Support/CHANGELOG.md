# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.2.2] - 2026-10-17 Vesuvius patch

### 🔄 Changed

#### Infrastructure & DevOps
- Moved FareHarbor webhook authentication to validated configuration, with deployment secrets wired and local setup documented.
- Separated environment-specific settings and refreshed pipeline package versions.

### ✅ Added

#### Testing & Quality
- Expanded unit and API coverage and added isolated SMTP integration tests.

## [1.2.1] - 2026-10-03 Vesuvius patch

### ✅ Added

#### Tour Selection
- Added an accessible supplier-type filter to the tour selection page when supplier tags are available.

### 🔄 Changed

#### Infrastructure & DevOps
- Migrated the build pipelines from NUKE to Fallout and refreshed NuGet dependencies.

#### Testing & Quality
- Expanded model and service unit-test coverage and improved integration-test isolation.

### 🐛 Fixed

#### Tour Selection
- Suppliers on the tour selection page are now listed alphabetically.

## [1.2.0] - 2026-07-20 Vesuvius 

### ✅ Added

#### Booking Flow
- Support three‑tasting tour bookings: persisted TastingCount on the Booking model, parsing FareHarbor items to derive tasting count, and API/webhook handling to set the count.
- EF Core migration added (20260718145929_AddThreeTastingTour) and model snapshot updated — deploys must run the migration.

### 🔄 Changed

#### Admin Experience
- Admin bookings: link booking visits to the Visits list and highlight the target row; booking-detail rows are clickable, keyboard-accessible, and preserve list context when navigating back.

### 🐛 Fixed

#### Booking Flow
- Past bookings tile now navigates to the past bookings filter.
- Tour validation now surfaces results for tours in the past.
- Tasting selection is locked until booking validation completes; improved accessibility and client-side guards to prevent programmatic bypass.

### 🔄 Changed

#### Testing & Quality
- Unit, API and UI tests updated to cover the three‑tasting flow, locked/unlocked selection behaviour, and admin bookings navigation.

## [1.1.3] - 2026-07-16

### ✅ Added

#### Booking Flow
- Accept identifying the Weymouth tour by either its tour name or availability.item.pk when processing incoming bookings.
- Webhook handling now accepts pk as string or number and logs which match triggered processing.

### 🔄 Changed

#### Testing & Quality
- Updated unit and API tests to use the new Weymouth tour name and matching webhook payloads.
- Isolated an anti-forgery integration test to avoid shared seeded state, ensured atomic setup/teardown, and improved teardown reliability.

## [1.1.2] - 2026-07-07

### ✅ Added

#### Testing & Quality
- Expanded supplier visit integration coverage for negative paths, anti-forgery rejection, and prevention of legacy visit-recording route regressions
- Added unit and integration coverage for partner booking lookup behaviour across partner reference and email-match scenarios

### 🔄 Changed

#### Admin Experience
- Improved supplier administration navigation with dashboard shortcuts, list filtering/sorting updates, and full-row navigation that preserves selected list context
- Refined admin dashboard navigation and supplier form labelling to make admin workflows more intuitive

#### Booking Flow
- Removed the dedicated visit-recording API surface in favour of the existing MVC supplier-visit confirmation flow with anti-forgery protection

#### Infrastructure & DevOps
- Integration email SSL handling is now opt-in when no explicit test configuration value is provided

### 🐛 Fixed

#### Booking Flow
- Partner booking lookup now bypasses email confirmation when the entered email matches the stored booking email (case-insensitive), while retaining confirmation behaviour for mismatches

## [1.1.1] - 2026-06-28

### ✅ Added

#### Supplier Features
- Supplier offer descriptions can now be captured in admin forms and shown on the supplier visit page
- Admins can now toggle whether supplier codes are required when recording visits

### 🔄 Changed

#### Testing & Quality
- UI and integration test runs now use dedicated test solution builds and `dotnet test`
- Playwright browser installation now targets the UI test project directly for more reliable setup
- Test package versions were refreshed to keep the suite stable

#### Infrastructure & DevOps
- CI now builds the test solution separately before running unit and integration tests

## [1.1.0] - 2026-06-25 Etna

### ✅ Added

#### Admin Experience
- Google OAuth admin authentication with email-whitelist authorisation, plus dedicated admin login, logout, and access-denied flows
- Admin bookings management with booking detail views, tab filtering, pagination, and sortable columns
- Admin supplier management with list, create, edit, and soft-delete (inactive) support
- Admin visits management with filtering, sorting, pagination, and reimbursement status controls
- Visit reimbursement tracking persisted on visits, including reimbursement timestamp data

#### Testing & Quality
- API integration test infrastructure with isolated fixtures, request helpers, shared test data, and broader endpoint coverage
- Playwright UI integration testing with page objects, diagnostics, fixture hardening, and expanded end-to-end journey coverage
- CI pipeline support for UI integration tests, including browser installation and failure artifact upload

### 🔄 Changed

#### Admin Experience
- Admin dashboard metrics and cards were refined for clearer navigation, improved accessibility labels, and emphasis on visits awaiting reimbursement
- Admin bookings and visits screens were improved for accessibility, sorting behaviour, and overall usability

#### Infrastructure & DevOps
- Deployment now injects Google OAuth credentials into deployed configuration from managed pipeline secrets
- Build and test pipeline behaviour was updated to better isolate integration test projects and execution paths
- Package maintenance updates include MailKit `4.17.0` and additional dependency version refreshes

### 🐛 Fixed

#### Booking Flow
- Booking lookup and client-side input handling were hardened to reduce stale async responses and race-condition issues


## [1.0.2] - 2026-04-28

### ✅ Added

#### Booking Flow
- Partner bookings can now require email confirmation before access, with persisted confirmation state, secure time-limited confirmation links, and a dedicated confirmation page

#### Notifications
- Partner booking confirmation emails are now sent using dedicated templates and view models

### 🔄 Changed

#### Booking Flow
- Booking lookup feedback now uses clearer alerts, improved accessibility, and more modular client-side message handling
- Booking state handling now redirects cancelled, completed, and confirmed bookings to the appropriate pages instead of surfacing generic model errors
- Past tour dates are now treated as completed when deciding whether to show QR-code flows
- Booking lookup now preserves user input when validation fails and prefers voucher numbers when deriving purchase codes
- The home page booking entry flow now uses `qrToken` as the index parameter instead of `bookingReference`

#### Infrastructure & DevOps
- EF Core and configuration-related NuGet packages were updated to version `10.0.7` across the application, test project, and pipeline project

### 🐛 Fixed

#### Booking Flow
- The booking confirmation modal no longer reopens repeatedly for the same booking and now reuses a single Bootstrap modal instance

## [1.0.1] - 2026-04-19

### ✅ Added

#### Booking Flow
- Optional voucher number support for booking creation, updates, and lookup
- Day-specific supplier opening times in the supplier data model and booking experience
- Supplier opening times that adapt to the looked-up booking's tour date

#### Notifications
- Support for multiple semicolon-separated supplier email addresses
- Strongly-typed Razor email templates for booking confirmations and visit notifications

### 🔄 Changed

- Supplier opening-times messaging now restores the default display when booking lookup feedback is cleared
- Supplier contact email length restrictions were relaxed to support the updated notification flow
- Email notification rendering is now centralised for more consistent customer, supplier, and admin emails

## [1.0.0] - 2026-04-04 Kilimanjaro

### ✅ Added

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

[1.2.2]: https://github.com/e33ah/FoodTour/releases/tag/v1.2.2
[1.2.1]: https://github.com/e33ah/FoodTour/releases/tag/v1.2.1
[1.2.0]: https://github.com/e33ah/FoodTour/releases/tag/v1.2.0
[1.1.3]: https://github.com/e33ah/FoodTour/releases/tag/v1.1.3
[1.1.2]: https://github.com/e33ah/FoodTour/releases/tag/v1.1.2
[1.1.1]: https://github.com/e33ah/FoodTour/releases/tag/v1.1.1
[1.1.0]: https://github.com/e33ah/FoodTour/releases/tag/v1.1.0
[1.0.2]: https://github.com/e33ah/FoodTour/releases/tag/v1.0.2
[1.0.1]: https://github.com/e33ah/FoodTour/releases/tag/v1.0.1
[1.0.0]: https://github.com/e33ah/FoodTour/releases/tag/v1.0.0
