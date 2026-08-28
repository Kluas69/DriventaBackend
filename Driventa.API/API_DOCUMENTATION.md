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

| Property | Value |
|----------|-------|
| **Name** | Driventa API |
| **Version** | v1 |
| **Framework** | ASP.NET Core 10.0 |
| **Database** | PostgreSQL 16 (via Npgsql + EF Core 10.0.11) |
| **Auth** | JWT Bearer + ASP.NET Core Identity (Guid-based) |
| **Real-time** | SignalR (Chat + Notifications) |
| **Validation** | FluentValidation 11.12.0 |
| **API Docs** | Swashbuckle/Swagger 7.3.1 |

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

| Field | Value |
|-------|-------|
| Email | `admin@driventa.com` |
| Password | `Admin@123` |
| Role | SuperAdmin |

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

| Setting | Value |
|---------|-------|
| Access Token Expiry | 15 minutes |
| Refresh Token Expiry | 7 days |
| Clock Skew | 1 minute |

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

| Role | Description |
|------|-------------|
| **SuperAdmin** | Full access to everything |
| **Admin** | Manage applications, carriers, loads, trucks, drivers, finance, reports |
| **DispatchManager** | Manage applications, carriers, loads, trucks, drivers, view brokers/reports |
| **Dispatcher** | Manage applications, loads, view reports |
| **Sales** | Registered role (no specific authorization policies mapped) |

### Authorization Policies

| Policy | Allowed Roles |
|--------|---------------|
| `CanManageApplications` | SuperAdmin, Admin, DispatchManager, Dispatcher |
| `CanManageCarriers` | SuperAdmin, Admin, DispatchManager |
| `CanManageLoads` | SuperAdmin, Admin, DispatchManager, Dispatcher |
| `CanManageTrucks` | SuperAdmin, Admin, DispatchManager |
| `CanManageDrivers` | SuperAdmin, Admin, DispatchManager |
| `CanViewBrokers` | SuperAdmin, Admin, DispatchManager |
| `CanManageFinance` | SuperAdmin, Admin |
| `CanViewReports` | SuperAdmin, Admin, DispatchManager, Dispatcher |
| `CanManageSettings` | SuperAdmin |
| `CanAssignDispatchers` | SuperAdmin, Admin, DispatchManager |

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

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/api/Auth/login` | No | Login and get JWT tokens |
| POST | `/api/Auth/register` | No | Register new user |
| POST | `/api/Auth/refresh` | No | Refresh access token |
| POST | `/api/Auth/logout` | Yes | Revoke refresh token |
| GET | `/api/Auth/me` | Yes | Get current user profile |
| POST | `/api/Auth/forgot-password` | No | Request password reset |
| POST | `/api/Auth/reset-password` | No | Reset password with token |

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

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/Applications` | Authorize | List all applications (paginated, filterable) |
| POST | `/api/Applications` | Authorize (CanManageApplications) | Create new application |
| GET | `/api/Applications/{id}` | Authorize | Get application by ID |
| PATCH | `/api/Applications/{id}` | Authorize (CanManageApplications) | Update application |
| DELETE | `/api/Applications/{id}` | Authorize (SuperAdmin, Admin) | Soft delete application |
| POST | `/api/Applications/{id}/assign` | Authorize (CanManageApplications) | Assign to user |
| POST | `/api/Applications/{id}/contact` | Authorize (CanManageApplications) | Mark as contacted |
| POST | `/api/Applications/{id}/approve` | Authorize (CanManageApplications) | Approve application |
| POST | `/api/Applications/{id}/reject` | Authorize (CanManageApplications) | Reject application |
| POST | `/api/Applications/{id}/convert` | Authorize (CanManageApplications) | Convert to carrier |
| GET | `/api/Applications/{id}/notes` | Authorize | Get application notes |
| POST | `/api/Applications/{id}/notes` | Authorize (CanManageApplications) | Add note |

