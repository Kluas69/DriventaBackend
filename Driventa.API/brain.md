The architecture uses ASP.NET Core Web API + SQL Server + Entity Framework Core + ASP.NET Core Identity + SignalR. Clean Architecture is a good fit because it keeps your dispatch business logic separate from the database and infrastructure, while ASP.NET Core Identity provides user/role management and SignalR supports real-time dashboard/chat updates.

1. Complete System Architecture
   ┌─────────────────────┐
   │ DRIVENTA WEBSITE │
   │ driventa.us │
   └──────────┬──────────┘
   │
   Application Form
   Contact Form
   Live Chat
   │
   ▼
   ┌─────────────────────┐
   │ .NET WEB API │
   │ ASP.NET Core │
   └──────────┬──────────┘
   │
   ┌────────────────┼────────────────┐
   │ │ │
   ▼ ▼ ▼
   ┌─────────────┐ ┌────────────┐ ┌──────────────┐
   │ SQL Server │ │ SignalR │ │ File Storage │
   │ Database │ │ Real-Time │ │ Documents │
   └─────────────┘ └────────────┘ └──────────────┘
   │ │
   └────────────────┘
   │
   ▼
   ┌─────────────────────┐
   │ ADMIN DASHBOARD │
   │ dashboard.driventa │
   └─────────────────────┘
   │
   ┌────────────┼────────────┐
   ▼ ▼ ▼
   Admin Dispatcher Sales
   Recommended domains
   www.driventa.us
   ↓
   Public Website

api.driventa.us
↓
ASP.NET Core Backend API

dashboard.driventa.us
↓
Internal Admin Dashboard

You can also keep the dashboard under:

driventa.us/dashboard

But I prefer a separate dashboard application because it keeps the public website and internal system cleanly separated.

2. Dashboard Navigation

Your final sidebar should look like this:

DRIVENTA
━━━━━━━━━━━━━━━━━━━━

🏠 Dashboard

LEADS & ONBOARDING
📝 Applications
👥 Carriers
📄 Documents

OPERATIONS
🚛 Loads
🚚 Trucks
👨 Drivers
🏢 Brokers

COMMUNICATION
💬 Messages
🔔 Notifications

FINANCE
💰 Billing
🧾 Invoices
💳 Payments

MANAGEMENT
👨‍💼 Dispatchers
👥 Team & Users
📊 Reports

SYSTEM
⚙️ Settings
🔐 Security
📋 Activity Logs

For the first version, I would actually hide some future modules until they're ready.

3. ASP.NET Core Project Structure

I recommend this:

Driventa.sln

src/
│
├── Driventa.Domain/
│ │
│ ├── Entities/
│ │ ├── Application.cs
│ │ ├── Carrier.cs
│ │ ├── Truck.cs
│ │ ├── Driver.cs
│ │ ├── Load.cs
│ │ ├── Broker.cs
│ │ ├── Document.cs
│ │ ├── Note.cs
│ │ ├── Notification.cs
│ │ ├── Conversation.cs
│ │ ├── Message.cs
│ │ ├── Invoice.cs
│ │ ├── Payment.cs
│ │ └── ActivityLog.cs
│ │
│ ├── Enums/
│ │ ├── ApplicationStatus.cs
│ │ ├── CarrierStatus.cs
│ │ ├── TruckStatus.cs
│ │ ├── DriverStatus.cs
│ │ ├── LoadStatus.cs
│ │ ├── InvoiceStatus.cs
│ │ └── UserRole.cs
│ │
│ └── Common/
│ └── BaseEntity.cs
│
├── Driventa.Application/
│ │
│ ├── Applications/
│ ├── Carriers/
│ ├── Loads/
│ ├── Trucks/
│ ├── Drivers/
│ ├── Brokers/
│ ├── Documents/
│ ├── Messages/
│ ├── Billing/
│ ├── Reports/
│ └── Interfaces/
│
├── Driventa.Infrastructure/
│ │
│ ├── Persistence/
│ │ ├── AppDbContext.cs
│ │ ├── Configurations/
│ │ └── Migrations/
│ │
│ ├── Identity/
│ │ └── ApplicationUser.cs
│ │
│ ├── Services/
│ │ ├── EmailService.cs
│ │ ├── FileStorageService.cs
│ │ ├── NotificationService.cs
│ │ └── CurrentUserService.cs
│ │
│ └── Repositories/
│
└── Driventa.API/
│
├── Controllers/
│ ├── AuthController.cs
│ ├── PublicApplicationsController.cs
│ ├── ApplicationsController.cs
│ ├── CarriersController.cs
│ ├── LoadsController.cs
│ ├── TrucksController.cs
│ ├── DriversController.cs
│ ├── BrokersController.cs
│ ├── DocumentsController.cs
│ ├── MessagesController.cs
│ ├── BillingController.cs
│ └── ReportsController.cs
│
├── Hubs/
│ ├── ChatHub.cs
│ └── NotificationHub.cs
│
├── Middleware/
│ └── ExceptionMiddleware.cs
│
└── Program.cs

This separation follows the Clean Architecture principle of keeping the core business model independent from infrastructure concerns.

4. Database Design
   Base fields

Almost every important table should have:

Id UNIQUEIDENTIFIER
CreatedAt DATETIMEOFFSET
UpdatedAt DATETIMEOFFSET
CreatedByUserId UNIQUEIDENTIFIER NULL
UpdatedByUserId UNIQUEIDENTIFIER NULL
IsDeleted BIT

I recommend using GUIDs/UUID-style identifiers for your public-facing API objects.

5. Identity & Users

Use ASP.NET Core Identity for your internal dashboard users. Microsoft documents Identity as the built-in membership system for managing users, passwords, roles, and related identity data.

Users
AspNetUsers

Additional profile fields:

Id
FirstName
LastName
Email
PhoneNumber
ProfileImageUrl
IsActive
LastLoginAt
CreatedAt
Roles
AspNetRoles

Your roles:

SuperAdmin
Admin
DispatchManager
Dispatcher
Sales
Permission matrix
Module Admin Manager Dispatcher Sales
Applications Full Full View assigned Full
Carriers Full Full Assigned only Limited
Loads Full Full Assigned only No
Trucks Full Full Assigned only No
Drivers Full Full Assigned only No
Brokers Full Full View No
Finance Full Limited No No
Reports Full Full Limited Limited
Settings Full No No No

Important: Don't rely only on hiding buttons in the frontend. The API itself must enforce authorization.

6. Website Application Module

Your current website form should call a public API endpoint:

POST /api/public/applications

Example request:

{
"fullName": "John Smith",
"email": "john@example.com",
"phone": "+1 555 000 0000",
"companyName": "Smith Trucking LLC",
"equipmentType": "Dry Van",
"truckCount": 2,
"mcNumber": "MC123456",
"dotNumber": "1234567",
"preferredLanes": "Texas to California",
"additionalDetails": "Looking for full dispatch support"
}
Applications table
Applications
──────────────────────────────────
Id
ApplicationNumber
FullName
Email
Phone
CompanyName
EquipmentType
TruckCount
McNumber
DotNumber
PreferredLanes
AdditionalDetails

Status
AssignedToUserId

SubmittedAt
ContactedAt
ApprovedAt
RejectedAt

ConvertedCarrierId
Application statuses
New
Reviewing
Contacted
Qualified
Approved
Rejected
Onboarded 7. Application → Carrier Conversion

This is a very important workflow.

WEBSITE APPLICATION
│
▼
NEW
│
▼
REVIEWING
│
▼
QUALIFIED
│
▼
APPROVED
│
▼
┌─────────────────┐
│ CONVERT TO │
│ CARRIER BUTTON │
└────────┬────────┘
│
▼
CARRIER PROFILE CREATED
│
├── Company Information
├── Contact Information
├── Equipment
├── Dispatcher Assigned
└── Onboarding Documents

When Admin clicks Convert to Carrier, the backend should perform a transaction:

1. Create Carrier
2. Copy approved application information
3. Link Application to Carrier
4. Create Activity Log
5. Update Application status
6. Create Notification
7. Commit everything

This should be one backend transaction so you don't end up with half-completed conversions.

8. Carrier Module
   Carriers table
   Carriers
   ──────────────────────────────
   Id
   CompanyName
   ContactName
   Email
   Phone

McNumber
DotNumber

AddressLine1
AddressLine2
City
State
ZipCode

Status
AssignedDispatcherId

PreferredLanes
Notes

ApplicationId
CreatedAt
UpdatedAt
Carrier status
Lead
Onboarding
Active
Paused
Inactive
Suspended
Carrier relationships
Carrier
│
├── Multiple Trucks
├── Multiple Drivers
├── Multiple Loads
├── Multiple Documents
├── Multiple Notes
├── Multiple Invoices
└── One Assigned Dispatcher 9. Trucks Module
Trucks
────────────────────
Id
CarrierId

TruckNumber
EquipmentType

Make
Model
Year

LicensePlate
LicenseState

Status
CreatedAt

Status:

Available
OnLoad
Maintenance
Inactive

Equipment types can initially be:

DryVan
Reefer
Flatbed
StepDeck
BoxTruck
Hotshot
PowerOnly

Later, move these into a database lookup/settings table if you need administrators to manage them.

10. Drivers Module
    Drivers
    ────────────────────
    Id
    CarrierId
    TruckId

FirstName
LastName
Email
Phone

LicenseNumber
LicenseState

Status
CreatedAt

Status:

Available
Assigned
Driving
OffDuty
Inactive 11. Loads Module — The Core of Operations
Loads table
Loads
──────────────────────────────
Id
LoadNumber

CarrierId
TruckId
DriverId
BrokerId
DispatcherId

EquipmentType

PickupCity
PickupState
PickupDateTime

DeliveryCity
DeliveryState
DeliveryDateTime

Rate
Miles
RatePerMile

DispatchFeeType
DispatchFeeValue
DispatchFeeAmount

CarrierNetAmount

Status

BookedAt
PickedUpAt
DeliveredAt
CompletedAt

CreatedAt
Load status
Available
Negotiating
Booked
Dispatched
PickedUp
InTransit
Delivered
Completed
Cancelled
Issue
Load lifecycle
LOAD CREATED
↓
NEGOTIATING
↓
BOOKED
↓
DISPATCHED
↓
PICKED UP
↓
IN TRANSIT
↓
DELIVERED
↓
COMPLETED

The dashboard should show a visual status timeline.

12. Broker Module
    Brokers
    ────────────────────────
    Id
    CompanyName
    ContactName
    Email
    Phone

McNumber
Address

InternalRating
PaymentNotes
GeneralNotes

IsActive
CreatedAt

Later you can calculate:

Total loads
Average rate
Average RPM
Cancellation rate
Internal payment reliability score 13. Documents Module

Don't store large PDF/document files directly inside SQL Server unless you have a specific reason.

Better:

SQL DATABASE
│
└── File metadata
│
▼
CLOUD STORAGE
│
├── PDF
├── Images
└── Documents
Documents table
Documents
────────────────────────────
Id

FileName
StoredFileName
FileUrl
ContentType
FileSize

DocumentType

CarrierId NULL
LoadId NULL
DriverId NULL

UploadedByUserId
CreatedAt
ExpiresAt NULL

Document types:

Insurance
W9
MC_Authority
RateConfirmation
BOL
POD
CarrierAgreement
DriverLicense
Other

The ExpiresAt field is useful for things such as insurance expiration reminders.

14. Notes Module

Instead of putting one Notes text field everywhere, create a reusable notes system.

Notes
────────────────────────
Id
EntityType
EntityId

Content

CreatedByUserId
CreatedAt

Examples:

EntityType = Carrier
EntityId = abc123

EntityType = Load
EntityId = xyz456

Or, if you prefer stricter foreign keys, create separate tables such as:

CarrierNotes
LoadNotes
ApplicationNotes

For Driventa, I would prefer separate strongly typed tables or explicit relationships over a generic polymorphic EntityType + EntityId design because foreign-key integrity matters.

15. Activity Logs

This should automatically track important actions.

ActivityLogs
────────────────────────────
Id
UserId

Action
EntityType
EntityId

Description

OldValuesJson
NewValuesJson

IpAddress
CreatedAt

Example:

John Admin
changed Load #LD-1042

Status:
Booked → Picked Up

Date:
August 28, 2026

This is especially useful when multiple dispatchers use the system.

16. Real-Time Website Chat

Use SignalR for this module. ASP.NET Core SignalR supports hub-based real-time communication, where connected clients can receive updates immediately.

Database
Conversations
Conversations
─────────────────────
Id
VisitorId
VisitorName
VisitorEmail
VisitorPhone

AssignedToUserId
Status

StartedAt
LastMessageAt
Messages
Messages
─────────────────────
Id
ConversationId

SenderType
SenderUserId NULL

Content
IsRead

CreatedAt

Sender type:

Visitor
Admin
Dispatcher
System
Flow
WEBSITE VISITOR
│
▼
Chat Widget
│
▼
SignalR Hub
│
├───────────────┐
▼ ▼
Save to SQL Real-Time Push
│ │
└───────┬───────┘
▼
ADMIN DASHBOARD

You should have:

/api/messages/conversations
GET conversations

/api/messages/conversations/{id}
GET messages

POST /api/messages
REST fallback/send endpoint

And:

/chatHub

for real-time communication.

17. Notifications
    Notifications table
    Notifications
    ────────────────────────
    Id
    UserId

Type
Title
Message

EntityType
EntityId

IsRead
CreatedAt

Examples:

NEW_APPLICATION
NEW_MESSAGE
LOAD_STATUS_CHANGED
DOCUMENT_EXPIRING
DOCUMENT_UPLOADED
CARRIER_ASSIGNED

Flow:

New Application
↓
Database saves application
↓
Notification created
↓
SignalR pushes notification
↓
Dashboard badge updates instantly 18. Finance Module

I recommend keeping the finance module simple initially.

Invoices
Invoices
────────────────────
Id
InvoiceNumber
CarrierId

PeriodStart
PeriodEnd

Subtotal
TaxAmount
TotalAmount

Status
DueDate

CreatedAt
PaidAt
Invoice Items
InvoiceItems
────────────────────
Id
InvoiceId
LoadId NULL

Description
Quantity
UnitPrice
Amount

Example:

Invoice #INV-2026-00124

Carrier: Smith Trucking

Load #LD-1001
Dispatch Fee: $200

Load #LD-1002
Dispatch Fee: $175

Load #LD-1003
Dispatch Fee: $225

TOTAL: $600
Payments
Payments
────────────────────
Id
InvoiceId

Amount
PaymentMethod
TransactionReference

Status
PaidAt
CreatedAt

Invoice status:

Draft
Sent
PartiallyPaid
Paid
Overdue
Cancelled 19. Dashboard Analytics API

Your dashboard homepage can call:

GET /api/dashboard/summary

Response:

{
"newApplications": 12,
"applicationsInReview": 7,
"activeCarriers": 34,
"activeTrucks": 51,
"activeLoads": 18,
"loadsInTransit": 8,
"completedLoadsThisMonth": 126,
"dispatchRevenueThisMonth": 28450
}

Additional endpoints:

GET /api/dashboard/recent-applications
GET /api/dashboard/recent-activity
GET /api/dashboard/load-status-summary
GET /api/dashboard/revenue-summary 20. Complete API Structure
Public Website API
POST /api/public/applications
POST /api/public/contact
POST /api/public/chat/session

These endpoints should have stricter anti-spam/rate-limiting protection because they are publicly exposed.

Authentication
POST /api/auth/login
POST /api/auth/refresh
POST /api/auth/logout

POST /api/auth/forgot-password
POST /api/auth/reset-password
Applications
GET /api/applications
GET /api/applications/{id}
PATCH /api/applications/{id}
POST /api/applications/{id}/assign
POST /api/applications/{id}/notes
POST /api/applications/{id}/convert-to-carrier
Carriers
GET /api/carriers
POST /api/carriers

GET /api/carriers/{id}
PATCH /api/carriers/{id}

POST /api/carriers/{id}/assign-dispatcher
GET /api/carriers/{id}/loads
GET /api/carriers/{id}/trucks
GET /api/carriers/{id}/drivers
GET /api/carriers/{id}/documents
Loads
GET /api/loads
POST /api/loads

GET /api/loads/{id}
PATCH /api/loads/{id}

POST /api/loads/{id}/status
POST /api/loads/{id}/documents
POST /api/loads/{id}/notes
Trucks
GET /api/trucks
POST /api/trucks
GET /api/trucks/{id}
PATCH /api/trucks/{id}
Drivers
GET /api/drivers
POST /api/drivers
GET /api/drivers/{id}
PATCH /api/drivers/{id}
Brokers
GET /api/brokers
POST /api/brokers
GET /api/brokers/{id}
PATCH /api/brokers/{id}
Documents
POST /api/documents/upload
GET /api/documents/{id}
DELETE /api/documents/{id}
Reports
GET /api/reports/loads
GET /api/reports/revenue
GET /api/reports/carriers
GET /api/reports/dispatchers 21. Database Relationships

This is the main relationship structure:

APPLICATION
│
│ approved/converted
▼
CARRIER
│
├───────────────┬───────────────┐
▼ ▼ ▼
TRUCKS DRIVERS DOCUMENTS
│ │
└───────┬───────┘
│
▼
LOAD
│
┌──────┼──────┐
▼ ▼ ▼
BROKER DOCS NOTES
│
▼
INVOICE
│
▼
PAYMENT

User relationships:

USER / DISPATCHER
│
├── Assigned Applications
├── Assigned Carriers
├── Assigned Loads
├── Messages
└── Activity Logs 22. Exact MVP Build Order

I strongly recommend building in this order.

STEP 1 — Foundation
ASP.NET Core API
SQL Server
Entity Framework Core
Migrations
Global Exception Handling
Logging
Swagger/OpenAPI
STEP 2 — Authentication
ASP.NET Core Identity
Roles
JWT Access Token
Refresh Token
Authorization Policies
STEP 3 — Website Integration
POST Public Application API
Validation
Anti-Spam Protection
Rate Limiting
Database Save
Email/Notification

At this point, your Driventa website form is already connected to your backend.

driventa.us Form
↓
POST /api/public/applications
↓
SQL Server
↓
Dashboard → Applications
STEP 4 — Admin Dashboard MVP

Build:

Login
Dashboard Summary
Applications List
Application Details
Status Changes
Internal Notes
Assign User
Convert to Carrier
STEP 5 — Carrier Operations
Carrier Profiles
Dispatcher Assignment
Trucks
Drivers
Documents
STEP 6 — Load Management
Create Load
Assign Carrier
Assign Truck
Assign Driver
Assign Broker
Calculate RPM
Calculate Dispatch Fee
Update Status
Upload Documents
STEP 7 — Real-Time Features
SignalR Notifications
Website Chat
Dashboard Chat Inbox
Real-Time Load Updates
STEP 8 — Finance & Reports
Invoices
Payments
Revenue Reports
Dispatcher Performance
Carrier Performance 23. Recommended Security Rules

You should include these from the beginning:

✓ HTTPS only
✓ Password hashing through Identity
✓ Role-based authorization
✓ JWT authentication
✓ Refresh tokens
✓ Rate limiting on public forms
✓ Server-side validation
✓ File type validation
✓ File size limits
✓ Secure document access
✓ Audit/activity logs
✓ Environment variables/secrets
✓ Database backups
✓ Global exception handling
✓ Do not expose stack traces in production

Also:

PUBLIC WEBSITE
↓
Can ONLY access public endpoints

DASHBOARD USER
↓
Must authenticate

DISPATCHER
↓
Can ONLY access authorized resources 24. My Final Recommended Tech Stack
BACKEND
ASP.NET Core Web API
C#

DATABASE
SQL Server

ORM
Entity Framework Core

AUTH
ASP.NET Core Identity
JWT + Refresh Tokens

REAL-TIME
ASP.NET Core SignalR

VALIDATION
FluentValidation

API DOCUMENTATION
OpenAPI / Swagger

FILE STORAGE
Azure Blob Storage or S3-compatible storage

EMAIL
Transactional email provider

LOGGING
Structured application logging

FRONTEND
Existing Driventa Website

- Separate React/Next.js or similar Admin Dashboard

ASP.NET Core's official SignalR guidance supports configuring hubs directly in the application's dependency injection and routing pipeline, making it suitable for your real-time messages and notifications module.

The architecture I would build for Driventa

Version 1:

Website Form
↓
.NET API
↓
Applications Dashboard
↓
Review / Assign / Notes
↓
Approve
↓
Convert to Carrier
↓
Add Trucks + Drivers + Documents

Version 2:

Carrier
↓
Dispatcher
↓
Create / Manage Loads
↓
Broker
↓
Track Status
↓
Delivered
↓
Completed

Version 3:

Live Website Chat
Real-Time Notifications
Invoices
Payments
Reports
Advanced Analytics

This is the structure I'd use before writing the actual code, because it gives you a scalable system without making the first version unnecessarily complex.

Next best step: build the actual SQL Server entity/database schema and ASP.NET Core models for Phase 1, starting with ApplicationUser, Application, Carrier, Truck, Driver, roles, relationships, and AppDbContext.

---

## Implementation Status

**Last Updated:** August 28, 2026

### Phase 1 — Foundation ✅ COMPLETE

- ASP.NET Core 10 API with Clean Architecture
- PostgreSQL database with Entity Framework Core
- Migrations applied successfully
- Global exception handling (ExceptionMiddleware)
- Structured logging
- Swagger/OpenAPI with JWT support
- UseStaticFiles() for serving uploaded files
- Migration guard (only runs in non-production)

### Phase 2 — Authentication ✅ COMPLETE

- ASP.NET Core Identity with roles (SuperAdmin, Admin, DispatchManager, Dispatcher, Sales)
- JWT Access Token + Refresh Token
- Password hashing through Identity
- Role-based authorization policies (10 policies)
- Forgot-password email integration
- IEmailService injected and active

### Phase 3 — Website Integration ✅ COMPLETE

- POST /api/public/applications — Rate limited (10 req/min)
- POST /api/public/contact — Rate limited
- POST /api/public/chat/session — Rate limited
- Server-side FluentValidation
- ActivityLog created on submission

### Phase 4 — Admin Dashboard MVP ✅ COMPLETE

