# Driventa — Complete Frontend Integration Report

> **For Dashboard Designers & Frontend Engineers**
> Everything you need to build, connect, and sync the Driventa dashboard.

---

## Table of Contents

1. [Quick Start](#1-quick-start)
2. [Authentication](#2-authentication)
3. [Roles & Permissions](#3-roles--permissions)
4. [Dashboard Navigation Structure](#4-dashboard-navigation)
5. [API Response Format](#5-api-response-format)
6. [SignalR Real-Time Hubs](#6-signalr-hubs)
7. [REST API — All Endpoints](#7-rest-api)
8. [Data Schemas — All Entities](#8-data-schemas)
9. [Enums — All Values](#9-enums)
10. [Notification System — Complete Reference](#10-notification-system)
11. [Chat System — Complete Reference](#11-chat-system)
12. [File Upload — Documents](#12-file-upload)
13. [Error Handling](#13-error-handling)
14. [Implementation Checklist](#14-checklist)
15. [Code Examples](#15-code-examples)

---

## 1. Quick Start

### Base URLs

| Environment | URL |
|-------------|-----|
| Local | `http://localhost:5165` |
| Production API | `https://api.driventa.us` |
| Production Dashboard | `https://dashboard.driventa.us` |
| Production Website | `https://www.driventa.us` |

### Default Login

| Field | Value |
|-------|-------|
| Email | `admin@driventa.com` |
| Password | `Admin@123` |
| Role | SuperAdmin |

### First 5 Steps

1. **Login:** `POST /api/Auth/login` → get `accessToken` + `refreshToken`
2. **Connect SignalR:** Connect to `/hubs/notifications` with JWT
3. **Join groups:** Call `JoinPersonalGroup` + `JoinAdminGroup` on hubs
4. **Fetch data:** Call REST endpoints (applications, carriers, loads, etc.)
5. **Listen for updates:** Handle `ReceiveNotification`, `ApplicationCreated`, `DashboardUpdate` events

---

## 2. Authentication

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
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIs...",
    "refreshToken": "eyJhbGciOiJIUzI1NiIs...",
    "expiresAt": "2026-08-31T12:00:00Z",
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

Every authenticated request needs:
```
Authorization: Bearer <accessToken>
```

### Token Refresh

```
POST /api/Auth/refresh
```
```json
{
  "refreshToken": "<your-refresh-token>"
}
```

### Token Lifetimes

| Setting | Value |
|---------|-------|
| Access Token Expiry | 15 minutes |
| Refresh Token Expiry | 7 days |
| Clock Skew | 1 minute |

### SignalR Authentication

For SignalR hubs, pass JWT via query string:
```
/hubs/notifications?access_token=<jwt>
/hubs/applications?access_token=<jwt>
/hubs/dashboard?access_token=<jwt>
/hubs/chat?access_token=<jwt>  (optional for visitors)
```

### Register New User

```
POST /api/Auth/register
```
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

### Get Current User

```
GET /api/Auth/me
```

### Logout

```
POST /api/Auth/logout
```

### Forgot / Reset Password

```
POST /api/Auth/forgot-password
POST /api/Auth/reset-password
```

---

## 3. Roles & Permissions

### Roles

| Role | Access Level |
|------|-------------|
| **SuperAdmin** | Full access to everything, including role management |
| **Admin** | Full access except role management |
| **DispatchManager** | Applications, carriers, loads, billing (view/create), reports |
| **Dispatcher** | Applications (view/edit), carriers (view), loads (full), reports |
| **Sales** | Applications (view/edit), carriers (create), loads (view), reports |

### Permission-Based Policies

| Policy | What It Guards |
|--------|----------------|
| `users.view` | Viewing user list |
| `users.create` | Creating users |
| `users.edit` | Editing users |
| `users.delete` | Deleting users |
| `applications.view` | Viewing applications |
| `applications.edit` | Editing applications |
| `applications.assign` | Assigning applications |
| `applications.convert` | Converting application to carrier |
| `carriers.view` | Viewing carriers |
| `carriers.create` | Creating carriers |
| `carriers.edit` | Editing carriers |
| `loads.view` | Viewing loads |
| `loads.create` | Creating loads |
| `loads.edit` | Editing loads |
| `loads.updateStatus` | Changing load status |
| `billing.view` | Viewing invoices |
| `billing.create` | Creating invoices |
| `billing.manage` | Managing billing settings |
| `reports.view` | Viewing reports |
| `roles.manage` | Managing roles (SuperAdmin only) |

---

## 4. Dashboard Navigation

```
DRIVENTA
━━━━━━━━━━━━━━━━━━━━

🏠 Dashboard                    → GET /api/Dashboard/summary

LEADS & ONBOARDING
📝 Applications                 → GET /api/Applications
👥 Carriers                     → GET /api/Carriers
📄 Documents                    → POST /api/Documents/upload

OPERATIONS
🚛 Loads                        → GET /api/Loads
🚚 Trucks                       → GET /api/Trucks
👨 Drivers                      → GET /api/Drivers
🏢 Brokers                      → GET /api/Brokers

COMMUNICATION
💬 Messages                     → GET /api/Messages/conversations
🔔 Notifications                → GET /api/Notifications

FINANCE
💰 Billing                      → GET /api/Billing/invoices
🧾 Invoices                     → (part of Billing)
💳 Payments                     → (part of Billing)

MANAGEMENT
👨‍💼 Dispatchers                  → GET /api/Dispatchers
👥 Team & Users                 → GET /api/Users
📊 Reports                      → GET /api/Reports/*

SYSTEM
⚙️ Settings
🔐 Security
📋 Activity Logs                → GET /api/Dashboard/recent-activity
```

---

## 5. API Response Format

### Standard Response

```json
{
  "success": true,
  "message": "Optional message",
  "data": { ... },
  "errors": null
}
```

### Paginated Response

```json
{
  "success": true,
  "data": {
    "items": [ ... ],
    "page": 1,
    "pageSize": 20,
    "totalCount": 150,
    "totalPages": 8,
    "hasPrevious": false,
    "hasNext": true
  }
}
```

### Error Response

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

### Common Pagination Parameters

Every list endpoint accepts:
| Parameter | Type | Default |
|-----------|------|---------|
| `page` | int | 1 |
| `pageSize` | int | 20 |

---

## 6. SignalR Hubs

### Hub Summary

| Hub | URL | Auth | Purpose |
|-----|-----|------|---------|
| **NotificationHub** | `/hubs/notifications` | JWT Required | Individual user notifications |
| **ApplicationsHub** | `/hubs/applications` | JWT Required | Live application feed for all admins |
| **DashboardHub** | `/hubs/dashboard` | JWT Required | Unified live dashboard feed |
| **ChatHub** | `/hubs/chat` | Optional | Real-time chat messages |

### Connection Code (JavaScript)

```javascript
import * as signalR from "@microsoft/signalr";

const JWT = "your-jwt-token";

// --- Notification Hub (individual notifications) ---
const notifConn = new signalR.HubConnectionBuilder()
  .withUrl("https://api.driventa.us/hubs/notifications", {
    accessTokenFactory: () => JWT
  })
  .withAutomaticReconnect()
  .build();

notifConn.on("ReceiveNotification", (n) => {
  // n = { id, userId, title, message, entityType, entityId, isRead, timestamp }
  showNotificationToast(n);
});

notifConn.on("Connected", (data) => {
  console.log("Connected as:", data.userId);
});

await notifConn.start();
await notifConn.invoke("JoinPersonalGroup");


// --- Applications Hub (live application feed) ---
const appConn = new signalR.HubConnectionBuilder()
  .withUrl("https://api.driventa.us/hubs/applications", {
    accessTokenFactory: () => JWT
  })
  .withAutomaticReconnect()
  .build();

appConn.on("ApplicationCreated", (data) => {
  // data = { applicationId, applicationNumber, companyName, fullName, status, ... }
  addApplicationToTable(data);
});

appConn.on("ApplicationStatusChanged", (data) => {
  // data = { applicationId, oldStatus, newStatus, ... }
  updateApplicationStatus(data);
});

appConn.on("ApplicationUpdated", (data) => { /* update row */ });
appConn.on("ApplicationDeleted", (data) => { /* remove row */ });

await appConn.start();
await appConn.invoke("JoinAdminGroup");


// --- Dashboard Hub (unified feed) ---
const dashConn = new signalR.HubConnectionBuilder()
  .withUrl("https://api.driventa.us/hubs/dashboard", {
    accessTokenFactory: () => JWT
  })
  .withAutomaticReconnect()
  .build();

dashConn.on("DashboardUpdate", (data) => {
  // data.entityType = "Application" | "Load" | "Carrier" | "Driver" | "Truck" | "Document" | "Message"
  // data.action = "Created" | "Updated" | "Deleted" | "StatusChanged" | "Assigned" | etc.
  // data.entity = { ... entity-specific fields }
  handleDashboardEvent(data);
});

await dashConn.start();
await dashConn.invoke("JoinAdminGroup");


// --- Chat Hub (real-time messaging) ---
const chatConn = new signalR.HubConnectionBuilder()
  .withUrl("https://api.driventa.us/hubs/chat", {
    accessTokenFactory: () => JWT
  })
  .withAutomaticReconnect()
  .build();

chatConn.on("ReceiveMessage", (msg) => {
  // msg = { messageId, message, senderUserId, senderType, timestamp }
  appendChatMessage(msg);
});

chatConn.on("MessagesRead", (conversationId) => {
  markMessagesRead(conversationId);
});

await chatConn.start();
await chatConn.invoke("JoinConversation", conversationId);
```

### Connection Code (Flutter/Dart)

```dart
import 'package:signalr_core/signalr_core.dart';

final connection = HubConnectionBuilder()
    .withUrl(
      'https://api.driventa.us/hubs/notifications',
      options: HttpConnectionOptions(
        accessTokenFactory: () async => jwtToken,
      ),
    )
    .withAutomaticReconnect()
    .build();

connection.on('ReceiveNotification', (List<dynamic>? args) {
  final notification = args![0] as Map<String, dynamic>;
  // notification['title'], notification['message'], notification['timestamp']
});

await connection.start();
await connection.invoke('JoinPersonalGroup');
```

---

## 7. REST API — All Endpoints

### Auth

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/api/Auth/login` | No | Login |
| POST | `/api/Auth/register` | No | Register new user |
| POST | `/api/Auth/refresh` | No | Refresh token |
| POST | `/api/Auth/logout` | Yes | Logout |
| GET | `/api/Auth/me` | Yes | Current user profile |
| POST | `/api/Auth/forgot-password` | No | Request password reset |
| POST | `/api/Auth/reset-password` | No | Reset password |

### Applications

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/Applications` | `applications.view` | List (paginated, filterable) |
| GET | `/api/Applications/{id}` | `applications.view` | Get by ID |
| PATCH | `/api/Applications/{id}` | `applications.edit` | Update |
| DELETE | `/api/Applications/{id}` | `applications.edit` | Soft delete |
| POST | `/api/Applications/{id}/assign` | `applications.assign` | Assign to user |
| POST | `/api/Applications/{id}/contact` | `applications.edit` | Mark contacted |
| POST | `/api/Applications/{id}/approve` | `applications.edit` | Approve |
| POST | `/api/Applications/{id}/reject` | `applications.edit` | Reject |
| POST | `/api/Applications/{id}/convert-to-carrier` | `applications.convert` | Convert to carrier |
| GET | `/api/Applications/{id}/notes` | `applications.view` | Get notes |
| POST | `/api/Applications/{id}/notes` | `applications.view` | Add note |

**Query params (GET list):** `page`, `pageSize`, `search` (name/email/company), `status` (0-6)

**Request — Create/Update:**
```json
{
  "fullName": "John Smith",
  "email": "john@company.com",
  "phone": "+1 (555) 000-0000",
  "companyName": "Smith Trucking LLC",
  "equipmentType": 0,
  "truckCount": 3,
  "mcNumber": "MC-123456",
  "dotNumber": "DOT-789012",
  "preferredLanes": "Texas → California, Florida → New York",
  "additionalDetails": "Experienced carrier"
}
```

**Request — Assign:**
```json
{
  "userId": "guid-of-user"
}
```

**Request — Convert to Carrier:**
```json
{
  "assignedDispatcherId": "guid-or-null",
  "notes": "Optional notes"
}
```

### Carriers

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/Carriers` | `carriers.view` | List (paginated, filterable) |
| GET | `/api/Carriers/{id}` | `carriers.view` | Get by ID |
| POST | `/api/Carriers` | `carriers.create` | Create |
| PATCH | `/api/Carriers/{id}` | `carriers.edit` | Update |
| DELETE | `/api/Carriers/{id}` | `carriers.edit` | Soft delete |
| POST | `/api/Carriers/{id}/assign-dispatcher` | `carriers.edit` | Assign dispatcher |
| GET | `/api/Carriers/{id}/loads` | `loads.view` | Get carrier's loads |
| GET | `/api/Carriers/{id}/trucks` | `carriers.view` | Get carrier's trucks |
| GET | `/api/Carriers/{id}/drivers` | `carriers.view` | Get carrier's drivers |
| GET | `/api/Carriers/{id}/documents` | `carriers.view` | Get carrier's documents |
| GET | `/api/Carriers/{id}/notes` | `carriers.view` | Get notes |
| POST | `/api/Carriers/{id}/notes` | `carriers.view` | Add note |

**Query params (GET list):** `page`, `pageSize`, `search` (company/contact/email), `status` (0-5)

**Request — Create:**
```json
{
  "companyName": "ABC Transport",
  "contactName": "John Smith",
  "email": "john@abc.com",
  "phone": "+1 (555) 000-0000",
  "mcNumber": "MC-123456",
  "dotNumber": "DOT-789012",
  "addressLine1": "123 Main St",
  "city": "Dallas",
  "state": "TX",
  "zipCode": "75201",
  "preferredLanes": "Texas → California",
  "notes": "Reliable carrier",
  "applicationId": "guid-or-null"
}
```

**Request — Assign Dispatcher:**
```json
{
  "dispatcherId": "guid-of-dispatcher"
}
```

### Loads

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/Loads` | `loads.view` | List (paginated, filterable) |
| GET | `/api/Loads/{id}` | `loads.view` | Get by ID |
| POST | `/api/Loads` | `loads.create` | Create |
| PATCH | `/api/Loads/{id}` | `loads.edit` | Update |
| DELETE | `/api/Loads/{id}` | `loads.edit` | Soft delete |
| POST | `/api/Loads/{id}/status` | `loads.updateStatus` | Update status |
| GET | `/api/Loads/{id}/notes` | `loads.view` | Get notes |
| POST | `/api/Loads/{id}/notes` | `loads.view` | Add note |

**Query params (GET list):** `page`, `pageSize`, `search` (load number/cities), `carrierId`, `status` (0-9)

**Request — Create:**
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
  "dispatchFeeType": "Percentage",
  "dispatchFeeValue": 10
}
```

**Request — Status Update:**
```json
{
  "status": 4,
  "notes": "Driver picked up load"
}
```

**Load Status Flow:**
```
Available(0) → Negotiating(1) → Booked(2) → Dispatched(3) → PickedUp(4) → InTransit(5) → Delivered(6) → Completed(7)
                                                                    ↘ Cancelled(8) / Issue(9)
```

### Trucks

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/Trucks` | `carriers.view` | List (paginated, filterable) |
| GET | `/api/Trucks/{id}` | `carriers.view` | Get by ID |
| POST | `/api/Trucks` | `carriers.create` | Create |
| PATCH | `/api/Trucks/{id}` | `carriers.edit` | Update |
| DELETE | `/api/Trucks/{id}` | `carriers.edit` | Soft delete |

**Query params:** `page`, `pageSize`, `search` (truck number/make/model), `carrierId`, `status`

**Request — Create:**
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

### Drivers

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/Drivers` | `carriers.view` | List (paginated, filterable) |
| GET | `/api/Drivers/{id}` | `carriers.view` | Get by ID |
| POST | `/api/Drivers` | `carriers.create` | Create |
| PATCH | `/api/Drivers/{id}` | `carriers.edit` | Update |
| DELETE | `/api/Drivers/{id}` | `carriers.edit` | Soft delete |

**Query params:** `page`, `pageSize`, `search` (name/email), `carrierId`, `status`

**Request — Create:**
```json
{
  "carrierId": "carrier-guid",
  "truckId": "truck-guid-or-null",
  "firstName": "Mike",
  "lastName": "Johnson",
  "email": "mike@example.com",
  "phone": "+1 (555) 000-0000",
  "licenseNumber": "DL123456",
  "licenseState": "TX"
}
```

### Brokers

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/Brokers` | `carriers.view` | List (paginated) |
| GET | `/api/Brokers/{id}` | `carriers.view` | Get by ID |
| POST | `/api/Brokers` | `carriers.create` | Create |
| PATCH | `/api/Brokers/{id}` | `carriers.edit` | Update |
| DELETE | `/api/Brokers/{id}` | `carriers.edit` | Soft delete |

**Request — Create:**
```json
{
  "companyName": "XYZ Freight Brokers",
  "contactName": "Jane Doe",
  "email": "jane@xyz.com",
  "phone": "+1 (555) 000-0000",
  "mcNumber": "MC-111222",
  "address": "456 Broker Ave",
  "internalRating": 4,
  "paymentNotes": "Net 30",
  "generalNotes": "Reliable broker"
}
```

### Documents

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/api/Documents/upload` | `carriers.view` | Upload file (multipart) |
| GET | `/api/Documents/{id}` | `carriers.view` | Get by ID |
| DELETE | `/api/Documents/{id}` | `billing.manage` | Delete |

**Upload — multipart/form-data:**

| Field | Type | Required |
|-------|------|----------|
| `file` | File | Yes |
| `documentType` | int | Yes (query param) |
| `carrierId` | Guid? | No (query param) |
| `loadId` | Guid? | No (query param) |
| `driverId` | Guid? | No (query param) |

### Messages

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/Messages/conversations` | Yes | List conversations |
| GET | `/api/Messages/conversations/{id}` | Yes | Get conversation + messages |
| POST | `/api/Messages` | Yes | Send message |
| PATCH | `/api/Messages/conversations/{id}/read` | Yes | Mark as read |

**Request — Send Message:**
```json
{
  "conversationId": "guid",
  "content": "Hello, how can I help you?"
}
```

### Notifications

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/Notifications` | Yes | List (paginated, filterable) |
| GET | `/api/Notifications/{id}` | Yes | Get by ID |
| GET | `/api/Notifications/unread-count` | Yes | Unread count |
| PATCH | `/api/Notifications/{id}/read` | Yes | Mark read |
| POST | `/api/Notifications/read-all` | Yes | Mark all read |

**Query params:** `page`, `pageSize`, `isRead` (bool)

### Billing

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/Billing/invoices` | `billing.view` | List invoices |
| POST | `/api/Billing/invoices` | `billing.create` | Create invoice |
| GET | `/api/Billing/invoices/{id}` | `billing.view` | Get invoice |
| PATCH | `/api/Billing/invoices/{id}/status` | `billing.manage` | Update status |
| POST | `/api/Billing/invoices/{id}/payments` | `billing.create` | Record payment |

### Dashboard

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/Dashboard/summary` | Yes | Summary stats |
| GET | `/api/Dashboard/load-status-summary` | Yes | Load counts by status |
| GET | `/api/Dashboard/recent-activity` | Yes | Recent activity logs |
| GET | `/api/Dashboard/revenue-summary` | `billing.view` | Revenue summary |

### Reports

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/Reports/loads` | `reports.view` | Load performance |
| GET | `/api/Reports/revenue` | `reports.view` | Revenue with monthly breakdown |
| GET | `/api/Reports/carriers` | `reports.view` | Carrier performance |
| GET | `/api/Reports/dispatchers` | `reports.view` | Dispatcher performance |

### Dispatchers

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/Dispatchers` | Yes | List all dispatchers |

### Users

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/Users` | `users.view` | List all users |
| POST | `/api/Users` | `users.create` | Create user |
| PATCH | `/api/Users/{id}` | `users.edit` | Update user |
| DELETE | `/api/Users/{id}` | `users.delete` | Deactivate user |

### Public (No Auth)

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/api/public/applications` | No (rate limited) | Submit carrier application |
| POST | `/api/public/chat/session` | No | Create chat session |
| POST | `/api/public/contact` | No | Submit contact form |

**Rate Limit:** 10 requests/minute

---

## 8. Data Schemas

### BaseEntity (All entities inherit this)

| Field | Type | Description |
|-------|------|-------------|
| `id` | Guid | Primary key |
| `createdAt` | DateTimeOffset | Created timestamp |
| `updatedAt` | DateTimeOffset | Last updated timestamp |
| `createdByUserId` | Guid? | Creator |
| `updatedByUserId` | Guid? | Last editor |
| `isDeleted` | bool | Soft delete |

### Application

| Field | Type | Notes |
|-------|------|-------|
| `id` | Guid | PK |
| `applicationNumber` | string | Auto: `APP-YYMMDD-XXXX` |
| `fullName` | string | Required, max 200 |
| `email` | string | Required, max 200 |
| `phone` | string | Required, max 50 |
| `companyName` | string | Required, max 200 |
| `equipmentType` | int (enum) | EquipmentType |
| `truckCount` | int | Must be > 0 |
| `mcNumber` | string? | Max 50 |
| `dotNumber` | string? | Max 50 |
| `preferredLanes` | string? | Max 500. Format: `"Texas → California, Florida → New York"` |
| `additionalDetails` | string? | Max 2000 |
| `status` | int (enum) | ApplicationStatus (0-6) |
| `assignedToUserId` | Guid? | Assigned dispatcher |
| `submittedAt` | DateTimeOffset | Submission date |
| `contactedAt` | DateTimeOffset? | Contact date |
| `approvedAt` | DateTimeOffset? | Approval date |
| `rejectedAt` | DateTimeOffset? | Rejection date |
| `convertedCarrierId` | Guid? | Linked carrier after conversion |

### Carrier

| Field | Type | Notes |
|-------|------|-------|
| `id` | Guid | PK |
| `companyName` | string | Max 200 |
| `contactName` | string | Max 200 |
| `email` | string | Max 200 |
| `phone` | string | Max 50 |
| `mcNumber` | string? | Max 50 |
| `dotNumber` | string? | Max 50 |
| `addressLine1` | string? | Max 200 |
| `addressLine2` | string? | Max 200 |
| `city` | string? | Max 100 |
| `state` | string? | Max 50 |
| `zipCode` | string? | Max 20 |
| `status` | int (enum) | CarrierStatus (0-5) |
| `assignedDispatcherId` | Guid? | Assigned dispatcher |
| `preferredLanes` | string? | Max 500 |
| `notes` | string? | Max 2000 |
| `applicationId` | Guid? | Linked application |

### Load

| Field | Type | Notes |
|-------|------|-------|
| `id` | Guid | PK |
| `loadNumber` | string | Auto: `LD-YYMMDD-XXXX` |
| `carrierId` | Guid | Required |
| `truckId` | Guid? | Assigned truck |
| `driverId` | Guid? | Assigned driver |
| `brokerId` | Guid? | Broker |
| `dispatcherId` | Guid? | Dispatcher |
| `equipmentType` | int (enum) | EquipmentType |
| `pickupCity` | string | Required |
| `pickupState` | string | Required |
| `pickupDateTime` | DateTimeOffset | Required |
| `deliveryCity` | string | Required |
| `deliveryState` | string | Required |
| `deliveryDateTime` | DateTimeOffset | Required |
| `rate` | decimal(12,2) | Load rate ($) |
| `miles` | int? | Distance |
| `ratePerMile` | decimal(8,2)? | Auto-calculated |
| `dispatchFeeType` | string? | `"Percentage"` or `"Flat"` |
| `dispatchFeeValue` | decimal? | Fee rate/amount |
| `dispatchFeeAmount` | decimal(12,2)? | Auto-calculated |
| `carrierNetAmount` | decimal(12,2)? | Auto-calculated |
| `status` | int (enum) | LoadStatus (0-9) |
| `bookedAt` | DateTimeOffset? | Auto on Booked status |
| `pickedUpAt` | DateTimeOffset? | Auto on PickedUp status |
| `deliveredAt` | DateTimeOffset? | Auto on Delivered status |
| `completedAt` | DateTimeOffset? | Auto on Completed status |

### Truck

| Field | Type | Notes |
|-------|------|-------|
| `id` | Guid | PK |
| `carrierId` | Guid | Parent carrier |
| `truckNumber` | string | Required |
| `equipmentType` | int (enum) | EquipmentType |
| `make` | string? | e.g. "Freightliner" |
| `model` | string? | e.g. "Cascadia" |
| `year` | int? | e.g. 2024 |
| `licensePlate` | string? | Plate number |
| `licenseState` | string? | Plate state |
| `status` | int (enum) | TruckStatus (0-3) |

### Driver

| Field | Type | Notes |
|-------|------|-------|
| `id` | Guid | PK |
| `carrierId` | Guid | Parent carrier |
| `truckId` | Guid? | Assigned truck |
| `firstName` | string | Required |
| `lastName` | string | Required |
| `email` | string? | |
| `phone` | string? | |
| `licenseNumber` | string? | CDL number |
| `licenseState` | string? | License state |
| `status` | int (enum) | DriverStatus (0-4) |

### Broker

| Field | Type | Notes |
|-------|------|-------|
| `id` | Guid | PK |
| `companyName` | string | Required |
| `contactName` | string | Required |
| `email` | string? | |
| `phone` | string? | |
| `mcNumber` | string? | |
| `address` | string? | |
| `internalRating` | int? | 1-5 |
| `paymentNotes` | string? | |
| `generalNotes` | string? | |

### Document

| Field | Type | Notes |
|-------|------|-------|
| `id` | Guid | PK |
| `fileName` | string | Original filename |
| `storedFileName` | string | GUID-based stored name |
| `fileUrl` | string | `/uploads/{storedFileName}` |
| `contentType` | string | MIME type |
| `fileSize` | long | Bytes |
| `documentType` | int (enum) | DocumentType |
| `carrierId` | Guid? | Linked carrier |
| `loadId` | Guid? | Linked load |
| `driverId` | Guid? | Linked driver |
| `uploadedByUserId` | Guid? | Who uploaded |
| `expiresAt` | DateTimeOffset? | Expiry date |

### Notification

| Field | Type | Notes |
|-------|------|-------|
| `id` | Guid | PK |
| `userId` | Guid | Recipient user |
| `type` | int (enum) | NotificationType |
| `title` | string | Short title |
| `message` | string | Description |
| `entityType` | string? | "Application", "Load", etc. |
| `entityId` | Guid? | Related entity ID |
| `isRead` | bool | Read status |
| `createdAt` | DateTimeOffset | Creation time |

### Conversation

| Field | Type | Notes |
|-------|------|-------|
| `id` | Guid | PK |
| `visitorId` | string | Browser fingerprint/session |
| `visitorName` | string | Visitor name |
| `visitorEmail` | string? | Visitor email |
| `visitorPhone` | string? | Visitor phone |
| `assignedToUserId` | Guid? | Assigned admin |
| `isActive` | bool | Active status |
| `startedAt` | DateTimeOffset | Start time |
| `lastMessageAt` | DateTimeOffset? | Last message time |

### Message

| Field | Type | Notes |
|-------|------|-------|
| `id` | Guid | PK |
| `conversationId` | Guid | Parent conversation |
| `senderType` | int (enum) | SenderType (0=Visitor, 1=Admin) |
| `senderUserId` | Guid? | Sender user ID |
| `content` | string | Message text, max 5000 |
| `isRead` | bool | Read status |

### Invoice

| Field | Type | Notes |
|-------|------|-------|
| `id` | Guid | PK |
| `invoiceNumber` | string | Auto-generated |
| `carrierId` | Guid | Billed carrier |
| `periodStart` | DateTimeOffset | Billing period start |
| `periodEnd` | DateTimeOffset | Billing period end |
| `subtotal` | decimal(12,2) | Items total |
| `taxAmount` | decimal(12,2) | Tax |
| `totalAmount` | decimal(12,2) | Grand total |
| `status` | int (enum) | InvoiceStatus |
| `dueDate` | DateTimeOffset? | Due date |
| `paidAt` | DateTimeOffset? | Payment date |

---

## 9. Enums

### ApplicationStatus
| Value | Name | Color Suggestion |
|-------|------|-----------------|
| 0 | `New` | Blue |
| 1 | `Reviewing` | Yellow |
| 2 | `Contacted` | Orange |
| 3 | `Qualified` | Teal |
| 4 | `Approved` | Green |
| 5 | `Rejected` | Red |
| 6 | `Onboarded` | Purple |

### CarrierStatus
| Value | Name | Color Suggestion |
|-------|------|-----------------|
| 0 | `Lead` | Gray |
| 1 | `Onboarding` | Yellow |
| 2 | `Active` | Green |
| 3 | `Paused` | Orange |
| 4 | `Inactive` | Red |
| 5 | `Suspended` | Dark Red |

### LoadStatus
| Value | Name | Color Suggestion |
|-------|------|-----------------|
| 0 | `Available` | Blue |
| 1 | `Negotiating` | Yellow |
| 2 | `Booked` | Orange |
| 3 | `Dispatched` | Teal |
| 4 | `PickedUp` | Green |
| 5 | `InTransit` | Indigo |
| 6 | `Delivered` | Light Green |
| 7 | `Completed` | Dark Green |
| 8 | `Cancelled` | Red |
| 9 | `Issue` | Dark Red |

### TruckStatus
| Value | Name |
|-------|------|
| 0 | `Available` |
| 1 | `OnLoad` |
| 2 | `Maintenance` |
| 3 | `Inactive` |

### DriverStatus
| Value | Name |
|-------|------|
| 0 | `Available` |
| 1 | `Assigned` |
| 2 | `Driving` |
| 3 | `OffDuty` |
| 4 | `Inactive` |

### EquipmentType
| Value | Name |
|-------|------|
| 0 | `DryVan` |
| 1 | `Reefer` |
| 2 | `Flatbed` |
| 3 | `StepDeck` |
| 4 | `BoxTruck` |
| 5 | `Hotshot` |
| 6 | `PowerOnly` |

### DocumentType
| Value | Name |
|-------|------|
| 0 | `Insurance` |
| 1 | `W9` |
| 2 | `MC_Authority` |
| 3 | `RateConfirmation` |
| 4 | `BOL` |
| 5 | `POD` |
| 6 | `CarrierAgreement` |
| 7 | `DriverLicense` |
| 8 | `Other` |

### NotificationType
| Value | Name | Trigger |
|-------|------|---------|
| 0 | `NewApplication` | Public form submitted |
| 1 | `NewMessage` | Chat message received |
| 2 | `LoadStatusChanged` | Load status updated |
| 3 | `DocumentExpiring` | Reserved |
| 4 | `DocumentUploaded` | File uploaded |
| 5 | `CarrierAssigned` | Carrier converted |
| 6 | `CarrierCreated` | New carrier added |
| 7 | `DriverCreated` | New driver added |
| 8 | `TruckCreated` | New truck added |
| 9 | `LoadCreated` | New load created |
| 10 | `DispatcherAssigned` | Dispatcher assigned to carrier |
| 11 | `ApplicationAssigned` | Application assigned to user |
| 12 | `ApplicationStatusChanged` | Application status changed |

### SenderType (Chat)
| Value | Name |
|-------|------|
| 0 | `Visitor` |
| 1 | `Admin` |

### InvoiceStatus
| Value | Name |
|-------|------|
| 0 | `Draft` |
| 1 | `Sent` |
| 2 | `PartiallyPaid` |
| 3 | `Paid` |
| 4 | `Overdue` |
| 5 | `Cancelled` |

---

## 10. Notification System

### How Notifications Work

```
Action happens in backend
        │
        ├──► 1. Saved to database (persistent)
        │
        ├──► 2. Sent via NotificationHub → individual user (personal notification)
        │
        ├──► 3. Sent via ApplicationsHub → all admins (live application feed)
        │
        └──► 4. Sent via DashboardHub → all admins (unified dashboard feed)
```

### Who Gets Notified

| Event | Recipients | Delivery |
|-------|-----------|----------|
| New application | All SuperAdmin, Admin, DispatchManager, Dispatcher | NotificationHub + ApplicationsHub + DashboardHub |
| Application status change | Assigned user | NotificationHub + ApplicationsHub + DashboardHub |
| Application assigned | The assigned user | NotificationHub + ApplicationsHub + DashboardHub |
| Application converted to carrier | Performing user | NotificationHub + ApplicationsHub + DashboardHub |
| New carrier | All SuperAdmin, Admin, DispatchManager | NotificationHub + DashboardHub |
| Dispatcher assigned | The dispatcher | NotificationHub + DashboardHub |
| New load | Carrier's dispatcher | NotificationHub + DashboardHub |
| Load status change | Carrier's dispatcher | NotificationHub + DashboardHub |
| New driver | Carrier's dispatcher | NotificationHub + DashboardHub |
| New truck | Carrier's dispatcher | NotificationHub + DashboardHub |
| Document uploaded | Carrier's dispatcher | NotificationHub + DashboardHub |
| Message from visitor | Conversation's assigned admin | NotificationHub |

### Notification Payload (ReceiveNotification)

```json
{
  "id": "guid",
  "userId": "guid",
  "title": "New Application",
  "message": "Smith Trucking LLC (John Smith) submitted a new application.",
  "entityType": "Application",
  "entityId": "guid",
  "isRead": false,
  "timestamp": "2026-08-31T12:00:00Z"
}
```

### DashboardUpdate Payload (All Events)

```json
{
  "entityType": "Application | Load | Carrier | Driver | Truck | Document | Message",
  "action": "Created | Updated | Deleted | StatusChanged | Assigned | ConvertedToCarrier | DispatcherAssigned | Uploaded | NewMessage",
  "entity": { ... entity-specific fields ... }
}
```

**Entity-specific payloads:**

| entityType | action | entity fields |
|------------|--------|---------------|
| Application | Created | applicationId, applicationNumber, companyName, fullName, email, phone, equipmentType, truckCount, status, submittedAt |
| Application | Updated | applicationId, applicationNumber, companyName, fullName, status |
| Application | StatusChanged | applicationId, applicationNumber, companyName, oldStatus, newStatus, timestamp |
| Application | Assigned | applicationId, applicationNumber, companyName, assignedToUserId, timestamp |
| Application | ConvertedToCarrier | applicationId, applicationNumber, carrierId, carrierName |
| Application | Deleted | applicationId, applicationNumber |
| Load | Created | loadId, loadNumber, carrierName, pickupCity, pickupState, deliveryCity, deliveryState, rate, status |
| Load | Updated | loadId, loadNumber, status |
| Load | StatusChanged | loadId, loadNumber, carrierName, oldStatus, newStatus, timestamp |
| Load | Deleted | loadId, loadNumber |
| Carrier | Created | carrierId, companyName, contactName, status |
| Carrier | Updated | carrierId, companyName, status |
| Carrier | DispatcherAssigned | carrierId, companyName, dispatcherId |
| Carrier | Deleted | carrierId, companyName |
| Driver | Created | driverId, firstName, lastName, carrierName, carrierId |
| Driver | Updated | driverId, firstName, lastName, status |
| Driver | Deleted | driverId, firstName, lastName |
| Truck | Created | truckId, truckNumber, make, model, year, carrierName, carrierId |
| Truck | Updated | truckId, truckNumber, status |
| Truck | Deleted | truckId, truckNumber |
| Document | Uploaded | documentId, fileName, documentType, carrierId, loadId, driverId |
| Document | Deleted | documentId, fileName |
| Message | NewMessage | conversationId, visitorName, message, senderType, timestamp |

---

## 11. Chat System

### Public Website Chat Flow

1. User opens chat widget
2. Call `POST /api/public/chat/session` → get `conversationId` + `visitorId`
3. Connect to `/hubs/chat` (no JWT needed)
4. Call `JoinConversation(conversationId)`
5. Send messages via `SendMessage(conversationId, message)`
6. Listen for `ReceiveMessage`

### Dashboard Chat Flow

1. Admin connects to `/hubs/chat` with JWT
2. Opens conversation → calls `JoinConversation(conversationId)`
3. Sends messages via REST: `POST /api/Messages`
4. Listens for `ReceiveMessage` events
5. Calls `MarkAsRead` when viewing conversation

### ReceiveMessage Payload

```json
{
  "messageId": "guid",
  "message": "Hello, I need help with my shipment",
  "senderUserId": "guid-or-null",
  "senderType": 0,
  "timestamp": "2026-08-31T12:00:00Z"
}
```

### Conversation List Response

```json
{
  "id": "guid",
  "visitorId": "visitor-abc-123",
  "visitorName": "John Visitor",
  "visitorEmail": "john@example.com",
  "visitorPhone": "+1 (555) 000-0000",
  "assignedToUserId": "guid",
  "isActive": true,
  "startedAt": "2026-08-31T10:00:00Z",
  "lastMessageAt": "2026-08-31T12:00:00Z",
  "unreadCount": 3,
  "lastMessage": "I need help with my load",
  "lastMessageSenderType": 0
}
```

---

## 12. File Upload

### Upload Document

```
POST /api/Documents/upload
Content-Type: multipart/form-data
```

**Fields:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `file` | File | Yes | The file to upload |
| `documentType` | int | Yes | DocumentType enum value (query param) |
| `carrierId` | Guid? | No | Link to carrier (query param) |
| `loadId` | Guid? | No | Link to load (query param) |
| `driverId` | Guid? | No | Link to driver (query param) |

**Max file size:** 50 MB

**Response:**
```json
{
  "success": true,
  "data": {
    "id": "guid",
    "fileName": "insurance.pdf",
    "fileUrl": "/uploads/abc123-def456.pdf",
    "contentType": "application/pdf",
    "fileSize": 1048576,
    "documentType": 0,
    "carrierId": "guid",
    "loadId": null,
    "driverId": null,
    "uploadedByUserId": "guid",
    "createdAt": "2026-08-31T12:00:00Z",
    "expiresAt": null
  }
}
```

**File URL:** To display the file, use: `{baseUrl}/uploads/{storedFileName}`

---

## 13. Error Handling

### HTTP Status Codes

| Code | Meaning |
|------|---------|
| 200 | Success |
| 400 | Bad Request / Validation Error |
| 401 | Unauthorized (no token or invalid) |
| 403 | Forbidden (no permission) |
| 404 | Not Found |
| 429 | Rate Limited |
| 500 | Internal Server Error |

### Error Response

```json
{
  "success": false,
  "message": "Application not found.",
  "data": null,
  "errors": null
}
```

### Validation Error Response

```json
{
  "success": false,
  "message": "Validation failed.",
  "data": null,
  "errors": [
    "Full Name is required.",
    "Email is required.",
    "Truck Count must be greater than 0."
  ]
}
```

---

## 14. Implementation Checklist

### Phase 1: Authentication
- [ ] Login screen with email/password
- [ ] Store JWT tokens securely
- [ ] Auto-refresh before expiry
- [ ] Add Bearer token to all API calls
- [ ] Logout functionality

### Phase 2: Dashboard Layout
- [ ] Sidebar navigation (see Section 4)
- [ ] Header with user info + notification bell
- [ ] Main content area with routing

### Phase 3: Applications Module
- [ ] Applications list (table with filters)
- [ ] Application detail view
- [ ] Status change actions (Contact, Approve, Reject)
- [ ] Assign application to user
- [ ] Convert to carrier
- [ ] Notes section
- [ ] Real-time updates (ApplicationsHub)

### Phase 4: Carriers Module
- [ ] Carriers list (table with filters)
- [ ] Carrier detail view
- [ ] Create/Edit carrier form
- [ ] Assign dispatcher
- [ ] View carrier's loads, trucks, drivers, documents
- [ ] Notes section

### Phase 5: Loads Module
- [ ] Loads list (table with filters)
- [ ] Load detail view
- [ ] Create/Edit load form
- [ ] Status update workflow
- [ ] Notes section

### Phase 6: Trucks & Drivers
- [ ] Trucks list + CRUD
- [ ] Drivers list + CRUD

### Phase 7: Communication
- [ ] Conversations list with unread badges
- [ ] Chat interface (send/receive messages)
- [ ] Mark as read
- [ ] Real-time updates (ChatHub)

### Phase 8: Notifications
- [ ] Notification bell with unread count
- [ ] Notification dropdown/panel
- [ ] Mark as read / Mark all read
- [ ] Notification history page
- [ ] Real-time notifications (NotificationHub)

### Phase 9: Dashboard Home
- [ ] Summary stats cards
- [ ] Load status chart
- [ ] Recent activity feed
- [ ] Revenue summary (if billing enabled)

### Phase 10: Finance
- [ ] Invoices list
- [ ] Create invoice
- [ ] Record payment
- [ ] Invoice status management

### Phase 11: Reports
- [ ] Load performance report
- [ ] Revenue report
- [ ] Carrier performance report
- [ ] Dispatcher performance report

### Phase 12: Settings & Users
- [ ] User management (CRUD)
- [ ] Role management
- [ ] System settings

---

## 15. Code Examples

### Fetch with Auth (JavaScript)

```javascript
const API_BASE = "https://api.driventa.us";

async function apiGet(path) {
  const res = await fetch(`${API_BASE}${path}`, {
    headers: { "Authorization": `Bearer ${token}` }
  });
  return res.json();
}

async function apiPost(path, body) {
  const res = await fetch(`${API_BASE}${path}`, {
    method: "POST",
    headers: {
      "Authorization": `Bearer ${token}`,
      "Content-Type": "application/json"
    },
    body: JSON.stringify(body)
  });
  return res.json();
}

async function apiPatch(path, body) {
  const res = await fetch(`${API_BASE}${path}`, {
    method: "PATCH",
    headers: {
      "Authorization": `Bearer ${token}`,
      "Content-Type": "application/json"
    },
    body: JSON.stringify(body)
  });
  return res.json();
}

// Usage:
const apps = await apiGet("/api/Applications?page=1&pageSize=20&status=0");
const carrier = await apiGet("/api/Carriers/some-guid");
const result = await apiPost("/api/Applications/some-guid/approve");
```

### Upload File (JavaScript)

```javascript
async function uploadDocument(file, documentType, carrierId) {
  const formData = new FormData();
  formData.append("file", file);

  const res = await fetch(
    `${API_BASE}/api/Documents/upload?documentType=${documentType}&carrierId=${carrierId}`,
    {
      method: "POST",
      headers: { "Authorization": `Bearer ${token}` },
      body: formData
    }
  );
  return res.json();
}
```

### SignalR with Auto-Reconnect (JavaScript)

```javascript
function createHubConnection(url, token) {
  const connection = new signalR.HubConnectionBuilder()
    .withUrl(url, {
      accessTokenFactory: () => token,
      withCredentials: false
    })
    .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
    .configureLogging(signalR.LogLevel.Information)
    .build();

  connection.onclose((error) => {
    console.error("Connection closed:", error);
    // Attempt to reconnect manually if needed
  });

  connection.onreconnecting((error) => {
    console.log("Reconnecting...", error);
  });

  connection.onreconnected((connectionId) => {
    console.log("Reconnected:", connectionId);
    // Re-join groups after reconnect
  });

  return connection;
}
```

### Complete Dashboard Connection Setup (JavaScript)

```javascript
import * as signalR from "@microsoft/signalr";

class DriventaRealtime {
  constructor(apiUrl, jwtToken) {
    this.apiUrl = apiUrl;
    this.token = jwtToken;
    this.connections = {};
    this.listeners = {};
  }

  async connect() {
    // Notification hub
    this.connections.notifications = this.createHub("/hubs/notifications");
    this.connections.notifications.on("ReceiveNotification", (n) => {
      this.emit("notification", n);
    });
    await this.connections.notifications.start();
    await this.connections.notifications.invoke("JoinPersonalGroup");

    // Applications hub
    this.connections.applications = this.createHub("/hubs/applications");
    ["ApplicationCreated", "ApplicationUpdated", "ApplicationStatusChanged", "ApplicationDeleted"]
      .forEach(event => {
        this.connections.applications.on(event, (data) => {
          this.emit(event.toLowerCase(), data);
        });
      });
    await this.connections.applications.start();
    await this.connections.applications.invoke("JoinAdminGroup");

    // Dashboard hub
    this.connections.dashboard = this.createHub("/hubs/dashboard");
    this.connections.dashboard.on("DashboardUpdate", (data) => {
      this.emit("dashboardupdate", data);
    });
    await this.connections.dashboard.start();
    await this.connections.dashboard.invoke("JoinAdminGroup");

    // Chat hub
    this.connections.chat = this.createHub("/hubs/chat");
    this.connections.chat.on("ReceiveMessage", (msg) => {
      this.emit("message", msg);
    });
    this.connections.chat.on("MessagesRead", (convId) => {
      this.emit("messagesread", convId);
    });
    await this.connections.chat.start();
  }

  createHub(path) {
    return new signalR.HubConnectionBuilder()
      .withUrl(`${this.apiUrl}${path}`, {
        accessTokenFactory: () => this.token
      })
      .withAutomaticReconnect()
      .build();
  }

  on(event, callback) {
    if (!this.listeners[event]) this.listeners[event] = [];
    this.listeners[event].push(callback);
  }

  emit(event, data) {
    (this.listeners[event] || []).forEach(cb => cb(data));
  }

  async joinConversation(conversationId) {
    await this.connections.chat.invoke("JoinConversation", conversationId);
  }

  async sendMessage(conversationId, message) {
    await this.connections.chat.invoke("SendMessage", conversationId, message);
  }

  async markAsRead(conversationId) {
    await this.connections.chat.invoke("MarkAsRead", conversationId);
  }

  disconnect() {
    Object.values(this.connections).forEach(conn => conn.stop());
  }
}

// Usage:
const realtime = new DriventaRealtime("https://api.driventa.us", jwtToken);

realtime.on("notification", (n) => {
  showToast(n.title, n.message);
  updateNotificationBadge();
});

realtime.on("applicationcreated", (data) => {
  addApplicationToTable(data);
});

realtime.on("dashboardupdate", (data) => {
  updateDashboardFeed(data);
});

realtime.on("message", (msg) => {
  appendChatMessage(msg);
});

await realtime.connect();
```

---

*Generated from Driventa API v1 — Backend built with ASP.NET Core 10, PostgreSQL, SignalR, FluentValidation*