#### Query Parameters (GET `/api/Applications`)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `page` | int | 1 | Page number |
| `pageSize` | int | 20 | Items per page |
| `search` | string? | null | Search by name, email, company |
| `status` | ApplicationStatus? | null | Filter by status |

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

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/Carriers` | Authorize | List all carriers (paginated, filterable) |
| POST | `/api/Carriers` | Authorize (CanManageCarriers) | Create new carrier |
| GET | `/api/Carriers/{id}` | Authorize | Get carrier by ID |
| PATCH | `/api/Carriers/{id}` | Authorize (CanManageCarriers) | Update carrier |
| DELETE | `/api/Carriers/{id}` | Authorize (SuperAdmin, Admin) | Soft delete carrier |
| POST | `/api/Carriers/{id}/assign-dispatcher` | Authorize (CanAssignDispatchers) | Assign dispatcher |
| GET | `/api/Carriers/{id}/notes` | Authorize | Get carrier notes |
| POST | `/api/Carriers/{id}/notes` | Authorize (CanManageCarriers) | Add note |
| GET | `/api/Carriers/{id}/trucks` | Authorize | Get carrier's trucks |
| GET | `/api/Carriers/{id}/drivers` | Authorize | Get carrier's drivers |
| GET | `/api/Carriers/{id}/loads` | Authorize | Get carrier's loads |

#### Query Parameters (GET `/api/Carriers`)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `page` | int | 1 | Page number |
| `pageSize` | int | 20 | Items per page |
| `search` | string? | null | Search by company, contact, email |
| `status` | CarrierStatus? | null | Filter by status |

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

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/Trucks` | Authorize | List all trucks (paginated, filterable) |
| POST | `/api/Trucks` | Authorize (CanManageTrucks) | Create new truck |
| GET | `/api/Trucks/{id}` | Authorize | Get truck by ID |
| PATCH | `/api/Trucks/{id}` | Authorize | Update truck |

#### Query Parameters (GET `/api/Trucks`)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `page` | int | 1 | Page number |
| `pageSize` | int | 20 | Items per page |
| `search` | string? | null | Search by truck number, make, model |
| `carrierId` | Guid? | null | Filter by carrier |
| `status` | TruckStatus? | null | Filter by status |

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

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/Drivers` | Authorize | List all drivers (paginated, filterable) |
| POST | `/api/Drivers` | Authorize (CanManageDrivers) | Create new driver |
| GET | `/api/Drivers/{id}` | Authorize | Get driver by ID |
| PATCH | `/api/Drivers/{id}` | Authorize | Update driver |
| DELETE | `/api/Drivers/{id}` | Authorize (SuperAdmin, Admin) | Soft delete driver |

#### Query Parameters (GET `/api/Drivers`)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `page` | int | 1 | Page number |
| `pageSize` | int | 20 | Items per page |
| `search` | string? | null | Search by name, email |
| `carrierId` | Guid? | null | Filter by carrier |
| `status` | DriverStatus? | null | Filter by status |

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

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/Loads` | Authorize | List all loads (paginated, filterable) |
| POST | `/api/Loads` | Authorize (CanManageLoads) | Create new load |
| GET | `/api/Loads/{id}` | Authorize | Get load by ID |
| PATCH | `/api/Loads/{id}` | Authorize (CanManageLoads) | Update load |
| DELETE | `/api/Loads/{id}` | Authorize (SuperAdmin, Admin) | Soft delete load |
| PATCH | `/api/Loads/{id}/status` | Authorize (CanManageLoads) | Update load status |
| GET | `/api/Loads/{id}/notes` | Authorize | Get load notes |
| POST | `/api/Loads/{id}/notes` | Authorize (CanManageLoads) | Add note |

#### Query Parameters (GET `/api/Loads`)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `page` | int | 1 | Page number |
| `pageSize` | int | 20 | Items per page |
| `search` | string? | null | Search by load number, cities |
| `carrierId` | Guid? | null | Filter by carrier |
| `status` | LoadStatus? | null | Filter by status |
| `dispatcherId` | Guid? | null | Filter by dispatcher |

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
  "rate": 3500.00,
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

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/Brokers` | Authorize (CanViewBrokers) | List all brokers (paginated) |
| POST | `/api/Brokers` | Authorize (CanViewBrokers) | Create new broker |
| GET | `/api/Brokers/{id}` | Authorize (CanViewBrokers) | Get broker by ID |
| PATCH | `/api/Brokers/{id}` | Authorize (CanViewBrokers) | Update broker |
| DELETE | `/api/Brokers/{id}` | Authorize (SuperAdmin, Admin) | Soft delete broker |

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

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/api/Documents/upload` | Authorize | Upload document (multipart/form-data) |
| GET | `/api/Documents/{id}` | Authorize | Get document by ID |
| DELETE | `/api/Documents/{id}` | Authorize (SuperAdmin, Admin) | Delete document |

#### POST `/api/Documents/upload`

**Request:** `multipart/form-data`

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `file` | IFormFile | Yes | File to upload |
| `documentType` | DocumentType | Yes | Type of document (query param) |
| `carrierId` | Guid? | No | Associated carrier (query param) |
| `loadId` | Guid? | No | Associated load (query param) |
| `driverId` | Guid? | No | Associated driver (query param) |