- Login with JWT
- Dashboard Summary (GET /api/dashboard/summary)
- Dashboard Revenue Summary (GET /api/dashboard/revenue-summary)
- Applications List with pagination, search, status filter
- Application Details with Notes
- Application Status Changes with ActivityLog
- Assign User to Application
- Convert to Carrier (transaction-wrapped)
- All CRUD operations across all modules

### Phase 5 — Carrier Operations ✅ COMPLETE

- Carrier Profiles (CRUD)
- Dispatcher Assignment with ActivityLog
- Trucks (CRUD per carrier)
- Drivers (CRUD per carrier)
- Documents Upload/Delete with ActivityLog
- Carrier relationships: Trucks, Drivers, Loads, Documents, Notes

### Phase 6 — Load Management ✅ COMPLETE

- Create Load with financial calculations (RPM, dispatch fee, carrier net)
- Assign Carrier, Truck, Driver, Broker
- Update Status with ActivityLog
- Load Notes
- DispatcherId support

### Phase 7 — Real-Time Features ✅ COMPLETE

- SignalR Notification Hub with JWT auth (query string token extraction)
- SignalR Chat Hub with anonymous visitor support
- Chat sessions via REST API
- Real-time message delivery

### Phase 8 — Finance & Reports ✅ COMPLETE

- Invoices (CRUD with line items)
- Payments (record payments, auto-update invoice status)
- Revenue Reports (by period, by carrier, by dispatcher)
- Load Reports with safe empty-sequence handling
- Carrier Performance Reports
- Dispatcher Performance Reports

### Security Fixes Applied

- FluentValidation auto-validation pipeline (20 validators now active)
- SignalR JWT extraction from query string for WebSocket auth
- ChatHub allows anonymous visitors (no [Authorize] on class)
- NotificationHub ownership checks
- ExceptionMiddleware catches FluentValidation ValidationException → 400
- ExceptionMiddleware catches HubException → 400
- Number generation uses timestamp + GUID (no collision)
- Rate limiting applied to all public endpoints

### Code Quality Fixes Applied

- BaseRepository.Delete() uses soft delete (IsDeleted = true)
- CarrierResponse DTO includes AddressLine2
- ConvertToCarrier wrapped in database transaction
- AuthController uses JwtSettings POCO injection
- ActivityLog created for all state-changing operations
- ReportsController handles empty sequences safely

### Remaining Items (Future Work)

- Dispatcher-specific filtering (Dispatchers see only assigned resources)
- Unit test infrastructure
- Hangfire/background jobs for notification delivery
- Azure Blob Storage / S3 for file storage (replace local)
- Real email service (replace placeholder EmailService)
- Database indexes on frequently queried fields
- Environment variables/secrets for production
- HTTPS-only in production
- CORS for production domains

---

## Complete Database Schema

**Database:** PostgreSQL 15+ (`driventa_db`)
**ORM:** Entity Framework Core 10 with Npgsql
**Identifier Strategy:** UUID (`gen_random_uuid()`)
**Soft Delete:** Global query filter on all BaseEntity-derived tables

### Base Entity (Inherited by all tables)

All 17 business tables inherit these columns from `BaseEntity`:

| Column            | Type          | Default             | Constraints             |
| ----------------- | ------------- | ------------------- | ----------------------- |
| `Id`              | `uuid`        | `gen_random_uuid()` | PRIMARY KEY             |
| `CreatedAt`       | `timestamptz` | `now()`             | NOT NULL                |
| `UpdatedAt`       | `timestamptz` | `now()`             | NOT NULL                |
| `CreatedByUserId` | `uuid`        | NULL                | NULLABLE                |
| `UpdatedByUserId` | `uuid`        | NULL                | NULLABLE                |
| `IsDeleted`       | `boolean`     | `false`             | NOT NULL, DEFAULT false |

Global query filter: `WHERE IsDeleted = false`

---

### Table: Applications

Website lead/application submissions from driventa.us.

| Column               | Type          | MaxLength | Constraints                                                                                                   |
| -------------------- | ------------- | --------- | ------------------------------------------------------------------------------------------------------------- |
| `Id`                 | `uuid`        | —         | PK, gen_random_uuid()                                                                                         |
| `ApplicationNumber`  | `varchar`     | 20        | NOT NULL, UNIQUE                                                                                              |
| `FullName`           | `varchar`     | 200       | NOT NULL                                                                                                      |
| `Email`              | `varchar`     | 200       | NOT NULL                                                                                                      |
| `Phone`              | `varchar`     | 50        | NOT NULL                                                                                                      |
| `CompanyName`        | `varchar`     | 200       | NOT NULL                                                                                                      |
| `EquipmentType`      | `int`         | —         | NOT NULL (enum: DryVan=0, Reefer=1, Flatbed=2, StepDeck=3, BoxTruck=4, Hotshot=5, PowerOnly=6)                |
| `TruckCount`         | `int`         | —         | NOT NULL                                                                                                      |
| `McNumber`           | `varchar`     | 50        | NULLABLE                                                                                                      |
| `DotNumber`          | `varchar`     | 50        | NULLABLE                                                                                                      |
| `PreferredLanes`     | `varchar`     | 500       | NULLABLE                                                                                                      |
| `AdditionalDetails`  | `varchar`     | 2000      | NULLABLE                                                                                                      |
| `Status`             | `int`         | —         | NOT NULL, DEFAULT 0 (enum: New=0, Reviewing=1, Contacted=2, Qualified=3, Approved=4, Rejected=5, Onboarded=6) |
| `AssignedToUserId`   | `uuid`        | —         | NULLABLE                                                                                                      |
| `SubmittedAt`        | `timestamptz` | —         | NOT NULL                                                                                                      |
| `ContactedAt`        | `timestamptz` | —         | NULLABLE                                                                                                      |
| `ApprovedAt`         | `timestamptz` | —         | NULLABLE                                                                                                      |
| `RejectedAt`         | `timestamptz` | —         | NULLABLE                                                                                                      |
| `ConvertedCarrierId` | `uuid`        | —         | NULLABLE, UNIQUE (FK → Carriers.Id, ClientSetNull)                                                            |
| `CreatedAt`          | `timestamptz` | —         | NOT NULL, DEFAULT now()                                                                                       |
| `UpdatedAt`          | `timestamptz` | —         | NOT NULL, DEFAULT now()                                                                                       |
| `CreatedByUserId`    | `uuid`        | —         | NULLABLE                                                                                                      |
| `UpdatedByUserId`    | `uuid`        | —         | NULLABLE                                                                                                      |
| `IsDeleted`          | `boolean`     | —         | NOT NULL, DEFAULT false                                                                                       |

**Indexes:** `ApplicationNumber` (unique), `Status`, `Email`

---

### Table: ApplicationNotes

Internal notes on applications.

| Column            | Type          | MaxLength | Constraints                              |
| ----------------- | ------------- | --------- | ---------------------------------------- |
| `Id`              | `uuid`        | —         | PK                                       |
| `ApplicationId`   | `uuid`        | —         | NOT NULL, FK → Applications.Id (Cascade) |
| `Content`         | `varchar`     | 5000      | NOT NULL                                 |
| `CreatedAt`       | `timestamptz` | —         | NOT NULL                                 |
| `UpdatedAt`       | `timestamptz` | —         | NOT NULL                                 |
| `CreatedByUserId` | `uuid`        | —         | NULLABLE                                 |
| `UpdatedByUserId` | `uuid`        | —         | NULLABLE                                 |
| `IsDeleted`       | `boolean`     | —         | NOT NULL, DEFAULT false                  |

---

### Table: Carriers

Carrier company profiles (created from applications or manually).

| Column                 | Type          | MaxLength | Constraints                                                                                   |
| ---------------------- | ------------- | --------- | --------------------------------------------------------------------------------------------- |
| `Id`                   | `uuid`        | —         | PK                                                                                            |
| `CompanyName`          | `varchar`     | 200       | NOT NULL                                                                                      |
| `ContactName`          | `varchar`     | 200       | NOT NULL                                                                                      |
| `Email`                | `varchar`     | 200       | NOT NULL                                                                                      |
| `Phone`                | `varchar`     | 50        | NOT NULL                                                                                      |
| `McNumber`             | `varchar`     | 50        | NULLABLE                                                                                      |
| `DotNumber`            | `varchar`     | 50        | NULLABLE                                                                                      |
| `AddressLine1`         | `varchar`     | 200       | NULLABLE                                                                                      |
| `AddressLine2`         | `varchar`     | 200       | NULLABLE                                                                                      |
| `City`                 | `varchar`     | 100       | NULLABLE                                                                                      |
| `State`                | `varchar`     | 50        | NULLABLE                                                                                      |
| `ZipCode`              | `varchar`     | 20        | NULLABLE                                                                                      |
| `Status`               | `int`         | —         | NOT NULL, DEFAULT 0 (enum: Lead=0, Onboarding=1, Active=2, Paused=3, Inactive=4, Suspended=5) |
| `AssignedDispatcherId` | `uuid`        | —         | NULLABLE                                                                                      |
| `PreferredLanes`       | `varchar`     | 500       | NULLABLE                                                                                      |
| `Notes`                | `varchar`     | 2000      | NULLABLE                                                                                      |
| `ApplicationId`        | `uuid`        | —         | NULLABLE, UNIQUE (FK → Applications.Id, ClientSetNull)                                        |
| `CreatedAt`            | `timestamptz` | —         | NOT NULL                                                                                      |
| `UpdatedAt`            | `timestamptz` | —         | NOT NULL                                                                                      |
| `CreatedByUserId`      | `uuid`        | —         | NULLABLE                                                                                      |
| `UpdatedByUserId`      | `uuid`        | —         | NULLABLE                                                                                      |
| `IsDeleted`            | `boolean`     | —         | NOT NULL, DEFAULT false                                                                       |

**Indexes:** `Status`, `AssignedDispatcherId`

**Relationships:** Has many Trucks, Drivers, Loads, Documents, CarrierNotes, Invoices. Has one Application.

---

### Table: CarrierNotes

Notes attached to carriers.

| Column            | Type          | MaxLength | Constraints                          |
| ----------------- | ------------- | --------- | ------------------------------------ |
| `Id`              | `uuid`        | —         | PK                                   |
| `CarrierId`       | `uuid`        | —         | NOT NULL, FK → Carriers.Id (Cascade) |
| `Content`         | `varchar`     | 5000      | NOT NULL                             |
| `CreatedAt`       | `timestamptz` | —         | NOT NULL                             |
| `UpdatedAt`       | `timestamptz` | —         | NOT NULL                             |
| `CreatedByUserId` | `uuid`        | —         | NULLABLE                             |
| `UpdatedByUserId` | `uuid`        | —         | NULLABLE                             |
| `IsDeleted`       | `boolean`     | —         | NOT NULL, DEFAULT false              |

---

### Table: Trucks

Trucks belonging to carriers.

| Column            | Type          | MaxLength | Constraints                                                                                    |
| ----------------- | ------------- | --------- | ---------------------------------------------------------------------------------------------- |
| `Id`              | `uuid`        | —         | PK                                                                                             |
| `CarrierId`       | `uuid`        | —         | NOT NULL, FK → Carriers.Id (Cascade)                                                           |
| `TruckNumber`     | `varchar`     | 50        | NOT NULL                                                                                       |
| `EquipmentType`   | `int`         | —         | NOT NULL (enum: DryVan=0, Reefer=1, Flatbed=2, StepDeck=3, BoxTruck=4, Hotshot=5, PowerOnly=6) |
| `Make`            | `varchar`     | 100       | NULLABLE                                                                                       |
| `Model`           | `varchar`     | 100       | NULLABLE                                                                                       |
| `Year`            | `int`         | —         | NULLABLE                                                                                       |
| `LicensePlate`    | `varchar`     | 30        | NULLABLE                                                                                       |
| `LicenseState`    | `varchar`     | 50        | NULLABLE                                                                                       |
| `Status`          | `int`         | —         | NOT NULL, DEFAULT 0 (enum: Available=0, OnLoad=1, Maintenance=2, Inactive=3)                   |
| `CreatedAt`       | `timestamptz` | —         | NOT NULL                                                                                       |
| `UpdatedAt`       | `timestamptz` | —         | NOT NULL                                                                                       |
| `CreatedByUserId` | `uuid`        | —         | NULLABLE                                                                                       |
| `UpdatedByUserId` | `uuid`        | —         | NULLABLE                                                                                       |
| `IsDeleted`       | `boolean`     | —         | NOT NULL, DEFAULT false                                                                        |

**Indexes:** `CarrierId`, `Status`

---

### Table: Drivers

Drivers belonging to carriers.

| Column            | Type          | MaxLength | Constraints                                                                           |
| ----------------- | ------------- | --------- | ------------------------------------------------------------------------------------- |
| `Id`              | `uuid`        | —         | PK                                                                                    |
| `CarrierId`       | `uuid`        | —         | NOT NULL, FK → Carriers.Id (Cascade)                                                  |
| `TruckId`         | `uuid`        | —         | NULLABLE, FK → Trucks.Id (ClientSetNull)                                              |
| `FirstName`       | `varchar`     | 100       | NOT NULL                                                                              |
| `LastName`        | `varchar`     | 100       | NOT NULL                                                                              |
| `Email`           | `varchar`     | 200       | NULLABLE                                                                              |
| `Phone`           | `varchar`     | 50        | NULLABLE                                                                              |
| `LicenseNumber`   | `varchar`     | 100       | NULLABLE                                                                              |
| `LicenseState`    | `varchar`     | 50        | NULLABLE                                                                              |
| `Status`          | `int`         | —         | NOT NULL, DEFAULT 0 (enum: Available=0, Assigned=1, Driving=2, OffDuty=3, Inactive=4) |
| `CreatedAt`       | `timestamptz` | —         | NOT NULL                                                                              |
| `UpdatedAt`       | `timestamptz` | —         | NOT NULL                                                                              |
| `CreatedByUserId` | `uuid`        | —         | NULLABLE                                                                              |
| `UpdatedByUserId` | `uuid`        | —         | NULLABLE                                                                              |
| `IsDeleted`       | `boolean`     | —         | NOT NULL, DEFAULT false                                                               |

**Indexes:** `CarrierId`, `Status`

---

### Table: Loads

Core operations table — dispatch loads.

| Column              | Type            | MaxLength | Constraints                                                                                                                                             |
| ------------------- | --------------- | --------- | ------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `Id`                | `uuid`          | —         | PK                                                                                                                                                      |
| `LoadNumber`        | `varchar`       | 20        | NOT NULL, UNIQUE                                                                                                                                        |
| `CarrierId`         | `uuid`          | —         | NOT NULL, FK → Carriers.Id (Restrict)                                                                                                                   |
| `TruckId`           | `uuid`          | —         | NULLABLE, FK → Trucks.Id (ClientSetNull)                                                                                                                |
| `DriverId`          | `uuid`          | —         | NULLABLE, FK → Drivers.Id (ClientSetNull)                                                                                                               |
| `BrokerId`          | `uuid`          | —         | NULLABLE, FK → Brokers.Id (ClientSetNull)                                                                                                               |
| `DispatcherId`      | `uuid`          | —         | NULLABLE (no FK constraint)                                                                                                                             |
| `EquipmentType`     | `int`           | —         | NOT NULL (enum: DryVan=0..PowerOnly=6)                                                                                                                  |
| `PickupCity`        | `varchar`       | 100       | NOT NULL                                                                                                                                                |
| `PickupState`       | `varchar`       | 50        | NOT NULL                                                                                                                                                |
| `PickupDateTime`    | `timestamptz`   | —         | NOT NULL                                                                                                                                                |
| `DeliveryCity`      | `varchar`       | 100       | NOT NULL                                                                                                                                                |
| `DeliveryState`     | `varchar`       | 50        | NOT NULL                                                                                                                                                |
| `DeliveryDateTime`  | `timestamptz`   | —         | NOT NULL                                                                                                                                                |
| `Rate`              | `decimal(12,2)` | —         | NOT NULL                                                                                                                                                |
| `Miles`             | `int`           | —         | NULLABLE                                                                                                                                                |
| `RatePerMile`       | `decimal(8,2)`  | —         | NULLABLE                                                                                                                                                |
| `DispatchFeeType`   | `varchar`       | 50        | NULLABLE ("Percentage" or "Flat")                                                                                                                       |
| `DispatchFeeValue`  | `decimal(12,2)` | —         | NULLABLE                                                                                                                                                |
| `DispatchFeeAmount` | `decimal(12,2)` | —         | NULLABLE (calculated)                                                                                                                                   |
| `CarrierNetAmount`  | `decimal(12,2)` | —         | NULLABLE (calculated)                                                                                                                                   |
| `Status`            | `int`           | —         | NOT NULL, DEFAULT 0 (enum: Available=0, Negotiating=1, Booked=2, Dispatched=3, PickedUp=4, InTransit=5, Delivered=6, Completed=7, Cancelled=8, Issue=9) |
| `BookedAt`          | `timestamptz`   | —         | NULLABLE                                                                                                                                                |
| `PickedUpAt`        | `timestamptz`   | —         | NULLABLE                                                                                                                                                |
| `DeliveredAt`       | `timestamptz`   | —         | NULLABLE                                                                                                                                                |
| `CompletedAt`       | `timestamptz`   | —         | NULLABLE                                                                                                                                                |
| `CreatedAt`         | `timestamptz`   | —         | NOT NULL                                                                                                                                                |
| `UpdatedAt`         | `timestamptz`   | —         | NOT NULL                                                                                                                                                |
| `CreatedByUserId`   | `uuid`          | —         | NULLABLE                                                                                                                                                |
| `UpdatedByUserId`   | `uuid`          | —         | NULLABLE                                                                                                                                                |
| `IsDeleted`         | `boolean`       | —         | NOT NULL, DEFAULT false                                                                                                                                 |

**Indexes:** `LoadNumber` (unique), `Status`, `CarrierId`

**Relationships:** Has many Documents, LoadNotes. Belongs to Carrier (required), Truck (optional), Driver (optional), Broker (optional).

---

### Table: LoadNotes

Notes attached to loads.

| Column            | Type          | MaxLength | Constraints                       |
| ----------------- | ------------- | --------- | --------------------------------- |
| `Id`              | `uuid`        | —         | PK                                |
| `LoadId`          | `uuid`        | —         | NOT NULL, FK → Loads.Id (Cascade) |
| `Content`         | `varchar`     | 5000      | NOT NULL                          |
| `CreatedAt`       | `timestamptz` | —         | NOT NULL                          |
| `UpdatedAt`       | `timestamptz` | —         | NOT NULL                          |
| `CreatedByUserId` | `uuid`        | —         | NULLABLE                          |
| `UpdatedByUserId` | `uuid`        | —         | NULLABLE                          |
| `IsDeleted`       | `boolean`     | —         | NOT NULL, DEFAULT false           |

---

### Table: Brokers

Broker companies that provide loads.

| Column            | Type          | MaxLength | Constraints             |
| ----------------- | ------------- | --------- | ----------------------- |
| `Id`              | `uuid`        | —         | PK                      |
| `CompanyName`     | `varchar`     | 200       | NOT NULL                |
| `ContactName`     | `varchar`     | 200       | NOT NULL                |
| `Email`           | `varchar`     | 200       | NOT NULL                |
| `Phone`           | `varchar`     | 50        | NOT NULL                |
| `McNumber`        | `varchar`     | 50        | NULLABLE                |
| `Address`         | `varchar`     | 500       | NULLABLE                |
| `InternalRating`  | `int`         | —         | NULLABLE (1-5 scale)    |
| `PaymentNotes`    | `varchar`     | 2000      | NULLABLE                |
| `GeneralNotes`    | `varchar`     | 2000      | NULLABLE                |
| `IsActive`        | `boolean`     | —         | NOT NULL, DEFAULT true  |
| `CreatedAt`       | `timestamptz` | —         | NOT NULL                |
| `UpdatedAt`       | `timestamptz` | —         | NOT NULL                |
| `CreatedByUserId` | `uuid`        | —         | NULLABLE                |
| `UpdatedByUserId` | `uuid`        | —         | NULLABLE                |
| `IsDeleted`       | `boolean`     | —         | NOT NULL, DEFAULT false |

**Relationships:** Has many Loads.

---

### Table: Documents

File metadata for uploaded documents (stored in wwwroot/uploads).

| Column             | Type          | MaxLength | Constraints                                                                                                                        |
| ------------------ | ------------- | --------- | ---------------------------------------------------------------------------------------------------------------------------------- |
| `Id`               | `uuid`        | —         | PK                                                                                                                                 |
| `FileName`         | `varchar`     | 500       | NOT NULL                                                                                                                           |
| `StoredFileName`   | `varchar`     | 500       | NOT NULL                                                                                                                           |
| `FileUrl`          | `varchar`     | 2000      | NOT NULL                                                                                                                           |
| `ContentType`      | `varchar`     | 100       | NOT NULL                                                                                                                           |
| `FileSize`         | `bigint`      | —         | NOT NULL                                                                                                                           |
| `DocumentType`     | `int`         | —         | NOT NULL (enum: Insurance=0, W9=1, MC_Authority=2, RateConfirmation=3, BOL=4, POD=5, CarrierAgreement=6, DriverLicense=7, Other=8) |
| `CarrierId`        | `uuid`        | —         | NULLABLE, FK → Carriers.Id (ClientSetNull)                                                                                         |
| `LoadId`           | `uuid`        | —         | NULLABLE, FK → Loads.Id (ClientSetNull)                                                                                            |
| `DriverId`         | `uuid`        | —         | NULLABLE, FK → Drivers.Id (ClientSetNull)                                                                                          |
| `UploadedByUserId` | `uuid`        | —         | NULLABLE                                                                                                                           |
| `ExpiresAt`        | `timestamptz` | —         | NULLABLE                                                                                                                           |
| `CreatedAt`        | `timestamptz` | —         | NOT NULL                                                                                                                           |
| `UpdatedAt`        | `timestamptz` | —         | NOT NULL                                                                                                                           |
| `CreatedByUserId`  | `uuid`        | —         | NULLABLE                                                                                                                           |
| `UpdatedByUserId`  | `uuid`        | —         | NULLABLE                                                                                                                           |
| `IsDeleted`        | `boolean`     | —         | NOT NULL, DEFAULT false                                                                                                            |

**Indexes:** `CarrierId`, `LoadId`, `DriverId`

---

### Table: Notifications