**Document Types:** Insurance(0), W9(1), MC_Authority(2), RateConfirmation(3), BOL(4), POD(5), CarrierAgreement(6), DriverLicense(7), Other(8)

---

### Billing Controller

**Route:** `api/Billing`

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/Billing/invoices` | Authorize (CanManageFinance) | List all invoices (paginated) |
| POST | `/api/Billing/invoices` | Authorize (CanManageFinance) | Create invoice |
| GET | `/api/Billing/invoices/{id}` | Authorize (CanManageFinance) | Get invoice by ID |
| PATCH | `/api/Billing/invoices/{id}/status` | Authorize (CanManageFinance) | Update invoice status |
| POST | `/api/Billing/invoices/{id}/payments` | Authorize (CanManageFinance) | Record payment |

#### POST `/api/Billing/invoices` — Create Invoice

**Request:**
```json
{
  "carrierId": "carrier-guid",
  "periodStart": "2026-08-01T00:00:00Z",
  "periodEnd": "2026-08-31T23:59:59Z",
  "taxAmount": 250.00,
  "dueDate": "2026-09-15T00:00:00Z",
  "items": [
    {
      "loadId": "load-guid",
      "Description": "Dispatch fee - Load #LD001",
      "quantity": 1,
      "unitPrice": 350.00
    },
    {
      "loadId": "load-guid",
      "Description": "Dispatch fee - Load #LD002",
      "quantity": 1,
      "unitPrice": 420.00
    }
  ]
}
```

#### POST `/api/Billing/invoices/{id}/payments` — Record Payment

**Request:**
```json
{
  "amount": 1020.00,
  "paymentMethod": "Bank Transfer",
  "transactionReference": "TXN-2026-001"
}
```

---

### Dashboard Controller

**Route:** `api/Dashboard`

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/Dashboard/summary` | Authorize | Get dashboard summary stats |
| GET | `/api/Dashboard/load-status-summary` | Authorize | Get load counts by status |
| GET | `/api/Dashboard/recent-activity` | Authorize | Get recent activity logs |
| GET | `/api/Dashboard/revenue-summary` | Authorize (CanManageFinance) | Get revenue summary |
| POST | `/api/Dashboard/contact` | No | Submit contact form |

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
  "dispatchRevenueThisMonth": 18500.00
}
```

---

### Reports Controller

**Route:** `api/Reports`

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/Reports/loads` | Authorize (CanViewReports) | Load performance report |
| GET | `/api/Reports/revenue` | Authorize (CanViewReports) | Revenue report with monthly breakdown |
| GET | `/api/Reports/carriers` | Authorize (CanViewReports) | Carrier performance report |
| GET | `/api/Reports/dispatchers` | Authorize (CanViewReports) | Dispatcher performance report |

#### GET `/api/Reports/loads` — Response

```json
{
  "totalLoads": 150,
  "activeLoads": 23,
  "completedLoads": 110,
  "cancelledLoads": 7,
  "averageRate": 2800.00,
  "averageRpm": 2.15,
  "totalRevenue": 308000.00
}
```

#### GET `/api/Reports/revenue` — Response

```json
{
  "totalRevenue": 500000.00,
  "totalDispatchFees": 50000.00,
  "totalCarrierPayouts": 450000.00,
  "averageRevenuePerLoad": 3333.33,
  "monthlyBreakdown": [
    {
      "year": 2026,
      "month": 8,
      "revenue": 45000.00,
      "loadCount": 18
    }
  ]
}
```

---

### Messages Controller

**Route:** `api/Messages`

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/Messages/conversations` | Authorize | List all conversations |
| GET | `/api/Messages/conversations/{id}` | Authorize | Get conversation with messages |
| POST | `/api/Messages/send` | Authorize | Send a message |
| PATCH | `/api/Messages/conversations/{id}/read` | Authorize | Mark conversation as read |

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

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/api/public/Applications` | No (rate limited) | Submit carrier application |

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

| Method | Parameters | Description |
|--------|-----------|-------------|
| `JoinConversation` | `conversationId: string` | Join a conversation group |
| `SendMessage` | `conversationId: string, message: string` | Send a message |
| `MarkAsRead` | `conversationId: string` | Mark all messages as read |

#### Server Events

| Event | Payload | Description |
|-------|---------|-------------|
| `ReceiveMessage` | `{ messageId, message, senderUserId, senderType, timestamp }` | New message received |
| `MessagesRead` | `conversationId: string` | Messages marked as read |

---

### Notification Hub

**URL:** `ws://localhost:5165/hubs/notifications`

**Authentication:** Required (JWT)

#### Client Methods

| Method | Parameters | Description |
|--------|-----------|-------------|
| `JoinPersonalGroup` | None | Join personal notification group |
| `SendNotificationToUser` | `targetUserId: Guid, title: string, message: string` | Send notification to self |

#### Server Events

| Event | Payload | Description |
|-------|---------|-------------|
| `Connected` | `{ userId }` | Connection established |
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

| Field | Type | Description |
|-------|------|-------------|
| Id | Guid | Primary key |
| CreatedAt | DateTimeOffset | Creation timestamp |
| UpdatedAt | DateTimeOffset | Last update timestamp |
| CreatedByUserId | Guid? | Creator user ID |
| UpdatedByUserId | Guid? | Last modifier user ID |
| IsDeleted | bool | Soft delete flag |

#### Application

| Field | Type | Description |
|-------|------|-------------|
| ApplicationNumber | string(20) | Auto-generated number |
| FullName | string(200) | Applicant name |
| Email | string(200) | Applicant email |
| Phone | string(50) | Phone number |
| CompanyName | string(200) | Company name |
| EquipmentType | EquipmentType | Equipment needed |
| TruckCount | int | Number of trucks |
| McNumber | string(50)? | MC authority number |
| DotNumber | string(50)? | DOT number |
| PreferredLanes | string(500)? | Preferred routes |
| AdditionalDetails | string(2000)? | Extra info |
| Status | ApplicationStatus | Current status |
| AssignedToUserId | Guid? | Assigned dispatcher |
| SubmittedAt | DateTimeOffset | Submission date |
| ContactedAt | DateTimeOffset? | Contact date |
| ApprovedAt | DateTimeOffset? | Approval date |
| RejectedAt | DateTimeOffset? | Rejection date |
| ConvertedCarrierId | Guid? | Linked carrier |

#### Carrier

| Field | Type | Description |
|-------|------|-------------|
| CompanyName | string(200) | Company name |
| ContactName | string(200) | Primary contact |
| Email | string(200) | Email |
| Phone | string(50) | Phone |
| McNumber | string(50)? | MC authority |
| DotNumber | string(50)? | DOT number |
| AddressLine1-2 | string(200)? | Address |
| City | string(100)? | City |
| State | string(50)? | State |
| ZipCode | string(20)? | ZIP |
| Status | CarrierStatus | Current status |
| AssignedDispatcherId | Guid? | Assigned dispatcher |
| PreferredLanes | string(500)? | Preferred routes |
| Notes | string(2000)? | Notes |
| ApplicationId | Guid? | Linked application |

#### Load

| Field | Type | Description |
|-------|------|-------------|
| LoadNumber | string(20) | Auto-generated number |
| CarrierId | Guid | Assigned carrier |
| TruckId | Guid? | Assigned truck |
| DriverId | Guid? | Assigned driver |
| BrokerId | Guid? | Broker |
| DispatcherId | Guid? | Dispatcher |
| EquipmentType | EquipmentType | Required equipment |
| PickupCity/State | string | Pickup location |
| PickupDateTime | DateTimeOffset | Pickup time |
| DeliveryCity/State | string | Delivery location |
| DeliveryDateTime | DateTimeOffset | Delivery time |
| Rate | decimal(12,2) | Load rate ($) |
| Miles | int? | Distance |
| RatePerMile | decimal(8,2)? | Calculated RPM |
| DispatchFeeType | string(50)? | "percentage" or "flat" |
| DispatchFeeValue | decimal(12,2)? | Fee amount/rate |
| DispatchFeeAmount | decimal(12,2)? | Calculated fee |
| CarrierNetAmount | decimal(12,2)? | Calculated net |
| Status | LoadStatus | Current status |

#### Invoice

| Field | Type | Description |
|-------|------|-------------|
| InvoiceNumber | string(30) | Auto-generated number |
| CarrierId | Guid | Billed carrier |
| PeriodStart/End | DateTimeOffset | Billing period |
| Subtotal | decimal(12,2) | Items total |
| TaxAmount | decimal(12,2) | Tax |
| TotalAmount | decimal(12,2) | Grand total |
| Status | InvoiceStatus | Current status |
| DueDate | DateTimeOffset? | Due date |
| PaidAt | DateTimeOffset? | Payment date |

---

## Enums Reference

### ApplicationStatus
| Value | Name |
|-------|------|
| 0 | New |
| 1 | Reviewing |
| 2 | Contacted |
| 3 | Qualified |
| 4 | Approved |
| 5 | Rejected |
| 6 | Onboarded |