In-app notifications for dashboard users.

| Column            | Type          | MaxLength | Constraints                                                                                                                     |
| ----------------- | ------------- | --------- | ------------------------------------------------------------------------------------------------------------------------------- |
| `Id`              | `uuid`        | —         | PK                                                                                                                              |
| `UserId`          | `uuid`        | —         | NOT NULL                                                                                                                        |
| `Type`            | `int`         | —         | NOT NULL (enum: NewApplication=0, NewMessage=1, LoadStatusChanged=2, DocumentExpiring=3, DocumentUploaded=4, CarrierAssigned=5) |
| `Title`           | `varchar`     | 200       | NOT NULL                                                                                                                        |
| `Message`         | `varchar`     | 2000      | NOT NULL                                                                                                                        |
| `EntityType`      | `varchar`     | 100       | NULLABLE                                                                                                                        |
| `EntityId`        | `uuid`        | —         | NULLABLE                                                                                                                        |
| `IsRead`          | `boolean`     | —         | NOT NULL, DEFAULT false                                                                                                         |
| `CreatedAt`       | `timestamptz` | —         | NOT NULL                                                                                                                        |
| `UpdatedAt`       | `timestamptz` | —         | NOT NULL                                                                                                                        |
| `CreatedByUserId` | `uuid`        | —         | NULLABLE                                                                                                                        |
| `UpdatedByUserId` | `uuid`        | —         | NULLABLE                                                                                                                        |
| `IsDeleted`       | `boolean`     | —         | NOT NULL, DEFAULT false                                                                                                         |

**Indexes:** `UserId`, `IsRead`

---

### Table: Conversations

Live chat sessions between website visitors and admin.

| Column             | Type          | MaxLength | Constraints             |
| ------------------ | ------------- | --------- | ----------------------- |
| `Id`               | `uuid`        | —         | PK                      |
| `VisitorId`        | `varchar`     | 100       | NOT NULL                |
| `VisitorName`      | `varchar`     | 200       | NOT NULL                |
| `VisitorEmail`     | `varchar`     | 200       | NULLABLE                |
| `VisitorPhone`     | `varchar`     | 50        | NULLABLE                |
| `AssignedToUserId` | `uuid`        | —         | NULLABLE                |
| `IsActive`         | `boolean`     | —         | NOT NULL, DEFAULT true  |
| `StartedAt`        | `timestamptz` | —         | NOT NULL                |
| `LastMessageAt`    | `timestamptz` | —         | NULLABLE                |
| `CreatedAt`        | `timestamptz` | —         | NOT NULL                |
| `UpdatedAt`        | `timestamptz` | —         | NOT NULL                |
| `CreatedByUserId`  | `uuid`        | —         | NULLABLE                |
| `UpdatedByUserId`  | `uuid`        | —         | NULLABLE                |
| `IsDeleted`        | `boolean`     | —         | NOT NULL, DEFAULT false |

**Indexes:** `VisitorId`, `IsActive`

**Relationships:** Has many Messages.

---

### Table: Messages

Individual chat messages within conversations.

| Column            | Type          | MaxLength | Constraints                                                 |
| ----------------- | ------------- | --------- | ----------------------------------------------------------- |
| `Id`              | `uuid`        | —         | PK                                                          |
| `ConversationId`  | `uuid`        | —         | NOT NULL, FK → Conversations.Id (Cascade)                   |
| `SenderType`      | `int`         | —         | NOT NULL (enum: Visitor=0, Admin=1, Dispatcher=2, System=3) |
| `SenderUserId`    | `uuid`        | —         | NULLABLE                                                    |
| `Content`         | `varchar`     | 5000      | NOT NULL                                                    |
| `IsRead`          | `boolean`     | —         | NOT NULL, DEFAULT false                                     |
| `CreatedAt`       | `timestamptz` | —         | NOT NULL                                                    |
| `UpdatedAt`       | `timestamptz` | —         | NOT NULL                                                    |
| `CreatedByUserId` | `uuid`        | —         | NULLABLE                                                    |
| `UpdatedByUserId` | `uuid`        | —         | NULLABLE                                                    |
| `IsDeleted`       | `boolean`     | —         | NOT NULL, DEFAULT false                                     |

**Indexes:** `ConversationId`

---

### Table: Invoices

Billing invoices for carriers.

| Column            | Type            | MaxLength | Constraints                                                                                  |
| ----------------- | --------------- | --------- | -------------------------------------------------------------------------------------------- |
| `Id`              | `uuid`          | —         | PK                                                                                           |
| `InvoiceNumber`   | `varchar`       | 30        | NOT NULL, UNIQUE                                                                             |
| `CarrierId`       | `uuid`          | —         | NOT NULL, FK → Carriers.Id (Restrict)                                                        |
| `PeriodStart`     | `timestamptz`   | —         | NOT NULL                                                                                     |
| `PeriodEnd`       | `timestamptz`   | —         | NOT NULL                                                                                     |
| `Subtotal`        | `decimal(12,2)` | —         | NOT NULL                                                                                     |
| `TaxAmount`       | `decimal(12,2)` | —         | NOT NULL                                                                                     |
| `TotalAmount`     | `decimal(12,2)` | —         | NOT NULL                                                                                     |
| `Status`          | `int`           | —         | NOT NULL, DEFAULT 0 (enum: Draft=0, Sent=1, PartiallyPaid=2, Paid=3, Overdue=4, Cancelled=5) |
| `DueDate`         | `timestamptz`   | —         | NULLABLE                                                                                     |
| `PaidAt`          | `timestamptz`   | —         | NULLABLE                                                                                     |
| `CreatedAt`       | `timestamptz`   | —         | NOT NULL                                                                                     |
| `UpdatedAt`       | `timestamptz`   | —         | NOT NULL                                                                                     |
| `CreatedByUserId` | `uuid`          | —         | NULLABLE                                                                                     |
| `UpdatedByUserId` | `uuid`          | —         | NULLABLE                                                                                     |
| `IsDeleted`       | `boolean`       | —         | NOT NULL, DEFAULT false                                                                      |

**Indexes:** `InvoiceNumber` (unique), `CarrierId`, `Status`

**Relationships:** Has many InvoiceItems, Payments. Belongs to Carrier (required).

---

### Table: InvoiceItems

Line items on invoices.

| Column            | Type            | MaxLength | Constraints                             |
| ----------------- | --------------- | --------- | --------------------------------------- |
| `Id`              | `uuid`          | —         | PK                                      |
| `InvoiceId`       | `uuid`          | —         | NOT NULL, FK → Invoices.Id (Cascade)    |
| `LoadId`          | `uuid`          | —         | NULLABLE, FK → Loads.Id (ClientSetNull) |
| `Description`     | `varchar`       | 500       | NOT NULL                                |
| `Quantity`        | `int`           | —         | NOT NULL, DEFAULT 1                     |
| `UnitPrice`       | `decimal(12,2)` | —         | NOT NULL                                |
| `Amount`          | `decimal(12,2)` | —         | NOT NULL                                |
| `CreatedAt`       | `timestamptz`   | —         | NOT NULL                                |
| `UpdatedAt`       | `timestamptz`   | —         | NOT NULL                                |
| `CreatedByUserId` | `uuid`          | —         | NULLABLE                                |
| `UpdatedByUserId` | `uuid`          | —         | NULLABLE                                |
| `IsDeleted`       | `boolean`       | —         | NOT NULL, DEFAULT false                 |

---

### Table: Payments

Payment records against invoices.

| Column                 | Type            | MaxLength | Constraints                                                              |
| ---------------------- | --------------- | --------- | ------------------------------------------------------------------------ |
| `Id`                   | `uuid`          | —         | PK                                                                       |
| `InvoiceId`            | `uuid`          | —         | NOT NULL, FK → Invoices.Id (Cascade)                                     |
| `Amount`               | `decimal(12,2)` | —         | NOT NULL                                                                 |
| `PaymentMethod`        | `varchar`       | 100       | NULLABLE                                                                 |
| `TransactionReference` | `varchar`       | 500       | NULLABLE                                                                 |
| `Status`               | `int`           | —         | NOT NULL, DEFAULT 0 (enum: Pending=0, Completed=1, Failed=2, Refunded=3) |
| `PaidAt`               | `timestamptz`   | —         | NULLABLE                                                                 |
| `CreatedAt`            | `timestamptz`   | —         | NOT NULL                                                                 |
| `UpdatedAt`            | `timestamptz`   | —         | NOT NULL                                                                 |
| `CreatedByUserId`      | `uuid`          | —         | NULLABLE                                                                 |
| `UpdatedByUserId`      | `uuid`          | —         | NULLABLE                                                                 |
| `IsDeleted`            | `boolean`       | —         | NOT NULL, DEFAULT false                                                  |

**Indexes:** `InvoiceId`

---

### Table: ActivityLogs

Audit trail for all important actions.

| Column            | Type          | MaxLength | Constraints             |
| ----------------- | ------------- | --------- | ----------------------- |
| `Id`              | `uuid`        | —         | PK                      |
| `UserId`          | `uuid`        | —         | NULLABLE                |
| `Action`          | `varchar`     | 100       | NOT NULL                |
| `EntityType`      | `varchar`     | 100       | NOT NULL                |
| `EntityId`        | `uuid`        | —         | NOT NULL                |
| `Description`     | `varchar`     | 2000      | NOT NULL                |
| `OldValuesJson`   | `jsonb`       | —         | NULLABLE                |
| `NewValuesJson`   | `jsonb`       | —         | NULLABLE                |
| `IpAddress`       | `varchar`     | 50        | NULLABLE                |
| `CreatedAt`       | `timestamptz` | —         | NOT NULL                |
| `UpdatedAt`       | `timestamptz` | —         | NOT NULL                |
| `CreatedByUserId` | `uuid`        | —         | NULLABLE                |
| `UpdatedByUserId` | `uuid`        | —         | NULLABLE                |
| `IsDeleted`       | `boolean`     | —         | NOT NULL, DEFAULT false |

**Indexes:** `EntityType`, `EntityId`

---

### Table: RefreshTokens

JWT refresh tokens for authentication.

| Column      | Type          | MaxLength | Constraints             |
| ----------- | ------------- | --------- | ----------------------- |
| `Id`        | `uuid`        | —         | PK                      |
| `Token`     | `varchar`     | 200       | NOT NULL, UNIQUE        |
| `UserId`    | `uuid`        | —         | NOT NULL                |
| `ExpiresAt` | `timestamptz` | —         | NOT NULL                |
| `IsRevoked` | `boolean`     | —         | NOT NULL, DEFAULT false |
| `CreatedAt` | `timestamptz` | —         | NOT NULL, DEFAULT now() |

**Indexes:** `Token` (unique), `UserId`

**Note:** This table does NOT inherit from BaseEntity. It has its own schema.

---

### ASP.NET Identity Tables

Managed by ASP.NET Core Identity (not custom entities):

| Table              | Purpose                                                                                           |
| ------------------ | ------------------------------------------------------------------------------------------------- |
| `AspNetUsers`      | Extended with: `FirstName`, `LastName`, `ProfileImageUrl`, `IsActive`, `LastLoginAt`, `CreatedAt` |
| `AspNetRoles`      | SuperAdmin, Admin, DispatchManager, Dispatcher, Sales                                             |
| `AspNetUserRoles`  | User-Role mapping                                                                                 |
| `AspNetUserClaims` | Identity claims                                                                                   |
| `AspNetUserLogins` | External logins                                                                                   |
| `AspNetUserTokens` | Reset tokens, 2FA tokens                                                                          |
| `AspNetRoleClaims` | Role claims                                                                                       |

---

### Entity Relationship Diagram

```
APPLICATIONS ──────1:1────── CARRIERS
     │                            │
     │                            ├── 1:N ── TRUCKS
     │                            │
     │                            ├── 1:N ── DRIVERS
     │                            │
     │                            ├── 1:N ── LOADS ──────── 1:N ── LOAD_NOTES
     │                            │              │
     │                            │              ├── N:1 ── BROKERS
     │                            │              └── 1:N ── DOCUMENTS
     │                            │
     │                            ├── 1:N ── DOCUMENTS
     │                            │
     │                            ├── 1:N ── CARRIER_NOTES
     │                            │
     │                            └── 1:N ── INVOICES ──── 1:N ── INVOICE_ITEMS
     │                                            │
     │                                            └── 1:N ── PAYMENTS
     │
     └── 1:N ── APPLICATION_NOTES

CONVERSATIONS ──── 1:N ── MESSAGES

USERS ──────── 1:N ── NOTIFICATIONS
         ──────── 1:N ── REFRESH_TOKENS
         ──────── 1:N ── ACTIVITY_LOGS
```

---

### Database Statistics

| Metric                  | Count                         |
| ----------------------- | ----------------------------- |
| **Total Tables**        | 21 (17 business + 4 Identity) |
| **Total Columns**       | ~200                          |
| **Indexes**             | 25+                           |
| **Foreign Keys**        | 18                            |
| **Enums**               | 12                            |
| **Soft-deleted Tables** | 17 (all BaseEntity-derived)   |

### Migration History

| Migration                        | Date       | Description                                            |
| -------------------------------- | ---------- | ------------------------------------------------------ |
| `InitialCreate`                  | 2026-08-28 | All tables, relationships, indexes                     |
| `FixFluentValidationAndSecurity` | 2026-08-28 | Code-level fixes (FluentValidation, SignalR JWT, etc.) |

# Driventa API Documentation

> Backend API for the Driventa Dispatch Management System

---

## Table of Contents