### CarrierStatus
| Value | Name |
|-------|------|
| 0 | Lead |
| 1 | Onboarding |
| 2 | Active |
| 3 | Paused |
| 4 | Inactive |
| 5 | Suspended |

### LoadStatus
| Value | Name |
|-------|------|
| 0 | Available |
| 1 | Negotiating |
| 2 | Booked |
| 3 | Dispatched |
| 4 | PickedUp |
| 5 | InTransit |
| 6 | Delivered |
| 7 | Completed |
| 8 | Cancelled |
| 9 | Issue |

### TruckStatus
| Value | Name |
|-------|------|
| 0 | Available |
| 1 | OnLoad |
| 2 | Maintenance |
| 3 | Inactive |

### DriverStatus
| Value | Name |
|-------|------|
| 0 | Available |
| 1 | Assigned |
| 2 | Driving |
| 3 | OffDuty |
| 4 | Inactive |

### EquipmentType
| Value | Name |
|-------|------|
| 0 | DryVan |
| 1 | Reefer |
| 2 | Flatbed |
| 3 | StepDeck |
| 4 | BoxTruck |
| 5 | Hotshot |
| 6 | PowerOnly |

### InvoiceStatus
| Value | Name |
|-------|------|
| 0 | Draft |
| 1 | Sent |
| 2 | PartiallyPaid |
| 3 | Paid |
| 4 | Overdue |
| 5 | Cancelled |

### PaymentStatus
| Value | Name |
|-------|------|
| 0 | Pending |
| 1 | Completed |
| 2 | Failed |
| 3 | Refunded |

### DocumentType
| Value | Name |
|-------|------|
| 0 | Insurance |
| 1 | W9 |
| 2 | MC_Authority |
| 3 | RateConfirmation |
| 4 | BOL |
| 5 | POD |
| 6 | CarrierAgreement |
| 7 | DriverLicense |
| 8 | Other |

### NotificationType
| Value | Name |
|-------|------|
| 0 | NewApplication |
| 1 | NewMessage |
| 2 | LoadStatusChanged |
| 3 | DocumentExpiring |
| 4 | DocumentUploaded |
| 5 | CarrierAssigned |

### SenderType
| Value | Name |
|-------|------|
| 0 | Visitor |
| 1 | Admin |
| 2 | Dispatcher |
| 3 | System |

### UserRole
| Value | Name |
|-------|------|
| 0 | SuperAdmin |
| 1 | Admin |
| 2 | DispatchManager |
| 3 | Dispatcher |
| 4 | Sales |

---

## Error Handling

### Exception to HTTP Status Mapping

| Exception | HTTP Status | Description |
|-----------|-------------|-------------|
| `ValidationException` | 400 Bad Request | FluentValidation failure |
| `ArgumentException` | 400 Bad Request | Invalid argument |
| `KeyNotFoundException` | 404 Not Found | Resource not found |
| `UnauthorizedAccessException` | 401 Unauthorized | Auth required |
| `InvalidOperationException` | 409 Conflict | Business rule violation |
| `HubException` | 400 Bad Request | SignalR error |
| Other exceptions | 500 Internal Server Error | Unexpected error |

### Error Response Format

```json
{
  "success": false,
  "message": "Validation failed.",
  "data": null,
  "errors": [
    "Email is required.",
    "Password must be at least 8 characters."
  ]
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

| Origin | Purpose |
|--------|---------|
| `http://localhost:3000` | Local development (React) |
| `http://localhost:5173` | Local development (Vite) |
| `https://driventa.us` | Production website |
| `https://dashboard.driventa.us` | Production dashboard |

### Rate Limiting

| Policy | Limit | Window | Queue |
|--------|-------|--------|-------|
| `PublicEndpoints` | 10 requests | 1 minute | 0 (reject immediately) |

### JWT Settings

| Setting | Value |
|---------|-------|
| Issuer | `Driventa.API` |
| Audience | `Driventa.Dashboard` |
| Access Token Expiry | 15 minutes |
| Refresh Token Expiry | 7 days |
| Clock Skew | 1 minute |
| SignalR Auth | Query string `?access_token=<jwt>` |

### Password Policy

| Rule | Value |
|------|-------|
| RequireDigit | true |
| RequireLowercase | true |
| RequireUppercase | true |
| RequireNonAlphanumeric | false |
| RequiredLength | 8 |
| RequireUniqueEmail | true |

---

*Generated from Driventa API v1 source code*