1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Getting Started](#getting-started)
4. [Authentication](#authentication)
5. [Roles & Authorization](#roles--authorization)
6. [API Endpoints](#api-endpoints)
   - [Auth](#auth-controller)
   - [Applications](#applications-controller)
   - [Carriers](#carriers-controller)
   - [Trucks](#trucks-controller)
   - [Drivers](#drivers-controller)
   - [Loads](#loads-controller)
   - [Brokers](#brokers-controller)
   - [Documents](#documents-controller)
   - [Billing](#billing-controller)
   - [Dashboard](#dashboard-controller)
   - [Reports](#reports-controller)
   - [Messages](#messages-controller)
   - [Public Applications](#public-applications-controller)
7. [Real-time (SignalR)](#real-time-signalr)
8. [Data Models](#data-models)
9. [Enums Reference](#enums-reference)
10. [DTOs Reference](#dtos-reference)
11. [Error Handling](#error-handling)
12. [Validation Rules](#validation-rules)
13. [Configuration](#configuration)

---

## Overview

| Property       | Value                                           |
| -------------- | ----------------------------------------------- |
| **Name**       | Driventa API                                    |
| **Version**    | v1                                              |
| **Framework**  | ASP.NET Core 10.0                               |
| **Database**   | PostgreSQL 16 (via Npgsql + EF Core 10.0.11)    |
| **Auth**       | JWT Bearer + ASP.NET Core Identity (Guid-based) |
| **Real-time**  | SignalR (Chat + Notifications)                  |
| **Validation** | FluentValidation 11.12.0                        |
| **API Docs**   | Swashbuckle/Swagger 7.3.1                       |

---

## Architecture

Clean Architecture with 4 layers:

```
Driventa.API                    (Presentation - Controllers, Hubs, Middleware)
    └── Driventa.Infrastructure (Data Access - DbContext, Identity, Repositories, Services)
            └── Driventa.Application (Business Logic - DTOs, Interfaces, Validators)
                    └── Driventa.Domain (Core - Entities, Enums, Common)
```

### Key Design Decisions

- **AppDbContext** injected directly into controllers (no service layer abstraction)
- **Repository pattern** registered in DI but unused by controllers (available for future use)
- **Soft delete** via `IsDeleted` flag on all entities (BaseEntity)
- **Audit trail** — `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId` auto-stamped
- **Activity logging** — Create/Update/Delete operations logged to `ActivityLogs` table

---

## Getting Started

### Prerequisites

- .NET 10 SDK
- PostgreSQL (running on localhost:5432)
- Database: `driventa_db`

### Setup

1. Clone the repository
2. Update connection string in `appsettings.json` if needed
3. Run the API:
   ```bash
   cd Driventa.API
   dotnet run
   ```
4. Migrations auto-apply on startup
5. Roles and SuperAdmin user auto-seed

### Default Credentials

| Field    | Value                |
| -------- | -------------------- |
| Email    | `admin@driventa.com` |
| Password | `Admin@123`          |
| Role     | SuperAdmin           |

### Swagger UI

Open: `http://localhost:5165/swagger/index.html`

---

## Authentication

### Login

```
POST /api/Auth/login
```

**Request:**

```json
{
  "email": "admin@driventa.com",
  "password": "Admin@123"
}
```

**Response:**

```json
{
  "success": true,
  "message": null,
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIs...",
    "refreshToken": "eyJhbGciOiJIUzI1NiIs...",
    "expiresAt": "2026-08-28T15:30:00Z",
    "userProfile": {
      "id": "guid",
      "firstName": "Super",
      "lastName": "Admin",
      "email": "admin@driventa.com",
      "phoneNumber": null,
      "role": "SuperAdmin"
    }
  }
}
```

### Using the Token

Add to request header:

```
Authorization: Bearer <your-jwt-token>
```

### Token Lifecycle

| Setting              | Value      |
| -------------------- | ---------- |
| Access Token Expiry  | 15 minutes |
| Refresh Token Expiry | 7 days     |
| Clock Skew           | 1 minute   |

### Refresh Token

```
POST /api/Auth/refresh
```

```json
{
  "refreshToken": "<your-refresh-token>"
}
```

### Password Requirements

- Minimum 8 characters
- At least 1 uppercase letter
- At least 1 lowercase letter
- At least 1 digit
- Special characters optional

---

## Roles & Authorization

### Available Roles

| Role                | Description                                                                 |
| ------------------- | --------------------------------------------------------------------------- |
| **SuperAdmin**      | Full access to everything                                                   |
| **Admin**           | Manage applications, carriers, loads, trucks, drivers, finance, reports     |
| **DispatchManager** | Manage applications, carriers, loads, trucks, drivers, view brokers/reports |
| **Dispatcher**      | Manage applications, loads, view reports                                    |
| **Sales**           | Registered role (no specific authorization policies mapped)                 |

### Authorization Policies

| Policy                  | Allowed Roles                                  |
| ----------------------- | ---------------------------------------------- |
| `CanManageApplications` | SuperAdmin, Admin, DispatchManager, Dispatcher |
| `CanManageCarriers`     | SuperAdmin, Admin, DispatchManager             |
| `CanManageLoads`        | SuperAdmin, Admin, DispatchManager, Dispatcher |
| `CanManageTrucks`       | SuperAdmin, Admin, DispatchManager             |
| `CanManageDrivers`      | SuperAdmin, Admin, DispatchManager             |
| `CanViewBrokers`        | SuperAdmin, Admin, DispatchManager             |
| `CanManageFinance`      | SuperAdmin, Admin                              |
| `CanViewReports`        | SuperAdmin, Admin, DispatchManager, Dispatcher |
| `CanManageSettings`     | SuperAdmin                                     |
| `CanAssignDispatchers`  | SuperAdmin, Admin, DispatchManager             |

---

## API Endpoints

### Standard Response Format

All endpoints return:

```json
{
  "success": true,
  "message": "Optional message",
  "data": { ... },
  "errors": null
}
```

### Pagination

Paginated endpoints accept `page` and `pageSize` query parameters and return:

```json
{
  "items": [...],
  "page": 1,
  "pageSize": 20,
  "totalCount": 150,
  "totalPages": 8,
  "hasPrevious": false,
  "hasNext": true
}
```

---

### Auth Controller

**Route:** `api/Auth`

| Method | Endpoint                    | Auth | Description               |
| ------ | --------------------------- | ---- | ------------------------- |
| POST   | `/api/Auth/login`           | No   | Login and get JWT tokens  |
| POST   | `/api/Auth/register`        | No   | Register new user         |
| POST   | `/api/Auth/refresh`         | No   | Refresh access token      |
| POST   | `/api/Auth/logout`          | Yes  | Revoke refresh token      |
| GET    | `/api/Auth/me`              | Yes  | Get current user profile  |
| POST   | `/api/Auth/forgot-password` | No   | Request password reset    |
| POST   | `/api/Auth/reset-password`  | No   | Reset password with token |

#### POST `/api/Auth/register`

**Request:**

```json
{
  "firstName": "John",
  "lastName": "Doe",
  "email": "john@example.com",
  "password": "Password123!",
  "confirmPassword": "Password123!",
  "phoneNumber": "+1234567890",
  "role": "Dispatcher"
}
```

#### POST `/api/Auth/forgot-password`

**Request:**

```json
{
  "email": "john@example.com"
}
```

#### POST `/api/Auth/reset-password`

**Request:**

```json
{
  "email": "john@example.com",
  "token": "<reset-token>",
  "newPassword": "NewPassword123!",
  "confirmPassword": "NewPassword123!"
}
```

---

### Applications Controller

**Route:** `api/Applications`

| Method | Endpoint                         | Auth                              | Description                                   |
| ------ | -------------------------------- | --------------------------------- | --------------------------------------------- |
| GET    | `/api/Applications`              | Authorize                         | List all applications (paginated, filterable) |
| POST   | `/api/Applications`              | Authorize (CanManageApplications) | Create new application                        |
| GET    | `/api/Applications/{id}`         | Authorize                         | Get application by ID                         |
| PATCH  | `/api/Applications/{id}`         | Authorize (CanManageApplications) | Update application                            |
| DELETE | `/api/Applications/{id}`         | Authorize (SuperAdmin, Admin)     | Soft delete application                       |
| POST   | `/api/Applications/{id}/assign`  | Authorize (CanManageApplications) | Assign to user                                |
| POST   | `/api/Applications/{id}/contact` | Authorize (CanManageApplications) | Mark as contacted                             |
| POST   | `/api/Applications/{id}/approve` | Authorize (CanManageApplications) | Approve application                           |
| POST   | `/api/Applications/{id}/reject`  | Authorize (CanManageApplications) | Reject application                            |
| POST   | `/api/Applications/{id}/convert` | Authorize (CanManageApplications) | Convert to carrier                            |
| GET    | `/api/Applications/{id}/notes`   | Authorize                         | Get application notes                         |
| POST   | `/api/Applications/{id}/notes`   | Authorize (CanManageApplications) | Add note                                      |

#### Query Parameters (GET `/api/Applications`)

| Parameter  | Type               | Default | Description                    |
| ---------- | ------------------ | ------- | ------------------------------ |
| `page`     | int                | 1       | Page number                    |
| `pageSize` | int                | 20      | Items per page                 |
| `search`   | string?            | null    | Search by name, email, company |
| `status`   | ApplicationStatus? | null    | Filter by status               |

#### POST `/api/Applications` — Create

**Request:**

```json
{
  "fullName": "John Doe",
  "email": "john@example.com",
  "phone": "+1234567890",
  "companyName": "Doe Trucking LLC",
  "equipmentType": 0,
  "truckCount": 5,
  "mcNumber": "MC123456",
  "dotNumber": "DOT789012",
  "preferredLanes": "TX to CA",
  "additionalDetails": "Experienced carrier"
}
```

#### POST `/api/Applications/{id}/convert` — Convert to Carrier

**Request:**

```json
{
  "assignedDispatcherId": "guid-or-null",
  "notes": "Optional notes"
}
```

---

### Carriers Controller

**Route:** `api/Carriers`

| Method | Endpoint                               | Auth                             | Description                               |
| ------ | -------------------------------------- | -------------------------------- | ----------------------------------------- |
| GET    | `/api/Carriers`                        | Authorize                        | List all carriers (paginated, filterable) |
| POST   | `/api/Carriers`                        | Authorize (CanManageCarriers)    | Create new carrier                        |
| GET    | `/api/Carriers/{id}`                   | Authorize                        | Get carrier by ID                         |
| PATCH  | `/api/Carriers/{id}`                   | Authorize (CanManageCarriers)    | Update carrier                            |
| DELETE | `/api/Carriers/{id}`                   | Authorize (SuperAdmin, Admin)    | Soft delete carrier                       |
| POST   | `/api/Carriers/{id}/assign-dispatcher` | Authorize (CanAssignDispatchers) | Assign dispatcher                         |
| GET    | `/api/Carriers/{id}/notes`             | Authorize                        | Get carrier notes                         |
| POST   | `/api/Carriers/{id}/notes`             | Authorize (CanManageCarriers)    | Add note                                  |
| GET    | `/api/Carriers/{id}/trucks`            | Authorize                        | Get carrier's trucks                      |
| GET    | `/api/Carriers/{id}/drivers`           | Authorize                        | Get carrier's drivers                     |
| GET    | `/api/Carriers/{id}/loads`             | Authorize                        | Get carrier's loads                       |

#### Query Parameters (GET `/api/Carriers`)

| Parameter  | Type           | Default | Description                       |
| ---------- | -------------- | ------- | --------------------------------- |
| `page`     | int            | 1       | Page number                       |
| `pageSize` | int            | 20      | Items per page                    |
| `search`   | string?        | null    | Search by company, contact, email |
| `status`   | CarrierStatus? | null    | Filter by status                  |

#### POST `/api/Carriers` — Create

**Request:**

```json
{
  "companyName": "ABC Transport",
  "contactName": "John Smith",
  "email": "john@abctransport.com",
  "phone": "+1234567890",
  "mcNumber": "MC654321",
  "dotNumber": "DOT210987",
  "addressLine1": "123 Main St",
  "city": "Dallas",
  "state": "TX",
  "zipCode": "75201",
  "preferredLanes": "TX, CA, FL",
  "notes": "Reliable carrier",
  "applicationId": "guid-or-null"
}
```

---

### Trucks Controller

**Route:** `api/Trucks`

| Method | Endpoint           | Auth                        | Description                             |
| ------ | ------------------ | --------------------------- | --------------------------------------- |
| GET    | `/api/Trucks`      | Authorize                   | List all trucks (paginated, filterable) |
| POST   | `/api/Trucks`      | Authorize (CanManageTrucks) | Create new truck                        |
| GET    | `/api/Trucks/{id}` | Authorize                   | Get truck by ID                         |
| PATCH  | `/api/Trucks/{id}` | Authorize                   | Update truck                            |

#### Query Parameters (GET `/api/Trucks`)

| Parameter   | Type         | Default | Description                         |
| ----------- | ------------ | ------- | ----------------------------------- |
| `page`      | int          | 1       | Page number                         |
| `pageSize`  | int          | 20      | Items per page                      |
| `search`    | string?      | null    | Search by truck number, make, model |
| `carrierId` | Guid?        | null    | Filter by carrier                   |
| `status`    | TruckStatus? | null    | Filter by status                    |

#### POST `/api/Trucks` — Create

**Request:**

```json
{
  "carrierId": "carrier-guid",
  "truckNumber": "T-001",
  "equipmentType": 0,
  "make": "Freightliner",
  "model": "Cascadia",
  "year": 2024,
  "licensePlate": "ABC1234",
  "licenseState": "TX"
}
```

---

### Drivers Controller

**Route:** `api/Drivers`

| Method | Endpoint            | Auth                          | Description                              |
| ------ | ------------------- | ----------------------------- | ---------------------------------------- |
| GET    | `/api/Drivers`      | Authorize                     | List all drivers (paginated, filterable) |
| POST   | `/api/Drivers`      | Authorize (CanManageDrivers)  | Create new driver                        |
| GET    | `/api/Drivers/{id}` | Authorize                     | Get driver by ID                         |
| PATCH  | `/api/Drivers/{id}` | Authorize                     | Update driver                            |
| DELETE | `/api/Drivers/{id}` | Authorize (SuperAdmin, Admin) | Soft delete driver                       |

#### Query Parameters (GET `/api/Drivers`)

| Parameter   | Type          | Default | Description           |
| ----------- | ------------- | ------- | --------------------- |
| `page`      | int           | 1       | Page number           |
| `pageSize`  | int           | 20      | Items per page        |
| `search`    | string?       | null    | Search by name, email |
| `carrierId` | Guid?         | null    | Filter by carrier     |
| `status`    | DriverStatus? | null    | Filter by status      |

#### POST `/api/Drivers` — Create

**Request:**

```json
{
  "carrierId": "carrier-guid",
  "truckId": "truck-guid-or-null",
  "firstName": "Mike",
  "lastName": "Johnson",
  "email": "mike@example.com",
  "phone": "+1234567890",
  "licenseNumber": "DL123456",
  "licenseState": "TX"
}
```

---

### Loads Controller

**Route:** `api/Loads`

| Method | Endpoint                 | Auth                          | Description                            |
| ------ | ------------------------ | ----------------------------- | -------------------------------------- |
| GET    | `/api/Loads`             | Authorize                     | List all loads (paginated, filterable) |
| POST   | `/api/Loads`             | Authorize (CanManageLoads)    | Create new load                        |
| GET    | `/api/Loads/{id}`        | Authorize                     | Get load by ID                         |
| PATCH  | `/api/Loads/{id}`        | Authorize (CanManageLoads)    | Update load                            |
| DELETE | `/api/Loads/{id}`        | Authorize (SuperAdmin, Admin) | Soft delete load                       |
| PATCH  | `/api/Loads/{id}/status` | Authorize (CanManageLoads)    | Update load status                     |
| GET    | `/api/Loads/{id}/notes`  | Authorize                     | Get load notes                         |
| POST   | `/api/Loads/{id}/notes`  | Authorize (CanManageLoads)    | Add note                               |

#### Query Parameters (GET `/api/Loads`)

| Parameter      | Type        | Default | Description                   |
| -------------- | ----------- | ------- | ----------------------------- |
| `page`         | int         | 1       | Page number                   |
| `pageSize`     | int         | 20      | Items per page                |
| `search`       | string?     | null    | Search by load number, cities |
| `carrierId`    | Guid?       | null    | Filter by carrier             |
| `status`       | LoadStatus? | null    | Filter by status              |
| `dispatcherId` | Guid?       | null    | Filter by dispatcher          |

#### POST `/api/Loads` — Create

**Request:**

```json
{
  "carrierId": "carrier-guid",
  "truckId": "truck-guid",
  "driverId": "driver-guid",
  "brokerId": "broker-guid",
  "equipmentType": 0,
  "pickupCity": "Dallas",
  "pickupState": "TX",
  "pickupDateTime": "2026-09-01T08:00:00Z",
  "deliveryCity": "Los Angeles",
  "deliveryState": "CA",
  "deliveryDateTime": "2026-09-03T18:00:00Z",
  "rate": 3500.0,
  "miles": 1400,
  "dispatchFeeType": "percentage",
  "dispatchFeeValue": 10
}
```

#### PATCH `/api/Loads/{id}/status` — Status Update

**Request:**

```json
{
  "status": 4,
  "notes": "Driver picked up load"
}
```

**Load Status Flow:**

```
Available → Negotiating → Booked → Dispatched → PickedUp → InTransit → Delivered → Completed
                                                                  ↓
                                                              Cancelled / Issue
```

---

### Brokers Controller

**Route:** `api/Brokers`

| Method | Endpoint            | Auth                          | Description                  |
| ------ | ------------------- | ----------------------------- | ---------------------------- |
| GET    | `/api/Brokers`      | Authorize (CanViewBrokers)    | List all brokers (paginated) |
| POST   | `/api/Brokers`      | Authorize (CanViewBrokers)    | Create new broker            |
| GET    | `/api/Brokers/{id}` | Authorize (CanViewBrokers)    | Get broker by ID             |
| PATCH  | `/api/Brokers/{id}` | Authorize (CanViewBrokers)    | Update broker                |
| DELETE | `/api/Brokers/{id}` | Authorize (SuperAdmin, Admin) | Soft delete broker           |

#### POST `/api/Brokers` — Create

**Request:**

```json
{
  "companyName": "XYZ Freight Brokers",
  "contactName": "Jane Doe",
  "email": "jane@xyzfreight.com",
  "phone": "+1234567890",
  "mcNumber": "MC111222",
  "address": "456 Broker Ave",
  "internalRating": 4,
  "paymentNotes": "Net 30",
  "generalNotes": "Reliable broker"
}
```

---

### Documents Controller

**Route:** `api/Documents`

| Method | Endpoint                | Auth                          | Description                           |
| ------ | ----------------------- | ----------------------------- | ------------------------------------- |
| POST   | `/api/Documents/upload` | Authorize                     | Upload document (multipart/form-data) |
| GET    | `/api/Documents/{id}`   | Authorize                     | Get document by ID                    |
| DELETE | `/api/Documents/{id}`   | Authorize (SuperAdmin, Admin) | Delete document                       |

#### POST `/api/Documents/upload`

**Request:** `multipart/form-data`

| Field          | Type         | Required | Description                      |
| -------------- | ------------ | -------- | -------------------------------- |
| `file`         | IFormFile    | Yes      | File to upload                   |
| `documentType` | DocumentType | Yes      | Type of document (query param)   |
| `carrierId`    | Guid?        | No       | Associated carrier (query param) |
| `loadId`       | Guid?        | No       | Associated load (query param)    |
| `driverId`     | Guid?        | No       | Associated driver (query param)  |

**Document Types:** Insurance(0), W9(1), MC_Authority(2), RateConfirmation(3), BOL(4), POD(5), CarrierAgreement(6), DriverLicense(7), Other(8)

---

### Billing Controller

**Route:** `api/Billing`

| Method | Endpoint                              | Auth                         | Description                   |
| ------ | ------------------------------------- | ---------------------------- | ----------------------------- |
| GET    | `/api/Billing/invoices`               | Authorize (CanManageFinance) | List all invoices (paginated) |
| POST   | `/api/Billing/invoices`               | Authorize (CanManageFinance) | Create invoice                |
| GET    | `/api/Billing/invoices/{id}`          | Authorize (CanManageFinance) | Get invoice by ID             |
| PATCH  | `/api/Billing/invoices/{id}/status`   | Authorize (CanManageFinance) | Update invoice status         |
| POST   | `/api/Billing/invoices/{id}/payments` | Authorize (CanManageFinance) | Record payment                |

#### POST `/api/Billing/invoices` — Create Invoice

**Request:**

```json
{
  "carrierId": "carrier-guid",
  "periodStart": "2026-08-01T00:00:00Z",
  "periodEnd": "2026-08-31T23:59:59Z",
  "taxAmount": 250.0,
  "dueDate": "2026-09-15T00:00:00Z",
  "items": [
    {
      "loadId": "load-guid",
      "Description": "Dispatch fee - Load #LD001",
      "quantity": 1,
      "unitPrice": 350.0
    },
    {
      "loadId": "load-guid",
      "Description": "Dispatch fee - Load #LD002",
      "quantity": 1,
      "unitPrice": 420.0
    }
  ]
}
```

#### POST `/api/Billing/invoices/{id}/payments` — Record Payment

**Request:**

```json
{
  "amount": 1020.0,
  "paymentMethod": "Bank Transfer",
  "transactionReference": "TXN-2026-001"
}
```

---

### Dashboard Controller

**Route:** `api/Dashboard`

| Method | Endpoint                             | Auth                         | Description                 |
| ------ | ------------------------------------ | ---------------------------- | --------------------------- |
| GET    | `/api/Dashboard/summary`             | Authorize                    | Get dashboard summary stats |
| GET    | `/api/Dashboard/load-status-summary` | Authorize                    | Get load counts by status   |
| GET    | `/api/Dashboard/recent-activity`     | Authorize                    | Get recent activity logs    |
| GET    | `/api/Dashboard/revenue-summary`     | Authorize (CanManageFinance) | Get revenue summary         |
| POST   | `/api/Dashboard/contact`             | No                           | Submit contact form         |

#### GET `/api/Dashboard/summary` — Response

```json
{
  "newApplications": 12,
  "applicationsInReview": 5,
  "activeCarriers": 48,
  "activeTrucks": 72,
  "activeLoads": 15,
  "loadsInTransit": 8,
  "completedLoadsThisMonth": 23,
  "dispatchRevenueThisMonth": 18500.0
}
```

---

### Reports Controller

**Route:** `api/Reports`

| Method | Endpoint                   | Auth                       | Description                           |
| ------ | -------------------------- | -------------------------- | ------------------------------------- |
| GET    | `/api/Reports/loads`       | Authorize (CanViewReports) | Load performance report               |
| GET    | `/api/Reports/revenue`     | Authorize (CanViewReports) | Revenue report with monthly breakdown |
| GET    | `/api/Reports/carriers`    | Authorize (CanViewReports) | Carrier performance report            |
| GET    | `/api/Reports/dispatchers` | Authorize (CanViewReports) | Dispatcher performance report         |

#### GET `/api/Reports/loads` — Response

```json
{
  "totalLoads": 150,
  "activeLoads": 23,
  "completedLoads": 110,
  "cancelledLoads": 7,
  "averageRate": 2800.0,
  "averageRpm": 2.15,
  "totalRevenue": 308000.0
}
```

#### GET `/api/Reports/revenue` — Response

```json
{
  "totalRevenue": 500000.0,
  "totalDispatchFees": 50000.0,
  "totalCarrierPayouts": 450000.0,
  "averageRevenuePerLoad": 3333.33,
  "monthlyBreakdown": [
    {
      "year": 2026,
      "month": 8,
      "revenue": 45000.0,
      "loadCount": 18
    }
  ]
}
```

---

### Messages Controller

**Route:** `api/Messages`

| Method | Endpoint                                | Auth      | Description                    |
| ------ | --------------------------------------- | --------- | ------------------------------ |
| GET    | `/api/Messages/conversations`           | Authorize | List all conversations         |
| GET    | `/api/Messages/conversations/{id}`      | Authorize | Get conversation with messages |
| POST   | `/api/Messages/send`                    | Authorize | Send a message                 |
| PATCH  | `/api/Messages/conversations/{id}/read` | Authorize | Mark conversation as read      |

#### POST `/api/Messages/send` — Request

```json
{
  "conversationId": "conversation-guid",
  "content": "Hello, how can I help you?"
}
```

---

### Public Applications Controller

**Route:** `api/public/Applications`

| Method | Endpoint                   | Auth              | Description                |
| ------ | -------------------------- | ----------------- | -------------------------- |
| POST   | `/api/public/Applications` | No (rate limited) | Submit carrier application |

**Rate Limit:** 10 requests per minute

#### POST `/api/public/Applications` — Request

```json
{
  "fullName": "John Doe",
  "email": "john@example.com",
  "phone": "+1234567890",
  "companyName": "Doe Trucking LLC",
  "equipmentType": 0,
  "truckCount": 5,
  "mcNumber": "MC123456",
  "dotNumber": "DOT789012",
  "preferredLanes": "TX to CA",
  "additionalDetails": "Experienced carrier looking for dispatch services"
}
```

---

## Real-time (SignalR)

### Chat Hub

**URL:** `ws://localhost:5165/hubs/chat`

**Authentication:** Optional (via query string `?access_token=<jwt>`)

#### Client Methods

| Method             | Parameters                                | Description               |
| ------------------ | ----------------------------------------- | ------------------------- |
| `JoinConversation` | `conversationId: string`                  | Join a conversation group |
| `SendMessage`      | `conversationId: string, message: string` | Send a message            |
| `MarkAsRead`       | `conversationId: string`                  | Mark all messages as read |

#### Server Events

| Event            | Payload                                                       | Description             |
| ---------------- | ------------------------------------------------------------- | ----------------------- |
| `ReceiveMessage` | `{ messageId, message, senderUserId, senderType, timestamp }` | New message received    |
| `MessagesRead`   | `conversationId: string`                                      | Messages marked as read |

---

### Notification Hub

**URL:** `ws://localhost:5165/hubs/notifications`

**Authentication:** Required (JWT)

#### Client Methods

| Method                   | Parameters                                           | Description                      |
| ------------------------ | ---------------------------------------------------- | -------------------------------- |
| `JoinPersonalGroup`      | None                                                 | Join personal notification group |
| `SendNotificationToUser` | `targetUserId: Guid, title: string, message: string` | Send notification to self        |

#### Server Events

| Event                 | Payload                         | Description               |
| --------------------- | ------------------------------- | ------------------------- |
| `Connected`           | `{ userId }`                    | Connection established    |
| `ReceiveNotification` | `{ title, message, timestamp }` | New notification received |

---

## Data Models

### Entity Relationship Diagram

```
Application ──1:N──> ApplicationNote
Application ──1:1──> Carrier (via ConvertedCarrierId)

Carrier ──1:N──> Truck
Carrier ──1:N──> Driver
Carrier ──1:N──> Load
Carrier ──1:N──> Document
Carrier ──1:N──> CarrierNote
Carrier ──1:N──> Invoice

Truck ──1:N──> Driver (optional assignment)
Truck ──1:N──> Load (optional assignment)

Driver ──1:N──> Load (optional assignment)
Driver ──1:N──> Document

Broker ──1:N──> Load

Load ──1:N──> Document
Load ──1:N──> LoadNote
Load ──1:N──> InvoiceItem

Invoice ──1:N──> InvoiceItem
Invoice ──1:N──> Payment

Conversation ──1:N──> Message
```

### Core Entities

#### BaseEntity

| Field           | Type           | Description           |
| --------------- | -------------- | --------------------- |
| Id              | Guid           | Primary key           |
| CreatedAt       | DateTimeOffset | Creation timestamp    |
| UpdatedAt       | DateTimeOffset | Last update timestamp |
| CreatedByUserId | Guid?          | Creator user ID       |
| UpdatedByUserId | Guid?          | Last modifier user ID |
| IsDeleted       | bool           | Soft delete flag      |

#### Application

| Field              | Type              | Description           |
| ------------------ | ----------------- | --------------------- |
| ApplicationNumber  | string(20)        | Auto-generated number |
| FullName           | string(200)       | Applicant name        |
| Email              | string(200)       | Applicant email       |
| Phone              | string(50)        | Phone number          |
| CompanyName        | string(200)       | Company name          |
| EquipmentType      | EquipmentType     | Equipment needed      |
| TruckCount         | int               | Number of trucks      |
| McNumber           | string(50)?       | MC authority number   |
| DotNumber          | string(50)?       | DOT number            |
| PreferredLanes     | string(500)?      | Preferred routes      |
| AdditionalDetails  | string(2000)?     | Extra info            |
| Status             | ApplicationStatus | Current status        |
| AssignedToUserId   | Guid?             | Assigned dispatcher   |
| SubmittedAt        | DateTimeOffset    | Submission date       |
| ContactedAt        | DateTimeOffset?   | Contact date          |
| ApprovedAt         | DateTimeOffset?   | Approval date         |
| RejectedAt         | DateTimeOffset?   | Rejection date        |
| ConvertedCarrierId | Guid?             | Linked carrier        |

#### Carrier

| Field                | Type          | Description         |
| -------------------- | ------------- | ------------------- |
| CompanyName          | string(200)   | Company name        |
| ContactName          | string(200)   | Primary contact     |
| Email                | string(200)   | Email               |
| Phone                | string(50)    | Phone               |
| McNumber             | string(50)?   | MC authority        |
| DotNumber            | string(50)?   | DOT number          |
| AddressLine1-2       | string(200)?  | Address             |
| City                 | string(100)?  | City                |
| State                | string(50)?   | State               |
| ZipCode              | string(20)?   | ZIP                 |
| Status               | CarrierStatus | Current status      |
| AssignedDispatcherId | Guid?         | Assigned dispatcher |
| PreferredLanes       | string(500)?  | Preferred routes    |
| Notes                | string(2000)? | Notes               |
| ApplicationId        | Guid?         | Linked application  |

#### Load

| Field              | Type           | Description            |
| ------------------ | -------------- | ---------------------- |
| LoadNumber         | string(20)     | Auto-generated number  |
| CarrierId          | Guid           | Assigned carrier       |
| TruckId            | Guid?          | Assigned truck         |
| DriverId           | Guid?          | Assigned driver        |
| BrokerId           | Guid?          | Broker                 |
| DispatcherId       | Guid?          | Dispatcher             |
| EquipmentType      | EquipmentType  | Required equipment     |
| PickupCity/State   | string         | Pickup location        |
| PickupDateTime     | DateTimeOffset | Pickup time            |
| DeliveryCity/State | string         | Delivery location      |
| DeliveryDateTime   | DateTimeOffset | Delivery time          |
| Rate               | decimal(12,2)  | Load rate ($)          |
| Miles              | int?           | Distance               |
| RatePerMile        | decimal(8,2)?  | Calculated RPM         |
| DispatchFeeType    | string(50)?    | "percentage" or "flat" |
| DispatchFeeValue   | decimal(12,2)? | Fee amount/rate        |
| DispatchFeeAmount  | decimal(12,2)? | Calculated fee         |
| CarrierNetAmount   | decimal(12,2)? | Calculated net         |
| Status             | LoadStatus     | Current status         |

#### Invoice

| Field           | Type            | Description           |
| --------------- | --------------- | --------------------- |
| InvoiceNumber   | string(30)      | Auto-generated number |
| CarrierId       | Guid            | Billed carrier        |
| PeriodStart/End | DateTimeOffset  | Billing period        |
| Subtotal        | decimal(12,2)   | Items total           |
| TaxAmount       | decimal(12,2)   | Tax                   |
| TotalAmount     | decimal(12,2)   | Grand total           |
| Status          | InvoiceStatus   | Current status        |
| DueDate         | DateTimeOffset? | Due date              |
| PaidAt          | DateTimeOffset? | Payment date          |

---

## Enums Reference

### ApplicationStatus

| Value | Name      |
| ----- | --------- |
| 0     | New       |
| 1     | Reviewing |
| 2     | Contacted |
| 3     | Qualified |
| 4     | Approved  |
| 5     | Rejected  |
| 6     | Onboarded |

### CarrierStatus

| Value | Name       |
| ----- | ---------- |
| 0     | Lead       |
| 1     | Onboarding |
| 2     | Active     |
| 3     | Paused     |
| 4     | Inactive   |
| 5     | Suspended  |

### LoadStatus

| Value | Name        |
| ----- | ----------- |
| 0     | Available   |
| 1     | Negotiating |
| 2     | Booked      |
| 3     | Dispatched  |
| 4     | PickedUp    |
| 5     | InTransit   |
| 6     | Delivered   |
| 7     | Completed   |
| 8     | Cancelled   |
| 9     | Issue       |

### TruckStatus

| Value | Name        |
| ----- | ----------- |
| 0     | Available   |
| 1     | OnLoad      |
| 2     | Maintenance |
| 3     | Inactive    |

### DriverStatus

| Value | Name      |
| ----- | --------- |
| 0     | Available |
| 1     | Assigned  |
| 2     | Driving   |
| 3     | OffDuty   |
| 4     | Inactive  |

### EquipmentType

| Value | Name      |
| ----- | --------- |
| 0     | DryVan    |
| 1     | Reefer    |
| 2     | Flatbed   |
| 3     | StepDeck  |
| 4     | BoxTruck  |
| 5     | Hotshot   |
| 6     | PowerOnly |

### InvoiceStatus

| Value | Name          |
| ----- | ------------- |
| 0     | Draft         |
| 1     | Sent          |
| 2     | PartiallyPaid |
| 3     | Paid          |
| 4     | Overdue       |
| 5     | Cancelled     |

### PaymentStatus

| Value | Name      |
| ----- | --------- |
| 0     | Pending   |
| 1     | Completed |
| 2     | Failed    |
| 3     | Refunded  |

### DocumentType

| Value | Name             |
| ----- | ---------------- |
| 0     | Insurance        |
| 1     | W9               |
| 2     | MC_Authority     |
| 3     | RateConfirmation |
| 4     | BOL              |
| 5     | POD              |
| 6     | CarrierAgreement |
| 7     | DriverLicense    |
| 8     | Other            |

### NotificationType

| Value | Name              |
| ----- | ----------------- |
| 0     | NewApplication    |
| 1     | NewMessage        |
| 2     | LoadStatusChanged |
| 3     | DocumentExpiring  |
| 4     | DocumentUploaded  |
| 5     | CarrierAssigned   |

### SenderType

| Value | Name       |
| ----- | ---------- |
| 0     | Visitor    |
| 1     | Admin      |
| 2     | Dispatcher |
| 3     | System     |

### UserRole

| Value | Name            |
| ----- | --------------- |
| 0     | SuperAdmin      |
| 1     | Admin           |
| 2     | DispatchManager |
| 3     | Dispatcher      |
| 4     | Sales           |

---

## Error Handling

### Exception to HTTP Status Mapping

| Exception                     | HTTP Status               | Description              |
| ----------------------------- | ------------------------- | ------------------------ |
| `ValidationException`         | 400 Bad Request           | FluentValidation failure |
| `ArgumentException`           | 400 Bad Request           | Invalid argument         |
| `KeyNotFoundException`        | 404 Not Found             | Resource not found       |
| `UnauthorizedAccessException` | 401 Unauthorized          | Auth required            |
| `InvalidOperationException`   | 409 Conflict              | Business rule violation  |
| `HubException`                | 400 Bad Request           | SignalR error            |
| Other exceptions              | 500 Internal Server Error | Unexpected error         |

### Error Response Format

```json
{
  "success": false,
  "message": "Validation failed.",
  "data": null,
  "errors": ["Email is required.", "Password must be at least 8 characters."]
}
```

---

## Validation Rules

### LoginRequest

- Email: required, valid email format, max 200 chars
- Password: required, max 100 chars

### RegisterRequest

- FirstName: required, max 100 chars
- LastName: required, max 100 chars
- Email: required, valid email, max 200 chars
- Password: required, 8-100 chars, uppercase, lowercase, digit
- ConfirmPassword: must match Password
- PhoneNumber: max 50 chars
- Role: required, max 50 chars

### CreateApplicationRequest

- FullName: required, max 200 chars
- Email: required, valid email, max 200 chars
- Phone: required, max 50 chars
- CompanyName: required, max 200 chars
- TruckCount: must be > 0

### UpdateApplicationRequest

- At least one field must be provided
- All fields optional (partial update)

### PublicApplicationRequest

- FullName: required, max 200 chars
- Email: required, valid email, max 200 chars
- Phone: required, max 50 chars
- CompanyName: required, max 200 chars
- TruckCount: must be > 0

---

## Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=driventa_db;Username=postgres;Password=zahid123"
  },
  "Jwt": {
    "SecretKey": "Driventa_SuperSecretKey_2026_!@#$%^&*()_+ThisMustBeAtLeast32Characters!!",
    "Issuer": "Driventa.API",
    "Audience": "Driventa.Dashboard",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  }
}
```

### CORS Origins

| Origin                          | Purpose                   |
| ------------------------------- | ------------------------- |
| `http://localhost:3000`         | Local development (React) |
| `http://localhost:5173`         | Local development (Vite)  |
| `https://driventa.us`           | Production website        |
| `https://dashboard.driventa.us` | Production dashboard      |

### Rate Limiting

| Policy            | Limit       | Window   | Queue                  |
| ----------------- | ----------- | -------- | ---------------------- |
| `PublicEndpoints` | 10 requests | 1 minute | 0 (reject immediately) |

### JWT Settings

| Setting              | Value                              |
| -------------------- | ---------------------------------- |
| Issuer               | `Driventa.API`                     |
| Audience             | `Driventa.Dashboard`               |
| Access Token Expiry  | 15 minutes                         |
| Refresh Token Expiry | 7 days                             |
| Clock Skew           | 1 minute                           |
| SignalR Auth         | Query string `?access_token=<jwt>` |

### Password Policy

| Rule                   | Value |
| ---------------------- | ----- |
| RequireDigit           | true  |
| RequireLowercase       | true  |
| RequireUppercase       | true  |
| RequireNonAlphanumeric | false |
| RequiredLength         | 8     |
| RequireUniqueEmail     | true  |

---

_Generated from Driventa API v1 source code_

# Driventa API Documentation

> Complete backend API documentation for the Driventa Dispatch Management System.

## 1. Overview

Driventa API is the backend for the Driventa dispatch management platform. It handles:

- User authentication and authorization
- Website carrier applications
- Carrier onboarding
- Application to carrier conversion
- Carrier management
- Truck management
- Driver management
- Load management
- Broker management
- Document uploads
- Internal notes
- Billing and invoices
- Payments
- Dashboard statistics
- Reports and analytics
- Website and dashboard messaging
- Real-time chat
- Real-time notifications
- Activity logging
- Role-based access control

### Technology Stack

| Component         | Technology                        |
| ----------------- | --------------------------------- |
| Framework         | ASP.NET Core 10                   |
| Language          | C#                                |
| Database          | PostgreSQL                        |
| ORM               | Entity Framework Core + Npgsql    |
| Authentication    | ASP.NET Core Identity + JWT       |
| Authorization     | Roles + Policies                  |
| Validation        | FluentValidation                  |
| Real-time         | SignalR                           |
| API Documentation | Swagger / OpenAPI                 |
| Database IDs      | GUID                              |
| Soft Delete       | `IsDeleted`                       |
| Audit Fields      | Automatic timestamps and user IDs |

---

# 2. Architecture

The backend follows a four-layer Clean Architecture structure:

```text
Driventa.API
│
├── Controllers
├── Hubs
├── Middleware
└── API configuration
        │
        ▼
Driventa.Infrastructure
│
├── AppDbContext
├── Identity
├── Repositories
└── Infrastructure services
        │
        ▼
Driventa.Application
│
├── DTOs
├── Interfaces
└── Validators
        │
        ▼
Driventa.Domain
│
├── Entities
├── Enums
└── Common models
```

## Layer Responsibilities

### Driventa.Domain

Contains the core business objects:

```text
Application
Carrier
Truck
Driver
Load
Broker
Document
Invoice
InvoiceItem
Payment
Conversation
Message
Notification
ActivityLog
Notes
```

This layer should not depend on the API, database, or frontend.

### Driventa.Application

Contains:

- Request DTOs
- Response DTOs
- Validators
- Interfaces
- Business contracts

### Driventa.Infrastructure

Contains infrastructure implementations:

- PostgreSQL database access
- EF Core configuration
- ASP.NET Identity
- JWT implementation
- Email service
- File storage
- Repository implementations

### Driventa.API

Contains the entry points used by the website and dashboard:

```text
Controllers
SignalR Hubs
Authentication configuration
Authorization configuration
Rate limiting
CORS
Swagger
Exception middleware
```

---

# 3. Base URL

Local development example:

```text
http://localhost:5165
```

Production should use HTTPS:

```text
https://api.driventa.us
```

Recommended production structure:

```text
https://www.driventa.us
        ↓
Public Website

https://dashboard.driventa.us
        ↓
Admin Dashboard

https://api.driventa.us
        ↓
.NET Backend API
```

---

# 4. Standard API Response Format

Successful responses follow this structure:

```json
{
  "success": true,
  "message": "Optional message",
  "data": {}
}
```

Example:

```json
{
  "success": true,
  "message": "Application created successfully.",
  "data": {
    "id": "application-guid"
  }
}
```

Error responses follow this structure:

```json
{
  "success": false,
  "message": "Validation failed.",
  "data": null,
  "errors": ["Email is required.", "Password must be at least 8 characters."]
}
```

---

# 5. Authentication

Most dashboard endpoints require a JWT access token.

Add the token to every protected request:

```http
Authorization: Bearer YOUR_ACCESS_TOKEN
```

Example:

```http
GET /api/Dashboard/summary
Authorization: Bearer eyJhbGciOi...
```

## Token Lifecycle

Recommended/current token configuration:

| Token         | Expiry     |
| ------------- | ---------- |
| Access Token  | 15 minutes |
| Refresh Token | 7 days     |
| Clock Skew    | 1 minute   |

When the access token expires:

```text
Access Token Expired
        ↓
POST /api/Auth/refresh
        ↓
Receive New Access Token
        ↓
Retry Protected Request
```

---

# 6. Roles

Available roles:

| Role            | Purpose                                 |
| --------------- | --------------------------------------- |
| SuperAdmin      | Full system access                      |
| Admin           | Administrative and operational access   |
| DispatchManager | Manages dispatch operations             |
| Dispatcher      | Handles assigned applications and loads |
| Sales           | Sales/onboarding role                   |

## Authorization Policies

| Policy                | Allowed Roles                                  |
| --------------------- | ---------------------------------------------- |
| CanManageApplications | SuperAdmin, Admin, DispatchManager, Dispatcher |
| CanManageCarriers     | SuperAdmin, Admin, DispatchManager             |
| CanManageLoads        | SuperAdmin, Admin, DispatchManager, Dispatcher |
| CanManageTrucks       | SuperAdmin, Admin, DispatchManager             |
| CanManageDrivers      | SuperAdmin, Admin, DispatchManager             |
| CanViewBrokers        | SuperAdmin, Admin, DispatchManager             |
| CanManageFinance      | SuperAdmin, Admin                              |
| CanViewReports        | SuperAdmin, Admin, DispatchManager, Dispatcher |
| CanManageSettings     | SuperAdmin                                     |
| CanAssignDispatchers  | SuperAdmin, Admin, DispatchManager             |

> Important: role checks must be enforced by the backend, not only by hiding frontend buttons.

---

# 7. Pagination

Paginated endpoints support:

```text
page
pageSize
```

Example:

```http
GET /api/Applications?page=1&pageSize=20
```

Standard pagination response:

```json
{
  "items": [],
  "page": 1,
  "pageSize": 20,
  "totalCount": 150,
  "totalPages": 8,
  "hasPrevious": false,
  "hasNext": true
}
```

Recommended frontend behavior:

```text
page = current page number
pageSize = number of records per page

Use:
hasPrevious → Enable/disable Previous button
hasNext → Enable/disable Next button
totalCount → Display total records
totalPages → Display page navigation
```

---

# 8. Auth Controller

Base route:

```text
/api/Auth
```

## Endpoint Summary

| Method | Endpoint           | Auth | Purpose                     |
| ------ | ------------------ | ---- | --------------------------- |
| POST   | `/login`           | No   | Login                       |
| POST   | `/register`        | No   | Create user                 |
| POST   | `/refresh`         | No   | Refresh access token        |
| POST   | `/logout`          | Yes  | Logout/revoke refresh token |
| GET    | `/me`              | Yes  | Get current user            |
| POST   | `/forgot-password` | No   | Request password reset      |
| POST   | `/reset-password`  | No   | Reset password              |

---

## POST `/api/Auth/login`

Authenticates a user and returns access and refresh tokens.

### Request Body

```json
{
  "email": "user@example.com",
  "password": "YourPassword"
}
```

### Success Response

```json
{
  "success": true,
  "message": null,
  "data": {
    "accessToken": "JWT_ACCESS_TOKEN",
    "refreshToken": "REFRESH_TOKEN",
    "expiresAt": "2026-08-28T15:30:00Z",
    "userProfile": {
      "id": "user-guid",
      "firstName": "John",
      "lastName": "Doe",
      "email": "user@example.com",
      "phoneNumber": "+1234567890",
      "role": "Dispatcher"
    }
  }
}
```

### Frontend Flow

```text
User enters email/password
        ↓
POST /api/Auth/login
        ↓
Store access token securely
        ↓
Store refresh token securely
        ↓
Save user profile
        ↓
Redirect to dashboard
```

---

## POST `/api/Auth/register`

Creates a new system user.

### Request Body

```json
{
  "firstName": "John",
  "lastName": "Doe",
  "email": "john@example.com",
  "password": "Password123!",
  "confirmPassword": "Password123!",
  "phoneNumber": "+1234567890",
  "role": "Dispatcher"
}
```

### Validation

```text
FirstName     Required, maximum 100 characters
LastName      Required, maximum 100 characters
Email         Required, valid email, maximum 200 characters
Password      8-100 characters
              Must contain uppercase
              Must contain lowercase
              Must contain digit
ConfirmPassword Must match Password
PhoneNumber   Maximum 50 characters
Role          Required, maximum 50 characters
```

> Recommended improvement: restrict this endpoint to authorized administrators in production if users should not self-register.

---

## POST `/api/Auth/refresh`

Creates a new access token using a valid refresh token.

### Request Body

```json
{
  "refreshToken": "YOUR_REFRESH_TOKEN"
}
```

### Recommended Frontend Flow

```text
API Request
    ↓
401 because access token expired
    ↓
POST /api/Auth/refresh
    ↓
New access token
    ↓
Retry original request
```

---

## POST `/api/Auth/logout`

Requires authentication.

Revokes the user's refresh token/session.

### Header

```http
Authorization: Bearer YOUR_ACCESS_TOKEN
```

---

## GET `/api/Auth/me`

Returns the currently authenticated user.

### Header

```http
Authorization: Bearer YOUR_ACCESS_TOKEN
```

Useful when:

- Dashboard refreshes
- Application starts
- Frontend needs the current role
- Permissions must be loaded

---

## POST `/api/Auth/forgot-password`

Requests a password reset.

### Request Body

```json
{
  "email": "user@example.com"
}
```

The backend should generate the reset flow without exposing whether an email exists.

Recommended generic response:

```json
{
  "success": true,
  "message": "If the account exists, password reset instructions have been sent.",
  "data": null
}
```

---

## POST `/api/Auth/reset-password`

### Request Body

```json
{
  "email": "user@example.com",
  "token": "RESET_TOKEN",
  "newPassword": "NewPassword123!",
  "confirmPassword": "NewPassword123!"
}
```

---

# 9. Applications Controller

Base route:

```text
/api/Applications
```

Applications represent leads or carrier applications.

Main workflow:

```text
New
 ↓
Reviewing
 ↓
Contacted
 ↓
Qualified
 ↓
Approved
 ↓
Convert to Carrier
 ↓
Onboarded
```

Rejected applications follow:

```text
New / Reviewing / Contacted / Qualified
                    ↓
                 Rejected
```

## Endpoints

| Method | Endpoint        | Authorization         | Purpose            |
| ------ | --------------- | --------------------- | ------------------ |
| GET    | `/`             | Authenticated         | List applications  |
| POST   | `/`             | CanManageApplications | Create application |
| GET    | `/{id}`         | Authenticated         | Get application    |
| PATCH  | `/{id}`         | CanManageApplications | Update application |
| DELETE | `/{id}`         | SuperAdmin/Admin      | Soft delete        |
| POST   | `/{id}/assign`  | CanManageApplications | Assign user        |
| POST   | `/{id}/contact` | CanManageApplications | Mark contacted     |
| POST   | `/{id}/approve` | CanManageApplications | Approve            |
| POST   | `/{id}/reject`  | CanManageApplications | Reject             |
| POST   | `/{id}/convert` | CanManageApplications | Convert to carrier |
| GET    | `/{id}/notes`   | Authenticated         | Get notes          |
| POST   | `/{id}/notes`   | CanManageApplications | Add note           |

---

## GET `/api/Applications`

Lists applications.

### Query Parameters

| Parameter | Type    | Default | Description                  |
| --------- | ------- | ------- | ---------------------------- |
| page      | integer | 1       | Page number                  |
| pageSize  | integer | 20      | Records per page             |
| search    | string  | null    | Search name, email, company  |
| status    | integer | null    | Filter by application status |

### Example

```http
GET /api/Applications?page=1&pageSize=20&search=trucking&status=0
```

---

## POST `/api/Applications`

Creates an application internally from the dashboard.

### Request Body

```json
{
  "fullName": "John Doe",
  "email": "john@example.com",
  "phone": "+1234567890",
  "companyName": "Doe Trucking LLC",
  "equipmentType": 0,
  "truckCount": 5,
  "mcNumber": "MC123456",
  "dotNumber": "DOT789012",
  "preferredLanes": "TX to CA",
  "additionalDetails": "Experienced carrier"
}
```

### Validation

```text
FullName       Required, maximum 200
Email          Required, valid email, maximum 200
Phone          Required, maximum 50
CompanyName    Required, maximum 200
TruckCount     Must be greater than 0
```

---

## GET `/api/Applications/{id}`

Returns one application and its details.

Example:

```http
GET /api/Applications/APPLICATION_GUID
```

---

## PATCH `/api/Applications/{id}`

Updates application fields.

Only changed fields should be sent.

Example:

```json
{
  "preferredLanes": "Texas, California, Florida"
}
```

At least one field must be provided.

---

## DELETE `/api/Applications/{id}`

Soft deletes an application.

The record remains in the database but is marked:

```text
IsDeleted = true
```

Recommended behavior:

```text
Normal GET queries
        ↓
Exclude soft-deleted records
```

---

## POST `/api/Applications/{id}/assign`

Assigns the application to a user/dispatcher.

Recommended request body:

```json
{
  "assignedToUserId": "user-guid"
}
```

Recommended flow:

```text
Application
    ↓
Assign Dispatcher
    ↓
Update AssignedToUserId
    ↓
Create Activity Log
    ↓
Create Notification
```

---

## POST `/api/Applications/{id}/contact`

Marks an application as contacted.

Recommended result:

```text
Status = Contacted
ContactedAt = Current UTC Time
```

---

## POST `/api/Applications/{id}/approve`

Approves the application.

Recommended result:

```text
Status = Approved
ApprovedAt = Current UTC Time
```

---

## POST `/api/Applications/{id}/reject`

Rejects an application.

Recommended result:

```text
Status = Rejected
RejectedAt = Current UTC Time
```

Recommended request body if a reason is supported:

```json
{
  "reason": "Application does not meet onboarding requirements."
}
```

---

## POST `/api/Applications/{id}/convert`

Converts an approved application into a carrier.

### Request Body

```json
{
  "assignedDispatcherId": "dispatcher-guid",
  "notes": "Carrier approved and ready for onboarding."
}
```

### Required Transaction Flow

```text
BEGIN TRANSACTION
        ↓
Validate Application
        ↓
Verify Application Status
        ↓
Create Carrier
        ↓
Copy Application Information
        ↓
Assign Dispatcher
        ↓
Link Carrier to Application
        ↓
Update Application Status
        ↓
Create Activity Log
        ↓
Create Notification if required
        ↓
COMMIT
```

If any step fails:

```text
ROLLBACK
```

This prevents partial conversion.

---

## GET `/api/Applications/{id}/notes`

Returns internal notes for an application.

---

## POST `/api/Applications/{id}/notes`

Recommended request:

```json
{
  "content": "Called carrier. Waiting for insurance documents."
}
```

Notes should include:

```text
Id
ApplicationId
Content
CreatedByUserId
CreatedAt
```

---

# 10. Carriers Controller

Base route:

```text
/api/Carriers
```

A carrier is generally created after application approval/conversion.

## Endpoints

| Method | Endpoint                  | Authorization        | Purpose           |
| ------ | ------------------------- | -------------------- | ----------------- |
| GET    | `/`                       | Authenticated        | List carriers     |
| POST   | `/`                       | CanManageCarriers    | Create carrier    |
| GET    | `/{id}`                   | Authenticated        | Get carrier       |
| PATCH  | `/{id}`                   | CanManageCarriers    | Update carrier    |
| DELETE | `/{id}`                   | SuperAdmin/Admin     | Soft delete       |
| POST   | `/{id}/assign-dispatcher` | CanAssignDispatchers | Assign dispatcher |
| GET    | `/{id}/notes`             | Authenticated        | Get notes         |
| POST   | `/{id}/notes`             | CanManageCarriers    | Add note          |
| GET    | `/{id}/trucks`            | Authenticated        | Carrier trucks    |
| GET    | `/{id}/drivers`           | Authenticated        | Carrier drivers   |
| GET    | `/{id}/loads`             | Authenticated        | Carrier loads     |

---

## GET `/api/Carriers`

### Query Parameters

```text
page
pageSize
search
status
```

Search should cover:

```text
Company name
Contact name
Email
```

Example:

```http
GET /api/Carriers?page=1&pageSize=20&search=transport&status=2
```

---

## POST `/api/Carriers`

### Request Body

```json
{
  "companyName": "ABC Transport",
  "contactName": "John Smith",
  "email": "john@abctransport.com",
  "phone": "+1234567890",
  "mcNumber": "MC654321",
  "dotNumber": "DOT210987",
  "addressLine1": "123 Main St",
  "city": "Dallas",
  "state": "TX",
  "zipCode": "75201",
  "preferredLanes": "TX, CA, FL",
  "notes": "Reliable carrier",
  "applicationId": null
}
```

---

## POST `/api/Carriers/{id}/assign-dispatcher`

Recommended request:

```json
{
  "dispatcherId": "dispatcher-guid"
}
```

Flow:

```text
Carrier
   ↓
Assign Dispatcher
   ↓
Update AssignedDispatcherId
   ↓
Create Activity Log
   ↓
Notify Dispatcher
```

---

# 11. Trucks Controller

Base route:

```text
/api/Trucks
```

## Endpoints

| Method | Endpoint | Authorization   |
| ------ | -------- | --------------- |
| GET    | `/`      | Authenticated   |
| POST   | `/`      | CanManageTrucks |
| GET    | `/{id}`  | Authenticated   |
| PATCH  | `/{id}`  | Authenticated   |

## GET Query Parameters

```text
page
pageSize
search
carrierId
status
```

Search covers:

```text
Truck number
Make
Model
```

## POST `/api/Trucks`

```json
{
  "carrierId": "carrier-guid",
  "truckNumber": "T-001",
  "equipmentType": 0,
  "make": "Freightliner",
  "model": "Cascadia",
  "year": 2024,
  "licensePlate": "ABC1234",
  "licenseState": "TX"
}
```

---

# 12. Drivers Controller

Base route:

```text
/api/Drivers
```

## Endpoints

| Method | Endpoint | Authorization    |
| ------ | -------- | ---------------- |
| GET    | `/`      | Authenticated    |
| POST   | `/`      | CanManageDrivers |
| GET    | `/{id}`  | Authenticated    |
| PATCH  | `/{id}`  | Authenticated    |
| DELETE | `/{id}`  | SuperAdmin/Admin |

## GET Query Parameters

```text
page
pageSize
search
carrierId
status
```

## POST `/api/Drivers`

```json
{
  "carrierId": "carrier-guid",
  "truckId": "truck-guid",
  "firstName": "Mike",
  "lastName": "Johnson",
  "email": "mike@example.com",
  "phone": "+1234567890",
  "licenseNumber": "DL123456",
  "licenseState": "TX"
}
```

`truckId` may be null when a driver has not yet been assigned a truck.

---

# 13. Loads Controller

Base route:

```text
/api/Loads
```

Loads are the main operational records in the system.

## Endpoints

| Method | Endpoint       | Authorization    |
| ------ | -------------- | ---------------- |
| GET    | `/`            | Authenticated    |
| POST   | `/`            | CanManageLoads   |
| GET    | `/{id}`        | Authenticated    |
| PATCH  | `/{id}`        | CanManageLoads   |
| DELETE | `/{id}`        | SuperAdmin/Admin |
| PATCH  | `/{id}/status` | CanManageLoads   |
| GET    | `/{id}/notes`  | Authenticated    |
| POST   | `/{id}/notes`  | CanManageLoads   |

---

## GET `/api/Loads`

### Query Parameters

```text
page
pageSize
search
carrierId
status
dispatcherId
```

Search covers:

```text
Load number
Pickup city
Delivery city
```

Example:

```http
GET /api/Loads?page=1&pageSize=20&status=5&dispatcherId=DISPATCHER_GUID
```

---

## POST `/api/Loads`

Creates a load and calculates financial values.

### Request Body

```json
{
  "carrierId": "carrier-guid",
  "truckId": "truck-guid",
  "driverId": "driver-guid",
  "brokerId": "broker-guid",
  "equipmentType": 0,
  "pickupCity": "Dallas",
  "pickupState": "TX",
  "pickupDateTime": "2026-09-01T08:00:00Z",
  "deliveryCity": "Los Angeles",
  "deliveryState": "CA",
  "deliveryDateTime": "2026-09-03T18:00:00Z",
  "rate": 3500.0,
  "miles": 1400,
  "dispatchFeeType": "percentage",
  "dispatchFeeValue": 10
}
```

### Financial Calculations

Rate per mile:

```text
RatePerMile = Rate / Miles
```

Example:

```text
3500 / 1400 = 2.50
```

Percentage dispatch fee:

```text
DispatchFeeAmount =
Rate × DispatchFeeValue / 100
```

Example:

```text
3500 × 10 / 100 = 350
```

Carrier net:

```text
CarrierNetAmount =
Rate - DispatchFeeAmount
```

Example:

```text
3500 - 350 = 3150
```

For a flat fee:

```text
DispatchFeeAmount = DispatchFeeValue
CarrierNetAmount = Rate - DispatchFeeValue
```

---

## PATCH `/api/Loads/{id}/status`

Updates the load status.

### Request Body

```json
{
  "status": 4,
  "notes": "Driver picked up load."
}
```

### Load Status Flow

```text
Available
    ↓
Negotiating
    ↓
Booked
    ↓
Dispatched
    ↓
PickedUp
    ↓
InTransit
    ↓
Delivered
    ↓
Completed
```

Alternative statuses:

```text
Cancelled
Issue
```

Recommended behavior:

```text
Status changes
       ↓
Create Activity Log
       ↓
Store status timestamp
       ↓
Create notification when needed
       ↓
Push real-time update
```

---

# 14. Brokers Controller

Base route:

```text
/api/Brokers
```

## Endpoints

| Method | Endpoint | Authorization    |
| ------ | -------- | ---------------- |
| GET    | `/`      | CanViewBrokers   |
| POST   | `/`      | CanViewBrokers   |
| GET    | `/{id}`  | CanViewBrokers   |
| PATCH  | `/{id}`  | CanViewBrokers   |
| DELETE | `/{id}`  | SuperAdmin/Admin |

## POST `/api/Brokers`

```json
{
  "companyName": "XYZ Freight Brokers",
  "contactName": "Jane Doe",
  "email": "jane@xyzfreight.com",
  "phone": "+1234567890",
  "mcNumber": "MC111222",
  "address": "456 Broker Ave",
  "internalRating": 4,
  "paymentNotes": "Net 30",
  "generalNotes": "Reliable broker"
}
```

Recommended future broker metrics:

```text
Total Loads
Average Rate
Average RPM
Cancellation Rate
Payment Reliability
Internal Rating
```

---

# 15. Documents Controller

Base route:

```text
/api/Documents
```

## Endpoints

| Method | Endpoint  | Authorization    |
| ------ | --------- | ---------------- |
| POST   | `/upload` | Authenticated    |
| GET    | `/{id}`   | Authenticated    |
| DELETE | `/{id}`   | SuperAdmin/Admin |

---

## POST `/api/Documents/upload`

Content type:

```text
multipart/form-data
```

### Fields

| Field        | Type    | Required | Description         |
| ------------ | ------- | -------- | ------------------- |
| file         | File    | Yes      | File being uploaded |
| documentType | Integer | Yes      | Document type       |
| carrierId    | GUID    | No       | Related carrier     |
| loadId       | GUID    | No       | Related load        |
| driverId     | GUID    | No       | Related driver      |

Example JavaScript:

```javascript
const formData = new FormData();

formData.append("file", selectedFile);
formData.append("documentType", "0");
formData.append("carrierId", carrierId);

await fetch("/api/Documents/upload?documentType=0&carrierId=" + carrierId, {
  method: "POST",
  headers: {
    Authorization: `Bearer ${token}`,
  },
  body: formData,
});
```

> Do not manually set `Content-Type` when sending FormData. The browser sets the multipart boundary automatically.

---

# 16. Billing Controller

Base route:

```text
/api/Billing
```

## Endpoints

| Method | Endpoint                  | Authorization    |
| ------ | ------------------------- | ---------------- |
| GET    | `/invoices`               | CanManageFinance |
| POST   | `/invoices`               | CanManageFinance |
| GET    | `/invoices/{id}`          | CanManageFinance |
| PATCH  | `/invoices/{id}/status`   | CanManageFinance |
| POST   | `/invoices/{id}/payments` | CanManageFinance |

---

## POST `/api/Billing/invoices`

Creates an invoice with line items.

### Request Body

```json
{
  "carrierId": "carrier-guid",
  "periodStart": "2026-08-01T00:00:00Z",
  "periodEnd": "2026-08-31T23:59:59Z",
  "taxAmount": 250.0,
  "dueDate": "2026-09-15T00:00:00Z",
  "items": [
    {
      "loadId": "load-guid",
      "description": "Dispatch fee - Load #LD001",
      "quantity": 1,
      "unitPrice": 350.0
    },
    {
      "loadId": "load-guid",
      "description": "Dispatch fee - Load #LD002",
      "quantity": 1,
      "unitPrice": 420.0
    }
  ]
}
```

Calculation:

```text
Item Amount = Quantity × UnitPrice

Subtotal = Sum of all item amounts

TotalAmount = Subtotal + TaxAmount
```

---

## POST `/api/Billing/invoices/{id}/payments`

Records a payment.

### Request Body

```json
{
  "amount": 1020.0,
  "paymentMethod": "Bank Transfer",
  "transactionReference": "TXN-2026-001"
}
```

Recommended backend flow:

```text
Record Payment
       ↓
Calculate Total Paid
       ↓
Compare with Invoice Total
       ↓
Update Invoice Status
```

Possible results:

```text
No payment        → Sent / Draft
Partial payment   → PartiallyPaid
Full payment      → Paid
```

---

# 17. Dashboard Controller

Base route:

```text
/api/Dashboard
```

## Endpoints

| Method | Endpoint               | Authorization    |
| ------ | ---------------------- | ---------------- |
| GET    | `/summary`             | Authenticated    |
| GET    | `/load-status-summary` | Authenticated    |
| GET    | `/recent-activity`     | Authenticated    |
| GET    | `/revenue-summary`     | CanManageFinance |
| POST   | `/contact`             | Public           |

---

## GET `/api/Dashboard/summary`

Returns main dashboard statistics.

Example response:

```json
{
  "newApplications": 12,
  "applicationsInReview": 5,
  "activeCarriers": 48,
  "activeTrucks": 72,
  "activeLoads": 15,
  "loadsInTransit": 8,
  "completedLoadsThisMonth": 23,
  "dispatchRevenueThisMonth": 18500.0
}
```

Recommended dashboard cards:

```text
New Applications
Applications In Review
Active Carriers
Active Trucks
Active Loads
Loads In Transit
Completed This Month
Dispatch Revenue This Month
```

---

## GET `/api/Dashboard/load-status-summary`

Used for charts and load overview.

Recommended frontend visualization:

```text
Available       10
Negotiating      5
Booked           8
Dispatched       6
Picked Up        4
In Transit       8
Delivered        3
Completed       25
Cancelled        2
Issue            1
```

---

## GET `/api/Dashboard/recent-activity`

Returns recent activity logs.

Recommended activity item:

```json
{
  "userName": "John Doe",
  "action": "Updated",
  "entityType": "Load",
  "entityId": "load-guid",
  "description": "Changed status from Booked to PickedUp",
  "createdAt": "2026-08-28T10:00:00Z"
}
```

---

# 18. Reports Controller

Base route:

```text
/api/Reports
```

## Endpoints

| Method | Endpoint       | Authorization  |
| ------ | -------------- | -------------- |
| GET    | `/loads`       | CanViewReports |
| GET    | `/revenue`     | CanViewReports |
| GET    | `/carriers`    | CanViewReports |
| GET    | `/dispatchers` | CanViewReports |

---

## GET `/api/Reports/loads`

Example response:

```json
{
  "totalLoads": 150,
  "activeLoads": 23,
  "completedLoads": 110,
  "cancelledLoads": 7,
  "averageRate": 2800.0,
  "averageRpm": 2.15,
  "totalRevenue": 308000.0
}
```

---

## GET `/api/Reports/revenue`

Example response:

```json
{
  "totalRevenue": 500000.0,
  "totalDispatchFees": 50000.0,
  "totalCarrierPayouts": 450000.0,
  "averageRevenuePerLoad": 3333.33,
  "monthlyBreakdown": [
    {
      "year": 2026,
      "month": 8,
      "revenue": 45000.0,
      "loadCount": 18
    }
  ]
}
```

---

# 19. Messages Controller

Base route:

```text
/api/Messages
```

This is the REST API for dashboard messaging.

SignalR provides real-time delivery.

## Endpoints

| Method | Endpoint                   | Authorization |
| ------ | -------------------------- | ------------- |
| GET    | `/conversations`           | Authenticated |
| GET    | `/conversations/{id}`      | Authenticated |
| POST   | `/send`                    | Authenticated |
| PATCH  | `/conversations/{id}/read` | Authenticated |

---

## GET `/api/Messages/conversations`

Returns conversations for the dashboard inbox.

Recommended query parameters:

```text
page
pageSize
search
status
assignedToUserId
```

Recommended response fields:

```text
ConversationId
VisitorName
VisitorEmail
VisitorPhone
AssignedToUserId
Status
LastMessage
LastMessageAt
UnreadCount
```

---

## GET `/api/Messages/conversations/{id}`

Returns conversation details and messages.

Recommended response:

```json
{
  "id": "conversation-guid",
  "visitorName": "John Doe",
  "status": "Open",
  "messages": [
    {
      "id": "message-guid",
      "content": "Hello, I need dispatch services.",
      "senderType": 0,
      "senderUserId": null,
      "isRead": true,
      "createdAt": "2026-08-28T10:00:00Z"
    }
  ]
}
```

---

## POST `/api/Messages/send`

Sends a message from an authenticated dashboard user.

### Request Body

```json
{
  "conversationId": "conversation-guid",
  "content": "Hello, how can I help you?"
}
```

Recommended backend flow:

```text
Validate conversation
       ↓
Validate user access
       ↓
Save message
       ↓
Update LastMessageAt
       ↓
Create notification if required
       ↓
SignalR ReceiveMessage event
       ↓
Connected visitor/dashboard receives message
```

---

## PATCH `/api/Messages/conversations/{id}/read`

Marks messages as read.

Recommended result:

```text
Unread visitor/admin messages
        ↓
IsRead = true
        ↓
SignalR MessagesRead event
```

---

# 20. Public Website Application API

Base route:

```text
/api/public/Applications
```

This endpoint is used by the Driventa public website.

## POST `/api/public/Applications`

Authentication:

```text
No JWT required
```

Rate limited.

Current documented policy:

```text
10 requests per minute
```

### Request Body

```json
{
  "fullName": "John Doe",
  "email": "john@example.com",
  "phone": "+1234567890",
  "companyName": "Doe Trucking LLC",
  "equipmentType": 0,
  "truckCount": 5,
  "mcNumber": "MC123456",
  "dotNumber": "DOT789012",
  "preferredLanes": "TX to CA",
  "additionalDetails": "Experienced carrier looking for dispatch services"
}
```

### Website Flow

```text
Visitor fills Driventa form
        ↓
Frontend validation
        ↓
POST /api/public/Applications
        ↓
Rate limit check
        ↓
FluentValidation
        ↓
Save Application
        ↓
Create Activity Log
        ↓
Create dashboard notification
        ↓
Application appears in dashboard
```

Recommended website success response:

```json
{
  "success": true,
  "message": "Your application has been submitted successfully.",
  "data": {
    "applicationId": "application-guid",
    "applicationNumber": "APP-XXXX"
  }
}
```

---

# 21. Public Contact API

The backend documentation indicates a public contact submission endpoint.

Use:

```text
POST /api/public/contact
```

It should be public and rate limited.

Recommended request:

```json
{
  "fullName": "John Doe",
  "email": "john@example.com",
  "phone": "+1234567890",
  "subject": "Dispatch Services",
  "message": "I would like more information."
}
```

Recommended flow:

```text
Website Contact Form
        ↓
POST /api/public/contact
        ↓
Validate
        ↓
Rate Limit
        ↓
Save / Create Message
        ↓
Notify Dashboard
```

---

# 22. Public Chat Session API

The backend includes a public chat session endpoint.

Use:

```text
POST /api/public/chat/session
```

Recommended request:

```json
{
  "visitorName": "John Doe",
  "visitorEmail": "john@example.com",
  "visitorPhone": "+1234567890"
}
```

Recommended response:

```json
{
  "success": true,
  "data": {
    "conversationId": "conversation-guid",
    "visitorId": "visitor-guid"
  }
}
```

Flow:

```text
Visitor opens chat
        ↓
Create/restore session
        ↓
Receive Conversation ID
        ↓
Connect to ChatHub
        ↓
JoinConversation
        ↓
Start real-time chat
```

---

# 23. SignalR Chat Hub

Hub route:

```text
/hubs/chat
```

Local example:

```text
ws://localhost:5165/hubs/chat
```

Production should use secure WebSocket transport through HTTPS/WSS.

Authentication is optional because website visitors may be anonymous.

Authenticated clients may connect with:

```text
?access_token=JWT_TOKEN
```

## Client Methods

### JoinConversation

Parameters:

```text
conversationId: string
```

Purpose:

```text
Join the SignalR group for one conversation.
```

### SendMessage

Parameters:

```text
conversationId: string
message: string
```

Purpose:

```text
Send a real-time message.
```

### MarkAsRead

Parameters:

```text
conversationId: string
```

Purpose:

```text
Mark conversation messages as read.
```

## Server Events

### ReceiveMessage

Payload:

```json
{
  "messageId": "message-guid",
  "message": "Hello!",
  "senderUserId": "user-guid",
  "senderType": 1,
  "timestamp": "2026-08-28T10:00:00Z"
}
```

### MessagesRead

Payload:

```json
{
  "conversationId": "conversation-guid"
}
```

---

# 24. SignalR Notification Hub

Hub route:

```text
/hubs/notifications
```

Authentication:

```text
JWT Required
```

## Client Methods

### JoinPersonalGroup

No parameters.

Purpose:

```text
Join the authenticated user's notification group.
```

### SendNotificationToUser

Parameters:

```text
targetUserId: GUID
title: string
message: string
```

Important security rule:

```text
The backend must verify notification ownership and permission.
Never trust target user IDs from the frontend without authorization checks.
```

## Server Events

### Connected

```json
{
  "userId": "user-guid"
}
```

### ReceiveNotification

```json
{
  "title": "New Application",
  "message": "A new carrier application was submitted.",
  "timestamp": "2026-08-28T10:00:00Z"
}
```

---

# 25. Recommended SSE Implementation

SignalR should remain the primary real-time technology for chat.

Recommended use:

```text
SignalR
├── Two-way chat
├── Website messaging
└── Real-time notifications

SSE
├── Optional dashboard event stream
├── Application updates
├── Load updates
├── Activity feed
└── Long-lived server-to-client events
```

Do not unnecessarily duplicate every chat event through both SignalR and SSE.

## Recommended SSE Endpoint

```text
GET /api/events/stream
```

Required response header:

```text
Content-Type: text/event-stream
Cache-Control: no-cache
Connection: keep-alive
```

Example events:

```text
event: application.created
data: {"applicationId":"guid","applicationNumber":"APP-001"}

event: load.updated
data: {"loadId":"guid","status":"InTransit"}

event: notification
data: {"id":"guid","title":"New Message"}

event: heartbeat
data: {"timestamp":"2026-08-28T10:00:00Z"}
```

Recommended SSE architecture:

```text
Business Action
      ↓
Save Database
      ↓
Event Publisher
      ├── SignalR Publisher
      └── SSE Publisher
```

Use a shared abstraction such as:

```text
IRealtimeEventPublisher
```

instead of adding SignalR or SSE code directly throughout every controller.

---

# 26. Enum Reference

## ApplicationStatus

| Value | Name      |
| ----- | --------- |
| 0     | New       |
| 1     | Reviewing |
| 2     | Contacted |
| 3     | Qualified |
| 4     | Approved  |
| 5     | Rejected  |
| 6     | Onboarded |

## CarrierStatus

| Value | Name       |
| ----- | ---------- |
| 0     | Lead       |
| 1     | Onboarding |
| 2     | Active     |
| 3     | Paused     |
| 4     | Inactive   |
| 5     | Suspended  |

## LoadStatus

| Value | Name        |
| ----- | ----------- |
| 0     | Available   |
| 1     | Negotiating |
| 2     | Booked      |
| 3     | Dispatched  |
| 4     | PickedUp    |
| 5     | InTransit   |
| 6     | Delivered   |
| 7     | Completed   |
| 8     | Cancelled   |
| 9     | Issue       |

## TruckStatus

| Value | Name        |
| ----- | ----------- |
| 0     | Available   |
| 1     | OnLoad      |
| 2     | Maintenance |
| 3     | Inactive    |

## DriverStatus

| Value | Name      |
| ----- | --------- |
| 0     | Available |
| 1     | Assigned  |
| 2     | Driving   |
| 3     | OffDuty   |
| 4     | Inactive  |

## EquipmentType

| Value | Name      |
| ----- | --------- |
| 0     | DryVan    |
| 1     | Reefer    |
| 2     | Flatbed   |
| 3     | StepDeck  |
| 4     | BoxTruck  |
| 5     | Hotshot   |
| 6     | PowerOnly |

## InvoiceStatus

| Value | Name          |
| ----- | ------------- |
| 0     | Draft         |
| 1     | Sent          |
| 2     | PartiallyPaid |
| 3     | Paid          |
| 4     | Overdue       |
| 5     | Cancelled     |

## PaymentStatus

| Value | Name      |
| ----- | --------- |
| 0     | Pending   |
| 1     | Completed |
| 2     | Failed    |
| 3     | Refunded  |

## DocumentType

| Value | Name             |
| ----- | ---------------- |
| 0     | Insurance        |
| 1     | W9               |
| 2     | MC_Authority     |
| 3     | RateConfirmation |
| 4     | BOL              |
| 5     | POD              |
| 6     | CarrierAgreement |
| 7     | DriverLicense    |
| 8     | Other            |

## NotificationType

| Value | Name              |
| ----- | ----------------- |
| 0     | NewApplication    |
| 1     | NewMessage        |
| 2     | LoadStatusChanged |
| 3     | DocumentExpiring  |
| 4     | DocumentUploaded  |
| 5     | CarrierAssigned   |

## SenderType

| Value | Name       |
| ----- | ---------- |
| 0     | Visitor    |
| 1     | Admin      |
| 2     | Dispatcher |
| 3     | System     |

---

# 27. Database Relationships

```text
Application
    │
    ├── 1:N ApplicationNotes
    │
    └── 1:1 Carrier
            │
            ├── 1:N Trucks
            ├── 1:N Drivers
            ├── 1:N Loads
            ├── 1:N Documents
            ├── 1:N CarrierNotes
            └── 1:N Invoices
                    │
                    ├── 1:N InvoiceItems
                    └── 1:N Payments

Truck
    ├── optional relationship with Drivers
    └── optional relationship with Loads

Driver
    ├── optional relationship with Loads
    └── 1:N Documents

Broker
    └── 1:N Loads

Load
    ├── 1:N Documents
    ├── 1:N LoadNotes
    └── 1:N InvoiceItems

Conversation
    └── 1:N Messages
```

---

# 28. BaseEntity and Auditing

Core entities use common fields:

```text
Id
CreatedAt
UpdatedAt
CreatedByUserId
UpdatedByUserId
IsDeleted
```

## Soft Delete

Delete operations should generally perform:

```text
IsDeleted = true
UpdatedAt = UTC NOW
```

instead of permanently deleting the record.

All normal queries should exclude:

```text
IsDeleted = true
```

unless an administrator is explicitly viewing deleted records.

---

# 29. Activity Logging

Important operations should create an activity log.

Examples:

```text
Application Created
Application Assigned
Application Contacted
Application Approved
Application Rejected
Application Converted to Carrier
Carrier Assigned
Load Created
Load Updated
Load Status Changed
Document Uploaded
Document Deleted
Invoice Created
Payment Recorded
```

Recommended activity log structure:

```text
Id
UserId
Action
EntityType
EntityId
Description
OldValuesJson
NewValuesJson
CreatedAt
```

Example:

```json
{
  "action": "LoadStatusChanged",
  "entityType": "Load",
  "entityId": "load-guid",
  "description": "Changed status from Booked to PickedUp"
}
```

---

# 30. Error Handling

## HTTP Status Mapping

| Error                       | HTTP Status               |
| --------------------------- | ------------------------- |
| ValidationException         | 400 Bad Request           |
| ArgumentException           | 400 Bad Request           |
| KeyNotFoundException        | 404 Not Found             |
| UnauthorizedAccessException | 401 Unauthorized          |
| InvalidOperationException   | 409 Conflict              |
| HubException                | 400 Bad Request           |
| Unexpected Exception        | 500 Internal Server Error |

## Recommended Error Handling Rules

Never return:

```text
Database connection strings
JWT secrets
Stack traces
Internal server paths
Detailed exception data
```

to production clients.

Log those details on the server instead.

---

# 31. CORS

Development origins may include:

```text
http://localhost:3000
http://localhost:5173
```

Production origins should be restricted to the actual Driventa frontend applications:

```text
https://driventa.us
https://www.driventa.us
https://dashboard.driventa.us
```

Do not use:

```text
AllowAnyOrigin
```

with authenticated production APIs unless the architecture specifically supports that safely.

---

# 32. Rate Limiting

Public endpoints are rate limited to protect against spam and abuse.

Current documented policy:

```text
10 requests
per 1 minute
queue size: 0
```

Protected endpoints should additionally rely on:

```text
JWT Authentication
Role Authorization
Ownership Validation
Input Validation
```

---

# 33. Swagger

Swagger is used for interactive API testing and documentation.

Local development:

```text
/swagger
```

Typical flow:

```text
1. Login
2. Copy Access Token
3. Click Authorize
4. Enter:
   Bearer YOUR_ACCESS_TOKEN
5. Test protected endpoints
```

Swagger should be protected or disabled appropriately in production depending on deployment requirements.

---

# 34. Website to Dashboard Integration

The main Driventa website integration flow is:

```text
WEBSITE VISITOR
       ↓
Carrier Application Form
       ↓
POST /api/public/Applications
       ↓
Rate Limiting
       ↓
FluentValidation
       ↓
PostgreSQL
       ↓
Application Created
       ↓
Activity Log
       ↓
Notification
       ↓
Dashboard Applications Page
```

Then:

```text
Dashboard
   ↓
Review Application
   ↓
Assign Dispatcher
   ↓
Add Notes
   ↓
Contact
   ↓
Approve
   ↓
Convert to Carrier
   ↓
Carrier Created
   ↓
Add Trucks
   ↓
Add Drivers
   ↓
Upload Documents
   ↓
Create / Manage Loads
```

---

# 35. Recommended Frontend API Organization

```text
src/
├── api/
│   ├── client.ts
│   ├── auth.api.ts
│   ├── applications.api.ts
│   ├── carriers.api.ts
│   ├── trucks.api.ts
│   ├── drivers.api.ts
│   ├── loads.api.ts
│   ├── brokers.api.ts
│   ├── documents.api.ts
│   ├── billing.api.ts
│   ├── dashboard.api.ts
│   ├── reports.api.ts
│   └── messages.api.ts
│
├── services/
│   ├── auth.service.ts
│   ├── signalr.service.ts
│   └── sse.service.ts
│
├── hooks/
│   ├── useAuth.ts
│   ├── useSignalR.ts
│   └── useNotifications.ts
│
└── types/
    ├── auth.ts
    ├── application.ts
    ├── carrier.ts
    ├── load.ts
    └── common.ts
```

Do not place all API calls directly inside React components.

---

# 36. Recommended API Client Behavior

Every authenticated request:

```text
Request
   ↓
Add Access Token
   ↓
Send API Request
   ↓
If 401
   ↓
Try Refresh Token Once
   ↓
Get New Access Token
   ↓
Retry Original Request
   ↓
If Refresh Fails
   ↓
Logout User
```

Do not infinitely retry failed refresh requests.

---

# 37. Production Security Checklist

Before production:

```text
[ ] Remove development passwords and secrets from source control
[ ] Rotate any exposed credentials
[ ] Store secrets in environment variables or secret manager
[ ] Enable HTTPS
[ ] Configure production CORS
[ ] Verify JWT issuer and audience
[ ] Verify JWT secret length and security
[ ] Enable database backups
[ ] Add database indexes for common queries
[ ] Add automated tests
[ ] Validate file types
[ ] Validate file size
[ ] Secure document downloads
[ ] Verify dispatcher ownership rules
[ ] Add logging and monitoring
[ ] Disable detailed production exception pages
[ ] Configure production Swagger access
[ ] Use production cloud file storage
```

---

# 38. Important Recommended Improvements

## Dispatcher Data Isolation

This should be enforced at the query level.

Example:

```text
Dispatcher
   ↓
GET /api/Loads
   ↓
Backend automatically filters:
DispatcherId == CurrentUserId
```

A dispatcher should not be able to change a URL or GUID and access another dispatcher's private resources.

Apply the same rule to:

```text
Applications
Carriers
Loads
Conversations
Notifications
```

---

## Database Indexes

Recommended indexes:

```text
Applications:
(Status, CreatedAt)
(AssignedToUserId, Status)

Carriers:
(Status)
(AssignedDispatcherId)

Loads:
(CarrierId, Status)
(DispatcherId, Status)
(Status, CreatedAt)

Messages:
(ConversationId, CreatedAt)

Conversations:
(AssignedToUserId, LastMessageAt)

Notifications:
(UserId, IsRead, CreatedAt)

ActivityLogs:
(EntityType, EntityId, CreatedAt)
```

---

## Testing

Recommended test projects:

```text
tests/
├── Driventa.Domain.Tests
├── Driventa.Application.Tests
└── Driventa.API.IntegrationTests
```

Critical tests:

```text
[ ] Login
[ ] Token refresh
[ ] Role authorization
[ ] Application validation
[ ] Application assignment
[ ] Application conversion transaction
[ ] Carrier creation
[ ] Load financial calculations
[ ] Load status changes
[ ] Dispatcher data isolation
[ ] Message ownership
[ ] Notification ownership
[ ] Invoice totals
[ ] Partial payments
[ ] Full payments
[ ] Public endpoint rate limiting
[ ] Document validation
```

---

# 39. Final System Flow

```text
                    DRIVENTA WEBSITE
                           │
             ┌─────────────┼─────────────┐
             │             │             │
             ▼             ▼             ▼
       Application      Contact         Chat
             │             │             │
             └─────────────┼─────────────┘
                           ▼
                    ASP.NET CORE API
                           │
          ┌────────────────┼────────────────┐
          ▼                ▼                ▼
     Authentication     PostgreSQL       SignalR
       JWT/Identity      Database       Real-Time
          │                │                │
          └────────────────┼────────────────┘
                           ▼
                    ADMIN DASHBOARD
                           │
      ┌────────────────────┼────────────────────┐
      ▼                    ▼                    ▼
 Applications          Operations           Finance
      │                    │                    │
      ▼                    ▼                    ▼
  Carriers          Loads/Trucks         Invoices
      │             Drivers/Brokers       Payments
      │                    │                    │
      └────────────────────┼────────────────────┘
                           ▼
                     Reports & Analytics
```

---

# 40. API Endpoint Quick Reference

```text
AUTH
POST   /api/Auth/login
POST   /api/Auth/register
POST   /api/Auth/refresh
POST   /api/Auth/logout
GET    /api/Auth/me
POST   /api/Auth/forgot-password
POST   /api/Auth/reset-password

APPLICATIONS
GET    /api/Applications
POST   /api/Applications
GET    /api/Applications/{id}
PATCH  /api/Applications/{id}
DELETE /api/Applications/{id}
POST   /api/Applications/{id}/assign
POST   /api/Applications/{id}/contact
POST   /api/Applications/{id}/approve
POST   /api/Applications/{id}/reject
POST   /api/Applications/{id}/convert
GET    /api/Applications/{id}/notes
POST   /api/Applications/{id}/notes

CARRIERS
GET    /api/Carriers
POST   /api/Carriers
GET    /api/Carriers/{id}
PATCH  /api/Carriers/{id}
DELETE /api/Carriers/{id}
POST   /api/Carriers/{id}/assign-dispatcher
GET    /api/Carriers/{id}/notes
POST   /api/Carriers/{id}/notes
GET    /api/Carriers/{id}/trucks
GET    /api/Carriers/{id}/drivers
GET    /api/Carriers/{id}/loads

TRUCKS
GET    /api/Trucks
POST   /api/Trucks
GET    /api/Trucks/{id}
PATCH  /api/Trucks/{id}

DRIVERS
GET    /api/Drivers
POST   /api/Drivers
GET    /api/Drivers/{id}
PATCH  /api/Drivers/{id}
DELETE /api/Drivers/{id}

LOADS
GET    /api/Loads
POST   /api/Loads
GET    /api/Loads/{id}
PATCH  /api/Loads/{id}
DELETE /api/Loads/{id}
PATCH  /api/Loads/{id}/status
GET    /api/Loads/{id}/notes
POST   /api/Loads/{id}/notes

BROKERS
GET    /api/Brokers
POST   /api/Brokers
GET    /api/Brokers/{id}
PATCH  /api/Brokers/{id}
DELETE /api/Brokers/{id}

DOCUMENTS
POST   /api/Documents/upload
GET    /api/Documents/{id}
DELETE /api/Documents/{id}

BILLING
GET    /api/Billing/invoices
POST   /api/Billing/invoices
GET    /api/Billing/invoices/{id}
PATCH  /api/Billing/invoices/{id}/status
POST   /api/Billing/invoices/{id}/payments

DASHBOARD
GET    /api/Dashboard/summary
GET    /api/Dashboard/load-status-summary
GET    /api/Dashboard/recent-activity
GET    /api/Dashboard/revenue-summary
POST   /api/Dashboard/contact

REPORTS
GET    /api/Reports/loads
GET    /api/Reports/revenue
GET    /api/Reports/carriers
GET    /api/Reports/dispatchers

MESSAGES
GET    /api/Messages/conversations
GET    /api/Messages/conversations/{id}
POST   /api/Messages/send
PATCH  /api/Messages/conversations/{id}/read

PUBLIC
POST   /api/public/Applications
POST   /api/public/contact
POST   /api/public/chat/session

SIGNALR
/hubs/chat
/hubs/notifications

RECOMMENDED SSE
GET    /api/events/stream
```

1. APP SHELL / MAIN UI
   Desktop
   ┌──────────────────────────────────────────────────────────────────────────────┐
   │ │
   │ 🚛 DRIVENTA 🔍 Search... 🔔 3 👤 Super Admin│
   ├───────────────────┬──────────────────────────────────────────────────────────┤
   │ │ │
   │ ◉ Dashboard │ Dashboard │
   │ │ Welcome back, Super Admin │
   │ LEADS │ │
   │ ◉ Applications │ [ New Apps ] [ Review ] [ Active Loads ] [ Revenue ] │
   │ ◉ Carriers │ │
   │ ◉ Documents │ ┌───────────────────────────┐ ┌──────────────────────┐ │
   │ │ │ Load Status │ │ Recent Activity │ │
   │ OPERATIONS │ │ │ │ │ │
   │ ◉ Loads │ │ Chart │ │ • Load updated │ │
   │ ◉ Trucks │ │ │ │ • New application │ │
   │ ◉ Drivers │ └───────────────────────────┘ └──────────────────────┘ │
   │ ◉ Brokers │ │
   │ │ Recent Applications View All → │
   │ COMMUNICATION │ ┌───────────────────────────────────────────────────┐ │
   │ ◉ Messages │ │ Applicant │ Company │ Trucks │ Status │ Action │ │
   │ ◉ Notifications │ └───────────────────────────────────────────────────┘ │
   │ │ │
   │ FINANCE │ │
   │ ◉ Billing │ │
   │ │ │
   │ ANALYTICS │ │
   │ ◉ Reports │ │
   │ │ │
   │ ───────────── │ │
   │ ⚙ Settings │ │
   └───────────────────┴──────────────────────────────────────────────────────────┘

Your sidebar modules directly correspond to the documented system modules.

Mobile
┌─────────────────────────────────┐
│ ☰ DRIVENTA 🔔 3 │
├─────────────────────────────────┤
│ │
│ PAGE CONTENT │
│ │
├─────────────────────────────────┤
│ 🏠 📋 💬 ☰ │
│ Home Apps Inbox More│
└─────────────────────────────────┘

More opens a full navigation sheet.

2. AUTH UI
   POST /api/Auth/login
   Screen
   ┌─────────────────────────────┐
   │ 🚛 DRIVENTA │
   │ │
   │ Welcome back │
   │ Sign in to manage operations│
   │ │
   │ Email │
   │ [ admin@driventa.com ] │
   │ │
   │ Password │
   │ [ ••••••••••••••••• ] │
   │ │
   │ Forgot Password? │
   │ │
   │ [ SIGN IN ] │
   │ │
   └─────────────────────────────┘
   API body
   {
   "email": "admin@driventa.com",
   "password": "Admin@123"
   }

On success, store accessToken, refreshToken, expiry, and the user profile including role.

POST /api/Auth/register
UI

Team Member → Add User

Add Team Member

First Name
[ John ]

Last Name
[ Doe ]

Email
[ john@example.com ]

Phone
[ +123456789 ]

Role
[ Dispatcher ▾ ]

Password
[ ••••••••••••••••••• ]

Confirm Password
[ ••••••••••••••••••• ]

              [ Cancel ] [ Create User ]

Body
{
"firstName": "John",
"lastName": "Doe",
"email": "john@example.com",
"password": "Password123!",
"confirmPassword": "Password123!",
"phoneNumber": "+1234567890",
"role": "Dispatcher"
}

Roles should be selected from your actual role set, not typed freely.

POST /api/Auth/refresh

No visible page. Handle automatically:

{
"refreshToken": "YOUR_REFRESH_TOKEN"
}

When a protected request returns 401, refresh once and retry; if refresh fails, log out.

POST /api/Auth/logout
UI

Profile menu:

Super Admin
admin@driventa.com

My Profile
Security
────────────────
🚪 Logout

Clicking logout shows:

Logout?

You will need to sign in again.

[ Cancel ] [ Logout ]
GET /api/Auth/me

Use silently at startup to restore the user session and determine role/permissions.

Forgot password
POST /api/Auth/forgot-password
Forgot Password?

Enter your email and we'll send password
reset instructions.

Email
[ admin@driventa.com ]

[ Send Reset Instructions ]
{
"email": "admin@driventa.com"
}
POST /api/Auth/reset-password
Create New Password

New Password
[ ••••••••••••• ]

Confirm Password
[ ••••••••••••• ]

[ Reset Password ]
{
"email": "admin@driventa.com",
"token": "RESET_TOKEN",
"newPassword": "NewPassword123!",
"confirmPassword": "NewPassword123!"
} 3. DASHBOARD
GET /api/Dashboard/summary

This is your primary KPI section:

┌────────────────┐ ┌────────────────┐ ┌────────────────┐ ┌────────────────┐
│ 📋 New Apps │ │ 🚚 Carriers │ │ 🚛 Active Loads│ │ 💰 Revenue │
│ 12 │ │ 48 │ │ 15 │ │ $18,500 │
└────────────────┘ └────────────────┘ └────────────────┘ └────────────────┘

┌────────────────┐ ┌────────────────┐ ┌────────────────┐ ┌────────────────┐
│ 🔍 In Review │ │ 🚛 Trucks │ │ 📍 In Transit │ │ ✓ Completed │
│ 5 │ │ 72 │ │ 8 │ │ 23 │
└────────────────┘ └────────────────┘ └────────────────┘ └────────────────┘

Expected data maps directly to these cards.

GET /api/Dashboard/load-status-summary
UI: Load Operations
LOAD STATUS

Available 10
Negotiating 5
Booked 8
Dispatched 6
Picked Up 4
In Transit 8
Delivered 3
Completed 25
Cancelled 2
Issues 1

Desktop: chart + legend.
Mobile: stacked status list. The documented API is specifically intended for this overview.

GET /api/Dashboard/recent-applications

Show the latest 5–8 applications:

RECENT APPLICATIONS View All →

APP-1042 Smith Trucking LLC New →
APP-1041 ABC Transport Reviewing →
APP-1040 Fast Freight Qualified →

Click → Application Details.

GET /api/Dashboard/recent-activity
RECENT ACTIVITY

👤 John Doe
Updated Load LD-1042
Changed status from Booked to Picked Up
10 min ago

👤 Super Admin
Assigned Smith Trucking to Sarah
32 min ago

📄 System
Document uploaded
1 hour ago

This matches the documented activity item structure.

GET /api/Dashboard/revenue-summary

Show only for finance-authorized users:

REVENUE OVERVIEW

This Month Outstanding
$18,500 $8,420

Revenue Trend
╭───────────────────────────╮
│ 📈 Chart │
╰───────────────────────────╯

Finance navigation should also be role-aware; your backend policies grant finance access only to SuperAdmin and Admin.

4. APPLICATIONS
   GET /api/Applications
   Perfect list UI
   Applications + Add Application

[ 🔍 Search name, email or company... ] [ Status ▾ ] [ Clear ]

All 124 New 12 Reviewing 5 Contacted 8
Qualified 6 Approved 3 Rejected 4 Onboarded 86

┌───────────────────────────────────────────────────────────────────────┐
│ Application │ Applicant / Company │ Equipment │ Trucks │ Status │ ⋮ │
├───────────────────────────────────────────────────────────────────────┤
│ APP-1042 │ John Smith │ Dry Van │ 5 │ New │ → │
│ APP-1041 │ ABC Transport │ Reefer │ 8 │ Review │ → │
└───────────────────────────────────────────────────────────────────────┘

Showing 1–20 of 124 ← Previous 1 2 3 Next →

Query:

?page=1&pageSize=20&search=trucking&status=0

The documented endpoint supports exactly these filters.

Status enum
0 New
1 Reviewing
2 Contacted
3 Qualified
4 Approved
5 Rejected
6 Onboarded

GET /api/Applications/{id}
Application Workspace
← Applications

APP-1042 [ Reviewing ▾ ]

John Smith
Smith Trucking LLC
john@example.com • +1 234 567 890

[ Overview ] [ Notes ]

┌──────────────────────────┐ ┌──────────────────────────┐
│ EQUIPMENT │ │ ASSIGNMENT │
│ Dry Van │ │ Sarah Johnson │
│ 5 Trucks │ │ [ Change Dispatcher ] │
└──────────────────────────┘ └──────────────────────────┘

MC Number: MC123456
DOT Number: DOT789012
Preferred Lanes: TX → CA
Additional Details: Experienced carrier

────────────────────────────────────────

[ Mark Contacted ] [ Approve ] [ Reject ]

                         [ Convert to Carrier → ]

The workflow should visually follow New → Reviewing → Contacted → Qualified → Approved → Convert → Onboarded.

PATCH /api/Applications/{id}

Use an Edit Application side sheet or full mobile page.

Only send changed fields:

{
"preferredLanes": "Texas, California, Florida"
}

Do not send the complete object unnecessarily because the endpoint is partial update.

POST /api/Applications/{id}/assign
UI
Assign Application

Assigned Dispatcher
[ Search team member... ▾ ]

Current workload
Sarah Johnson 12 applications
Mike Ross 8 applications
John Doe 5 applications

[ Cancel ] [ Assign ]
{
"assignedToUserId": "user-guid"
}

POST /api/Applications/{id}/notes
UI
INTERNAL NOTES

Sarah Johnson
Called carrier. Waiting for insurance documents.
Today, 10:30 AM

──────────────────────────────────

[ Write an internal note... ]
[ Add Note ]
{
"content": "Called carrier. Waiting for insurance documents."
}

POST /api/Applications/{id}/convert-to-carrier

Your fuller docs describe this action as /convert; map the button to the exact route your running Swagger exposes.

Conversion modal
Convert to Carrier

This will create a carrier profile using the
application information.

Assign Dispatcher
[ Sarah Johnson ▾ ]

Onboarding Notes
[ Carrier approved and ready for onboarding... ]

[ Cancel ] [ Convert Carrier ]
{
"assignedDispatcherId": "dispatcher-guid",
"notes": "Carrier approved and ready for onboarding."
}

5. CARRIERS
   GET /api/Carriers
   Carriers + Add Carrier

[ 🔍 Search company, contact or email ] [ Status ▾ ]

All Lead Onboarding Active Paused Inactive

┌─────────────────────────────────────────────────────────────────────┐
│ Company │ Contact │ MC/DOT │ Dispatcher │ Status │
├─────────────────────────────────────────────────────────────────────┤
│ ABC Transport │ John Smith │ MC123... │ Sarah │ Active │
└─────────────────────────────────────────────────────────────────────┘

Query supports page, pageSize, search and status.

Carrier statuses:

0 Lead
1 Onboarding
2 Active
3 Paused
4 Inactive
5 Suspended

POST /api/Carriers
Add Carrier UI
Add Carrier

COMPANY
Company Name*
Contact Name*
Email*
Phone*

AUTHORITY
MC Number
DOT Number

ADDRESS
Address
City
State
ZIP Code

OPERATIONS
Preferred Lanes

INTERNAL
Notes

[ Cancel ] [ Create Carrier ]
{
"companyName": "ABC Transport",
"contactName": "John Smith",
"email": "john@abctransport.com",
"phone": "+1234567890",
"mcNumber": "MC654321",
"dotNumber": "DOT210987",
"addressLine1": "123 Main St",
"city": "Dallas",
"state": "TX",
"zipCode": "75201",
"preferredLanes": "TX, CA, FL",
"notes": "Reliable carrier",
"applicationId": null
}

GET /api/Carriers/{id}
Carrier 360° workspace
← Carriers

ABC TRANSPORT 🟢 Active

MC654321 • DOT210987

[ Overview ] [ Loads ] [ Trucks ] [ Drivers ] [ Documents ]

┌─────────────┐ ┌─────────────┐ ┌─────────────┐
│ Loads │ │ Trucks │ │ Drivers │
│ 8 │ │ 12 │ │ 10 │
└─────────────┘ └─────────────┘ └─────────────┘

Assigned Dispatcher
Sarah Johnson [ Change ]

Contact Information
john@abctransport.com
+1234567890
PATCH /api/Carriers/{id}

Use the same Add Carrier form in edit mode.

[ Save Changes ]

Only changed values should be sent if your update DTO is partial.

POST /api/Carriers/{id}/assign-dispatcher
Assign Dispatcher

[ Search dispatcher... ▾ ]

Sarah Johnson
Current carriers: 8

Mike Ross
Current carriers: 12

[ Cancel ] [ Assign Dispatcher ]
{
"dispatcherId": "dispatcher-guid"
}

Carrier related endpoints
GET /api/Carriers/{id}/loads
GET /api/Carriers/{id}/trucks
GET /api/Carriers/{id}/drivers
GET /api/Carriers/{id}/documents

These should not open separate random pages. Use the tabs inside the Carrier Workspace.

6. TRUCKS
   GET /api/Trucks
   Trucks + Add Truck

[ Search truck number, make or model ]

[ Carrier ▾ ] [ Status ▾ ]

┌──────────────────────────────────────────────────────────────┐
│ Truck │ Equipment │ Carrier │ Make / Model │ Year │ Status │
├──────────────────────────────────────────────────────────────┤
│ T-001 │ Dry Van │ ABC │ Cascadia │ 2024 │ Available│
└──────────────────────────────────────────────────────────────┘

Supported filters are page, pageSize, search, carrierId and status.

POST /api/Trucks
Add Truck

Carrier\*
[ Select Carrier ▾ ]

Truck Number\*
[ T-001 ]

Equipment\*
[ Dry Van ▾ ]

Make\*
[ Freightliner ]

Model\*
[ Cascadia ]

Year\*
[ 2024 ]

License Plate\*
[ ABC1234 ]

License State\*
[ TX ]

[ Cancel ] [ Add Truck ]
{
"carrierId": "carrier-guid",
"truckNumber": "T-001",
"equipmentType": 0,
"make": "Freightliner",
"model": "Cascadia",
"year": 2024,
"licensePlate": "ABC1234",
"licenseState": "TX"
}

GET /api/Trucks/{id} / PATCH

Truck details:

T-001 Available

Freightliner Cascadia • 2024
Dry Van

Carrier
ABC Transport

License
ABC1234 • TX

[ Edit Truck ]

Edit opens the same form.

7. DRIVERS
   GET /api/Drivers
   Drivers + Add Driver

[ 🔍 Search name or email ]

[ Carrier ▾ ] [ Status ▾ ]

┌──────────────────────────────────────────────────────────────────┐
│ Driver │ Carrier │ Assigned Truck │ License │ Status │
├──────────────────────────────────────────────────────────────────┤
│ Mike Johnson │ ABC Transport │ T-001 │ DL12345 │ Driving│
└──────────────────────────────────────────────────────────────────┘
POST /api/Drivers
Add Driver

Carrier\*
[ Select Carrier ▾ ]

Assigned Truck
[ Not assigned ▾ ]

First Name*
Last Name*
Email*
Phone*
License Number*
License State*

[ Cancel ] [ Add Driver ]
{
"carrierId": "carrier-guid",
"truckId": "truck-guid-or-null",
"firstName": "Mike",
"lastName": "Johnson",
"email": "mike@example.com",
"phone": "+1234567890",
"licenseNumber": "DL123456",
"licenseState": "TX"
}

The truck can be null for an unassigned driver.

GET /api/Drivers/{id} / PATCH

Detail page:

Mike Johnson 🟢 Driving

ABC Transport

[ Overview ] [ Documents ] [ Loads ]

Phone
+1234567890

License
DL123456 • TX

Assigned Truck
T-001

[ Edit Driver ] 8. BROKERS
GET /api/Brokers
Brokers + Add Broker

[ 🔍 Search brokers ]

┌──────────────────────────────────────────────────────────────┐
│ Company │ Contact │ MC Number │ Rating │ Payment Terms │ │
├──────────────────────────────────────────────────────────────┤
│ XYZ Freight │ Jane Doe │ MC111222 │ ★★★★ │ Net 30 │ → │
└──────────────────────────────────────────────────────────────┘
POST /api/Brokers
Add Broker

Company Name*
Contact Name*
Email*
Phone*
MC Number
Address

Internal Rating
[ ★ ★ ★ ★ ☆ ]

Payment Notes
[ Net 30 ]

General Notes
[ Reliable broker ]

[ Cancel ] [ Add Broker ]
{
"companyName": "XYZ Freight Brokers",
"contactName": "Jane Doe",
"email": "jane@xyzfreight.com",
"phone": "+1234567890",
"mcNumber": "MC111222",
"address": "456 Broker Ave",
"internalRating": 4,
"paymentNotes": "Net 30",
"generalNotes": "Reliable broker"
}

GET /{id} and PATCH /{id} use the Broker Profile and edit form.

9. LOADS — THE MOST IMPORTANT OPERATIONS SCREEN
   GET /api/Loads
   Loads + Create Load

[ 🔍 Load number, pickup or delivery ]

[ Carrier ▾ ] [ Dispatcher ▾ ] [ Status ▾ ]

All 156 Available Negotiating Booked Dispatched In Transit

┌────────────────────────────────────────────────────────────────────────────┐
│ Load # │ Route │ Carrier │ Driver │ Pickup │ Rate │ Status │
├────────────────────────────────────────────────────────────────────────────┤
│ LD-1042 │ Dallas → LA │ ABC │ Mike │ Sep 1 │ $3500│ Transit │
└────────────────────────────────────────────────────────────────────────────┘

The documented filters are search, carrierId, status and dispatcherId.

POST /api/Loads

This deserves a multi-section form, not one giant popup.

Step 1 — Assignment
Create Load Step 1 of 3

Carrier\*
[ ABC Transport ▾ ]

Truck\*
[ T-001 ▾ ]

Driver\*
[ Mike Johnson ▾ ]

Broker\*
[ XYZ Freight Brokers ▾ ]

Equipment\*
[ Dry Van ▾ ]

                         [ Next → ]

Step 2 — Route
Create Load Step 2 of 3

PICKUP
City* State*
Dallas TX
Date & Time\*
Sep 1, 2026 • 8:00 AM

DELIVERY
City* State*
Los Angeles CA
Date & Time\*
Sep 3, 2026 • 6:00 PM

[ ← Back ] [ Next → ]
Step 3 — Financials
Create Load Step 3 of 3

Rate\*
$ 3,500

Miles\*
1,400

Rate Per Mile
$2.50 Auto calculated

Dispatch Fee Type
[ Percentage ▼ ]

Dispatch Fee
10%

Dispatch Fee Amount
$350 Auto calculated

Carrier Net
$3,150 Auto calculated

[ ← Back ] [ Create Load ]
API body
{
"carrierId": "carrier-guid",
"truckId": "truck-guid",
"driverId": "driver-guid",
"brokerId": "broker-guid",
"equipmentType": 0,
"pickupCity": "Dallas",
"pickupState": "TX",
"pickupDateTime": "2026-09-01T08:00:00Z",
"deliveryCity": "Los Angeles",
"deliveryState": "CA",
"deliveryDateTime": "2026-09-03T18:00:00Z",
"rate": 3500.0,
"miles": 1400,
"dispatchFeeType": "percentage",
"dispatchFeeValue": 10
}

Your backend calculates RPM, dispatch fee, and carrier net, so the UI should preview those calculations but treat backend values as authoritative.

GET /api/Loads/{id}
Load Command Center
← Loads

LD-1042 🚛 In Transit

Dallas, TX ─────────────────────→ Los Angeles, CA

[ Overview ] [ Status ] [ Notes ] [ Documents ]

STATUS TIMELINE

✓ Available ─ ✓ Negotiating ─ ✓ Booked ─ ✓ Dispatched
│
✓ Picked Up
│
● In Transit
│
○ Delivered ─ ○ Completed

ASSIGNMENT
Carrier ABC Transport
Truck T-001
Driver Mike Johnson
Broker XYZ Freight
Dispatcher Sarah Johnson

FINANCIALS
Rate $3,500
Miles 1,400
RPM $2.50
Dispatch Fee $350
Carrier Net $3,150

The timeline must follow your actual status enum.

PATCH /api/Loads/{id}

Use Edit Load.

Do not let users edit computed fields manually:

Rate Per Mile Auto calculated
Dispatch Fee Amount Auto calculated
Carrier Net Auto calculated
Status endpoint

Your documentation shows:

PATCH /api/Loads/{id}/status

while your pasted Swagger list showed POST. Use the exact method in your current backend/Swagger.

Status modal
Update Load Status

Current
Dispatched

New Status\*
[ Picked Up ▾ ]

Note
[ Driver confirmed pickup at 8:15 AM ]

[ Cancel ] [ Update Status ]
{
"status": 4,
"notes": "Driver picked up load."
}

Status 4 is PickedUp.

POST /api/Loads/{id}/notes
LOAD NOTES

[ Add operational note... ]
[ Add Note ]

Recommended pattern:

{
"content": "Customer requested delivery status update."
} 10. DOCUMENTS
POST /api/Documents/upload

Use a professional upload sheet:

Upload Document

Drag and drop your file here

                 📄

      PDF, JPG, PNG supported

[ Browse File ]

Document Type\*
[ Insurance ▾ ]

Attach To
[ Carrier ▾ ]

Carrier\*
[ ABC Transport ▾ ]

[ Cancel ] [ Upload ]

This is multipart/form-data, with:

file required
documentType required
carrierId optional
loadId optional
driverId optional

Document types include Insurance, W9, MC Authority, Rate Confirmation, BOL, POD, Carrier Agreement, Driver License, and Other.

GET /api/Documents/{id}

Click document → preview/download screen.

DELETE /api/Documents/{id}

Use confirmation:

Delete Document?

Insurance_2026.pdf will be removed.

[ Cancel ] [ Delete Document ] 11. BILLING
GET /api/Billing/invoices
Billing

[ Invoices ] [ Payments ]

Invoices + Create Invoice

[ 🔍 Search invoice or carrier ]

┌───────────────────────────────────────────────────────────────────────┐
│ Invoice │ Carrier │ Period │ Total │ Due Date │ Status │ Action │
├───────────────────────────────────────────────────────────────────────┤
│ INV-001 │ ABC │ Aug │ $1020 │ Sep 15 │ Sent │ View → │
└───────────────────────────────────────────────────────────────────────┘
POST /api/Billing/invoices

Use a multi-step invoice form.

Invoice header
Create Invoice

Carrier\*
[ ABC Transport ▾ ]

Billing Period
[ Aug 1, 2026 ] → [ Aug 31, 2026 ]

Due Date
[ Sep 15, 2026 ]
Line items
LINE ITEMS

Load Description Qty Unit Price Amount
LD-001 Dispatch Fee 1 $350 $350
LD-002 Dispatch Fee 1 $420 $420

[ + Add Item ]

Tax $250
Subtotal $770
────────────────────────────────
TOTAL $1,020
Body
{
"carrierId": "carrier-guid",
"periodStart": "2026-08-01T00:00:00Z",
"periodEnd": "2026-08-31T23:59:59Z",
"taxAmount": 250.0,
"dueDate": "2026-09-15T00:00:00Z",
"items": [
{
"loadId": "load-guid",
"description": "Dispatch fee - Load #LD001",
"quantity": 1,
"unitPrice": 350.0
},
{
"loadId": "load-guid",
"description": "Dispatch fee - Load #LD002",
"quantity": 1,
"unitPrice": 420.0
}
]
}

GET /api/Billing/invoices/{id}
Invoice detail
← Invoices

INV-2026-00124 SENT

ABC Transport

Period
Aug 1 → Aug 31

Due Date
Sep 15, 2026

LINE ITEMS
LD-001 Dispatch Fee $350
LD-002 Dispatch Fee $420

Subtotal $770
Tax $250
──────────────────────────────────────────
TOTAL $1,020

PAYMENT HISTORY
No payments yet

[ Update Status ] [ Record Payment ]
POST /api/Billing/invoices/{id}/status

Use a status dropdown with:

Draft
Sent
Partially Paid
Paid
Overdue
Cancelled

Those are the documented invoice statuses.

POST /api/Billing/invoices/{id}/payments
Record Payment

Amount\*
[ $1,020 ]

Payment Method\*
[ Bank Transfer ▾ ]

Transaction Reference
[ TXN-2026-001 ]

[ Cancel ] [ Record Payment ]
{
"amount": 1020.0,
"paymentMethod": "Bank Transfer",
"transactionReference": "TXN-2026-001"
}

GET /api/Billing/payments

Create a Payments History screen:

Payments

[ Search transaction reference ]

┌──────────────────────────────────────────────────────────────┐
│ Payment │ Invoice │ Method │ Amount │ Status │ Paid At │
├──────────────────────────────────────────────────────────────┤
│ TXN-001 │ INV-001 │ Bank │ $1020 │ Paid │ Aug 28 │
└──────────────────────────────────────────────────────────────┘ 12. MESSAGES
GET /api/Messages/conversations

Desktop:

┌────────────────────────────┬───────────────────────────────────────────┐
│ INBOX │ John Doe │
│ [ 🔍 Search conversations ]│ john@example.com │
│ ├───────────────────────────────────────────┤
│ ● John Doe 2 │ │
│ Need dispatch service │ Visitor: Hello, I need dispatch services.│
│ │ │
│ ○ Mike Ross │ Hello, how can I help?│
│ │ │
│ ○ ABC Transport │ │
│ ├───────────────────────────────────────────┤
│ │ Type your message... Send → │
└────────────────────────────┴───────────────────────────────────────────┘

Conversation list supports pagination/search/status/assignment in the fuller docs.

GET /api/Messages/conversations/{id}

Open conversation detail with message bubbles and visitor information. The documented response includes visitor details, status, messages, sender type, read state, and timestamps.

Send message

Your fuller docs use:

POST /api/Messages/send

Body:

{
"conversationId": "conversation-guid",
"content": "Hello, how can I help you?"
}

For instant delivery, connect the Flutter screen to SignalR and update the UI on ReceiveMessage.

13. NOTIFICATIONS
    GET /api/Notifications

I would make this both:

a top-right notification dropdown
a full Notifications page
Notifications Mark all as read

● New Application
Smith Trucking submitted APP-1042
2 minutes ago

● New Message
John Doe sent you a message
12 minutes ago

○ Load Status Changed
LD-1042 is now In Transit
1 hour ago
GET /api/Notifications/unread-count

Drives:

🔔 3
PATCH /api/Notifications/{id}/read

Clicking a notification marks it read and navigates to the related record.

POST /api/Notifications/read-all

Button:

[ ✓ Mark all as read ]

Notification types should visually map to the documented types.

Use SignalR ReceiveNotification to instantly insert new notifications into the bell and page.

14. REPORTS

Use one page with tabs:

Reports

[ Load Performance ] [ Revenue ] [ Carriers ] [ Dispatchers ]
GET /api/Reports/loads
LOAD PERFORMANCE

Total Loads Active Loads Completed Cancelled
150 23 110 7

Average Rate Average RPM Total Revenue
$2,800 $2.15 $308,000

GET /api/Reports/revenue
REVENUE ANALYTICS

Total Revenue $500,000
Dispatch Fees $50,000
Carrier Payouts $450,000
Revenue Per Load $3,333.33

[ Monthly Revenue Chart ]

Month Revenue Loads
Aug 2026 $45,000 18

GET /api/Reports/carriers

Use:

Carrier Performance

Carrier Loads Completed Revenue Status
ABC Transport 45 40 $125K Active
GET /api/Reports/dispatchers
Dispatcher Performance

Dispatcher Applications Loads Completed
Sarah Johnson 24 42 38
Mike Ross 18 35 30 15. PUBLIC APPLICATIONS
POST /api/public/applications

This is primarily for your public website, but the dashboard needs to react to new submissions.

Example body:

{
"fullName": "John Doe",
"email": "john@example.com",
"phone": "+1234567890",
"companyName": "Doe Trucking LLC",
"equipmentType": 0,
"truckCount": 5,
"mcNumber": "MC123456",
"dotNumber": "DOT789012",
"preferredLanes": "TX to CA",
"additionalDetails": "Experienced carrier looking for dispatch services"
}

On success:

Website Submission
↓
Application created
↓
Activity log
↓
Notification
↓
Flutter dashboard updates

The endpoint is documented as rate-limited to 10 requests/minute.

16. PUBLIC CONTACT
    POST /api/public/contact

Website form body:

{
"fullName": "John Doe",
"email": "john@example.com",
"phone": "+1234567890",
"subject": "Dispatch Services",
"message": "I would like more information."
}

Dashboard effect: create a new conversation/message/notification depending on your backend implementation.

17. PUBLIC CHAT SESSION
    POST /api/public/chat/session

Website visitor:

{
"visitorName": "John Doe",
"visitorEmail": "john@example.com",
"visitorPhone": "+1234567890"
}

Response gives:

{
"success": true,
"data": {
"conversationId": "conversation-guid",
"visitorId": "visitor-guid"
}
}

The dashboard then receives this as a conversation in the Inbox.

🔥 Final Flutter Screen Structure
lib/
├── app/
│ ├── app.dart
│ ├── router.dart
│ └── theme/
│ ├── app_theme.dart
│ └── app_colors.dart
│
├── core/
│ ├── api/
│ │ ├── api_client.dart
│ │ ├── api_interceptor.dart
│ │ └── api_response.dart
│ ├── auth/
│ │ ├── token_storage.dart
│ │ └── permission_service.dart
│ ├── realtime/
│ │ ├── chat_signalr_service.dart
│ │ └── notification_signalr_service.dart
│ └── responsive/
│ └── breakpoints.dart
│
├── features/
│ ├── auth/
│ │ ├── login_page.dart
│ │ ├── forgot_password_page.dart
│ │ └── reset_password_page.dart
│ │
│ ├── dashboard/
│ │ └── dashboard_page.dart
│ │
│ ├── applications/
│ │ ├── applications_page.dart
│ │ ├── application_detail_page.dart
│ │ └── application_form_page.dart
│ │
│ ├── carriers/
│ │ ├── carriers_page.dart
│ │ └── carrier_detail_page.dart
│ │
│ ├── loads/
│ │ ├── loads_page.dart
│ │ ├── load_detail_page.dart
│ │ └── create_load_wizard.dart
│ │
│ ├── trucks/
│ ├── drivers/
│ ├── brokers/
│ ├── documents/
│ ├── billing/
│ │ ├── invoices_page.dart
│ │ ├── invoice_detail_page.dart
│ │ └── payments_page.dart
│ ├── messages/
│ ├── notifications/
│ └── reports/
│
└── shared/
└── widgets/
├── app_shell.dart
├── responsive_navigation.dart
├── desktop_sidebar.dart
├── mobile_bottom_nav.dart
├── stat_card.dart
├── status_chip.dart
├── responsive_table.dart
├── filter_bar.dart
├── app_empty_state.dart
├── app_loading_state.dart
└── confirm_dialog.dart
My final UX recommendation

Don't design the dashboard as "one screen per endpoint." Instead:

GET endpoints → list/detail/dashboard data
POST create endpoints → forms or wizards
PATCH endpoints → edit sheets/status dialogs
Assign endpoints → focused selection modal
Notes endpoints → timeline/comment components
Document endpoints → upload/preview/delete workflow
Message endpoints + SignalR → real-time inbox
Notification endpoints + SignalR → global notification system

This will give you a dashboard that feels like a real logistics SaaS product rather than a Swagger UI converted into Flutter.
