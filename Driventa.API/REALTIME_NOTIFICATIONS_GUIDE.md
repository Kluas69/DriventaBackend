# Driventa Real-Time Notification System — Dashboard Integration Guide

## Overview

The Driventa backend provides real-time notifications via **SignalR WebSocket hubs** and a persistent **REST API** for notification history. Every significant action (new application, status change, message, load update, carrier/driver/truck changes, document uploads) triggers both:

1. **Real-time push** via SignalR to connected dashboard clients
2. **Persistent notification** saved to the database for later retrieval

---

## Table of Contents

1. [Architecture](#architecture)
2. [SignalR Hubs](#signalr-hubs)
3. [Connection Setup](#connection-setup)
4. [Hub Events — Full Reference](#hub-events)
5. [REST API — Notifications](#rest-api)
6. [REST API — Conversations & Messages](#conversations-api)
7. [Data Schemas](#data-schemas)
8. [Notification Types Enum](#notification-types)
9. [Implementation Checklist](#checklist)

---

## Architecture

```
Frontend Dashboard
    │
    ├── SignalR Hub: /hubs/notifications  (individual user notifications)
    ├── SignalR Hub: /hubs/applications   (live application feed for all admins)
    ├── SignalR Hub: /hubs/dashboard      (unified live dashboard feed)
    ├── SignalR Hub: /hubs/chat           (real-time chat messages)
    │
    └── REST API: /api/Notifications      (persistent notification history)
```

**How it works:**
- When an action happens (new application, status change, etc.), the backend broadcasts to the relevant SignalR group AND saves a notification to the database.
- The `NotificationHub` sends notifications to individual users via group `user_{userId}`.
- The `ApplicationsHub` broadcasts to the `admins` group (all connected admins see application changes).
- The `DashboardHub` broadcasts to the `dashboard-admins` group (unified feed).
- The `ChatHub` broadcasts to conversation groups (real-time chat).

---

## SignalR Hubs

### 1. Notification Hub — `/hubs/notifications`

**Auth:** JWT required (Bearer token or query string `access_token`)

**Client Methods:**
| Method | Parameters | Description |
|--------|-----------|-------------|
| `JoinPersonalGroup` | none | Joins the user's personal notification group. Call this on connect. |

**Server Events Received:**
| Event | Payload | Description |
|-------|---------|-------------|
| `Connected` | `{ userId }` | Confirms connection, returns the user's ID |
| `ReceiveNotification` | See schema below | A new notification for this user |

---

### 2. Applications Hub — `/hubs/applications`

**Auth:** JWT required

**Client Methods:**
| Method | Parameters | Description |
|--------|-----------|-------------|
| `JoinAdminGroup` | none | Joins the `admins` group to receive all application events |
| `LeaveAdminGroup` | none | Leaves the `admins` group |

**Server Events Received:**
| Event | Payload | Description |
|-------|---------|-------------|
| `ApplicationCreated` | See below | A new application was submitted |
| `ApplicationUpdated` | See below | An application was edited |
| `ApplicationStatusChanged` | See below | An application's status changed |
| `ApplicationDeleted` | See below | An application was deleted |

---

### 3. Dashboard Hub — `/hubs/dashboard`

**Auth:** JWT required

**Client Methods:**
| Method | Parameters | Description |
|--------|-----------|-------------|
| `JoinAdminGroup` | none | Joins the `dashboard-admins` group for the unified feed |
| `LeaveAdminGroup` | none | Leaves the group |

**Server Events Received:**
| Event | Payload | Description |
|-------|---------|-------------|
| `DashboardUpdate` | `{ entityType, action, entity }` | Any entity changed — use this for the main dashboard feed |

---

### 4. Chat Hub — `/hubs/chat`

**Auth:** Optional (visitors = no auth, admins = JWT)

**Client Methods:**
| Method | Parameters | Description |
|--------|-----------|-------------|
| `JoinConversation` | `conversationId: string` | Join a conversation room |
| `SendMessage` | `conversationId: string, message: string` | Send a message |
| `MarkAsRead` | `conversationId: string` | Mark all messages in conversation as read |

**Server Events Received:**
| Event | Payload | Description |
|-------|---------|-------------|
| `ReceiveMessage` | `{ messageId, message, senderUserId, senderType, timestamp }` | New message in the conversation |
| `MessagesRead` | `conversationId` | Messages were marked as read |

---

## Connection Setup

### JavaScript/TypeScript (SignalR client)

```typescript
import * as signalR from "@microsoft/signalr";

const JWT_TOKEN = "your-jwt-token-here";

// 1. Notification Hub — individual notifications
const notificationConnection = new signalR.HubConnectionBuilder()
  .withUrl("https://api.driventa.us/hubs/notifications", {
    accessTokenFactory: () => JWT_TOKEN
  })
  .withAutomaticReconnect()
  .build();

notificationConnection.on("ReceiveNotification", (notification) => {
  // notification = { id, userId, title, message, entityType, entityId, isRead, timestamp }
  console.log("New notification:", notification);
  // Update your notification bell/panel
});

notificationConnection.on("Connected", (data) => {
  console.log("Connected as user:", data.userId);
});

await notificationConnection.start();
await notificationConnection.invoke("JoinPersonalGroup");


// 2. Applications Hub — live application feed
const applicationsConnection = new signalR.HubConnectionBuilder()
  .withUrl("https://api.driventa.us/hubs/applications", {
    accessTokenFactory: () => JWT_TOKEN
  })
  .withAutomaticReconnect()
  .build();

applicationsConnection.on("ApplicationCreated", (data) => {
  // data = { applicationId, applicationNumber, companyName, fullName, email, phone,
  //          equipmentType, truckCount, status, submittedAt }
  console.log("New application:", data);
  // Add to applications list in real-time
});

applicationsConnection.on("ApplicationUpdated", (data) => {
  // data = { applicationId, applicationNumber, companyName, fullName, status }
  console.log("Application updated:", data);
});

applicationsConnection.on("ApplicationStatusChanged", (data) => {
  // data = { applicationId, applicationNumber, companyName, fullName,
  //          oldStatus, newStatus, timestamp }
  console.log("Status changed:", data);
});

applicationsConnection.on("ApplicationDeleted", (data) => {
  // data = { applicationId, applicationNumber, timestamp }
  console.log("Application deleted:", data);
});

await applicationsConnection.start();
await applicationsConnection.invoke("JoinAdminGroup");


// 3. Dashboard Hub — unified feed
const dashboardConnection = new signalR.HubConnectionBuilder()
  .withUrl("https://api.driventa.us/hubs/dashboard", {
    accessTokenFactory: () => JWT_TOKEN
  })
  .withAutomaticReconnect()
  .build();

dashboardConnection.on("DashboardUpdate", (data) => {
  // data.entityType: "Application" | "Load" | "Carrier" | "Driver" | "Truck" | "Document" | "Message"
  // data.action: "Created" | "Updated" | "Deleted" | "StatusChanged" | "Assigned" | "ConvertedToCarrier" | "DispatcherAssigned" | "Uploaded" | "NewMessage"
  // data.entity: { ... entity-specific fields }
  console.log("Dashboard update:", data);
  // Update sidebar feed, counters, etc.
});

await dashboardConnection.start();
await dashboardConnection.invoke("JoinAdminGroup");


// 4. Chat Hub — real-time messaging
const chatConnection = new signalR.HubConnectionBuilder()
  .withUrl("https://api.driventa.us/hubs/chat", {
    accessTokenFactory: () => JWT_TOKEN  // omit for visitor chat
  })
  .withAutomaticReconnect()
  .build();

chatConnection.on("ReceiveMessage", (msg) => {
  // msg = { messageId, message, senderUserId, senderType, timestamp }
  console.log("New message:", msg);
});

await chatConnection.start();
await chatConnection.invoke("JoinConversation", conversationId);
```

### Flutter/Dart

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
  print('Notification: ${notification['title']}');
});

await connection.start();
await connection.invoke('JoinPersonalGroup');
```

---

## Hub Events — Full Reference

### `ReceiveNotification` (from `/hubs/notifications`)

```json
{
  "id": "guid (notification ID)",
  "userId": "guid",
  "title": "New Application",
  "message": "Smith Trucking LLC (John Smith) submitted a new application.",
  "entityType": "Application",
  "entityId": "guid (application ID)",
  "isRead": false,
  "timestamp": "2026-08-31T12:00:00Z"
}
```

### `ApplicationCreated` (from `/hubs/applications`)

```json
{
  "applicationId": "guid",
  "applicationNumber": "APP-260831-A1B2",
  "companyName": "Smith Trucking LLC",
  "fullName": "John Smith",
  "email": "john@smithtrucking.com",
  "phone": "+1 (555) 123-4567",
  "equipmentType": "DryVan",
  "truckCount": 5,
  "status": "New",
  "submittedAt": "2026-08-31T12:00:00Z"
}
```

### `ApplicationStatusChanged` (from `/hubs/applications`)

```json
{
  "applicationId": "guid",
  "applicationNumber": "APP-260831-A1B2",
  "companyName": "Smith Trucking LLC",
  "fullName": "John Smith",
  "oldStatus": "New",
  "newStatus": "Reviewing",
  "timestamp": "2026-08-31T12:05:00Z"
}
```

### `ApplicationUpdated` (from `/hubs/applications`)

```json
{
  "applicationId": "guid",
  "applicationNumber": "APP-260831-A1B2",
  "companyName": "Smith Trucking LLC",
  "fullName": "John Smith",
  "status": "Reviewing"
}
```

### `ApplicationDeleted` (from `/hubs/applications`)

```json
{
  "applicationId": "guid",
  "applicationNumber": "APP-260831-A1B2",
  "timestamp": "2026-08-31T12:10:00Z"
}
```

### `DashboardUpdate` (from `/hubs/dashboard`)

This is the **unified event** — every backend action sends a `DashboardUpdate`. The `entityType` and `action` fields tell you what happened.

```json
{
  "entityType": "Application",
  "action": "Created",
  "entity": {
    "applicationId": "guid",
    "applicationNumber": "APP-260831-A1B2",
    "companyName": "Smith Trucking LLC",
    "fullName": "John Smith",
    "status": "New",
    "submittedAt": "2026-08-31T12:00:00Z"
  }
}
```

**All `entityType` + `action` combinations:**

| entityType | action | entity fields |
|------------|--------|---------------|
| `Application` | `Created` | applicationId, applicationNumber, companyName, fullName, email, phone, equipmentType, truckCount, status, submittedAt |
| `Application` | `Updated` | applicationId, applicationNumber, companyName, fullName, status |
| `Application` | `StatusChanged` | applicationId, applicationNumber, companyName, oldStatus, newStatus, timestamp |
| `Application` | `Assigned` | applicationId, applicationNumber, companyName, assignedToUserId, timestamp |
| `Application` | `ConvertedToCarrier` | applicationId, applicationNumber, carrierId, carrierName |
| `Application` | `Deleted` | applicationId, applicationNumber |
| `Load` | `Created` | loadId, loadNumber, carrierName, pickupCity, pickupState, deliveryCity, deliveryState, rate, status |
| `Load` | `Updated` | loadId, loadNumber, status |
| `Load` | `StatusChanged` | loadId, loadNumber, carrierName, oldStatus, newStatus, timestamp |
| `Load` | `Deleted` | loadId, loadNumber |
| `Carrier` | `Created` | carrierId, companyName, contactName, status |
| `Carrier` | `Updated` | carrierId, companyName, status |
| `Carrier` | `DispatcherAssigned` | carrierId, companyName, dispatcherId |
| `Carrier` | `Deleted` | carrierId, companyName |
| `Driver` | `Created` | driverId, firstName, lastName, carrierName, carrierId |
| `Driver` | `Updated` | driverId, firstName, lastName, status |
| `Driver` | `Deleted` | driverId, firstName, lastName |
| `Truck` | `Created` | truckId, truckNumber, make, model, year, carrierName, carrierId |
| `Truck` | `Updated` | truckId, truckNumber, status |
| `Truck` | `Deleted` | truckId, truckNumber |
| `Document` | `Uploaded` | documentId, fileName, documentType, carrierId, loadId, driverId |
| `Document` | `Deleted` | documentId, fileName |
| `Message` | `NewMessage` | conversationId, visitorName, message, senderType, timestamp |

### `ReceiveMessage` (from `/hubs/chat`)

```json
{
  "messageId": "guid",
  "message": "Hello, I need help with my shipment",
  "senderUserId": "guid-or-null",
  "senderType": 0,
  "timestamp": "2026-08-31T12:00:00Z"
}
```

`senderType` values:
- `0` = Visitor
- `1` = Admin

---

## REST API — Notifications

**Base URL:** `https://api.driventa.us/api/Notifications`

**Auth:** JWT Bearer token required for all endpoints.

### GET /api/Notifications

Get paginated notifications for the authenticated user.

**Query Parameters:**
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `page` | int | 1 | Page number |
| `pageSize` | int | 20 | Items per page |
| `isRead` | bool? | null | Filter by read status |

**Response (200):**
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": "guid",
        "userId": "guid",
        "type": 0,
        "title": "New Application",
        "message": "Smith Trucking LLC submitted a new application.",
        "entityType": "Application",
        "entityId": "guid",
        "isRead": false,
        "createdAt": "2026-08-31T12:00:00Z"
      }
    ],
    "page": 1,
    "pageSize": 20,
    "totalCount": 45
  }
}
```

### GET /api/Notifications/{id}

Get a single notification by ID.

**Response (200):**
```json
{
  "success": true,
  "data": {
    "id": "guid",
    "userId": "guid",
    "type": 0,
    "title": "New Application",
    "message": "Smith Trucking LLC submitted a new application.",
    "entityType": "Application",
    "entityId": "guid",
    "isRead": false,
    "createdAt": "2026-08-31T12:00:00Z"
  }
}
```

### GET /api/Notifications/unread-count

Get the count of unread notifications for the authenticated user.

**Response (200):**
```json
{
  "success": true,
  "data": 7
}
```

### PATCH /api/Notifications/{id}/read

Mark a single notification as read.

**Response (200):**
```json
{
  "success": true,
  "message": "Notification marked as read."
}
```

### POST /api/Notifications/read-all

Mark all unread notifications as read for the authenticated user.

**Response (200):**
```json
{
  "success": true,
  "data": { "count": 7 },
  "message": "All notifications marked as read."
}
```

---

## REST API — Conversations & Messages

**Base URL:** `https://api.driventa.us/api/Messages`

**Auth:** JWT Bearer token required.

### GET /api/Messages/conversations

Get paginated list of conversations.

**Query Parameters:**
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `page` | int | 1 | Page number |
| `pageSize` | int | 20 | Items per page |

**Response (200):**
```json
{
  "success": true,
  "data": {
    "items": [
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
    ],
    "page": 1,
    "pageSize": 20,
    "totalCount": 15
  }
}
```

### GET /api/Messages/conversations/{id}

Get a single conversation with all messages.

**Response (200):**
```json
{
  "success": true,
  "data": {
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
    "lastMessageSenderType": 0,
    "messages": [
      {
        "id": "guid",
        "conversationId": "guid",
        "senderType": 0,
        "senderUserId": null,
        "senderName": "John Visitor",
        "content": "Hello, I need help",
        "isRead": true,
        "createdAt": "2026-08-31T10:00:00Z"
      },
      {
        "id": "guid",
        "conversationId": "guid",
        "senderType": 1,
        "senderUserId": "guid",
        "senderName": "Admin User",
        "content": "Hi! How can I help you?",
        "isRead": false,
        "createdAt": "2026-08-31T10:05:00Z"
      }
    ]
  }
}
```

### POST /api/Messages

Send a message in a conversation (admin side).

**Request Body:**
```json
{
  "conversationId": "guid",
  "content": "Hello! How can I assist you today?"
}
```

**Response (200):**
```json
{
  "success": true,
  "data": {
    "id": "guid",
    "conversationId": "guid",
    "senderType": 1,
    "senderUserId": "guid",
    "senderName": "Admin User",
    "content": "Hello! How can I assist you today?",
    "isRead": false,
    "createdAt": "2026-08-31T12:00:00Z"
  }
}
```

### PATCH /api/Messages/conversations/{id}/read

Mark all messages in a conversation as read.

**Response (200):**
```json
{
  "success": true,
  "message": "Conversation marked as read."
}
```

---

## Data Schemas

### NotificationType Enum

| Value | Name | Description |
|-------|------|-------------|
| 0 | `NewApplication` | A new carrier application was submitted |
| 1 | `NewMessage` | A new chat message was received |
| 2 | `LoadStatusChanged` | A load's status changed |
| 3 | `DocumentExpiring` | A document is expiring (reserved) |
| 4 | `DocumentUploaded` | A document was uploaded |
| 5 | `CarrierAssigned` | A carrier was assigned/converted |
| 6 | `CarrierCreated` | A new carrier was added |
| 7 | `DriverCreated` | A new driver was added |
| 8 | `TruckCreated` | A new truck was added |
| 9 | `LoadCreated` | A new load was created |
| 10 | `DispatcherAssigned` | A dispatcher was assigned to a carrier |
| 11 | `ApplicationAssigned` | An application was assigned to a user |
| 12 | `ApplicationStatusChanged` | An application's status changed |

### ApplicationStatus Enum

| Value | Name |
|-------|------|
| 0 | `New` |
| 1 | `Reviewing` |
| 2 | `Contacted` |
| 3 | `Qualified` |
| 4 | `Approved` |
| 5 | `Rejected` |
| 6 | `Onboarded` |

### LoadStatus Enum

| Value | Name |
|-------|------|
| 0 | `Available` |
| 1 | `Negotiating` |
| 2 | `Booked` |
| 3 | `Dispatched` |
| 4 | `PickedUp` |
| 5 | `InTransit` |
| 6 | `Delivered` |
| 7 | `Completed` |
| 8 | `Cancelled` |
| 9 | `Issue` |

### CarrierStatus Enum

| Value | Name |
|-------|------|
| 0 | `Lead` |
| 1 | `Onboarding` |
| 2 | `Active` |
| 3 | `Paused` |
| 4 | `Inactive` |
| 5 | `Suspended` |

### DriverStatus Enum

| Value | Name |
|-------|------|
| 0 | `Available` |
| 1 | `Assigned` |
| 2 | `Driving` |
| 3 | `OffDuty` |
| 4 | `Inactive` |

### TruckStatus Enum

| Value | Name |
|-------|------|
| 0 | `Available` |
| 1 | `Assigned` |
| 2 | `InTransit` |
| 3 | `Maintenance` |
| 4 | `Inactive` |

### SenderType Enum (Chat)

| Value | Name |
|-------|------|
| 0 | `Visitor` |
| 1 | `Admin` |

### DocumentType Enum

| Value | Name |
|-------|------|
| 0 | `Other` |
| 1 | `Insurance` |
| 2 | `W9` |
| 3 | `MC` |
| 4 | `DOT` |
| 5 | `OperatingAuthority` |
| 6 | `FTA` |
| 7 | `BusinessLicense` |

### EquipmentType Enum

| Value | Name |
|-------|------|
| 0 | `DryVan` |
| 1 | `Reefer` |
| 2 | `Flatbed` |
| 3 | `StepDeck` |
| 4 | `Lowboy` |
| 5 | `Tanker` |
| 6 | `CarHauler` |
| 7 | `BoxTruck` |
| 8 | `Other` |

---

## Notification Recipients — Who Gets What

| Event | Recipients | Delivery Method |
|-------|-----------|-----------------|
| New application submitted | All SuperAdmin, Admin, DispatchManager, Dispatcher users | Persistent notification + `ApplicationsHub` broadcast + `DashboardHub` broadcast |
| Application status change | Assigned user (if any) | Persistent notification + `ApplicationsHub` broadcast + `DashboardHub` broadcast |
| Application assigned | The assigned user | Persistent notification + `ApplicationsHub` broadcast |
| Application converted to carrier | The user who performed the conversion | Persistent notification + `ApplicationsHub` broadcast |
| New carrier created | All SuperAdmin, Admin, DispatchManager users | Persistent notification + `DashboardHub` broadcast |
| Dispatcher assigned to carrier | The assigned dispatcher | Persistent notification + `DashboardHub` broadcast |
| New load created | Carrier's assigned dispatcher | Persistent notification + `DashboardHub` broadcast |
| Load status changed | Carrier's assigned dispatcher | Persistent notification + `DashboardHub` broadcast |
| New driver added | Carrier's assigned dispatcher | Persistent notification + `DashboardHub` broadcast |
| New truck added | Carrier's assigned dispatcher | Persistent notification + `DashboardHub` broadcast |
| Driver/Truck status changed | Carrier's assigned dispatcher | Persistent notification + `DashboardHub` broadcast |
| Document uploaded | Owner of linked carrier/load/driver's dispatcher | Persistent notification + `DashboardHub` broadcast |
| New message from visitor | Conversation's assigned admin | Persistent notification via `NotificationBroadcaster` |
| Admin sends message | Conversation participants | Real-time via `ChatHub` conversation group |

---

## Implementation Checklist for Dashboard Designer

### Phase 1: Connection Setup
- [ ] Install `@microsoft/signalr` package
- [ ] Create a SignalR connection utility/service that manages all 4 hubs
- [ ] Handle automatic reconnect for all connections
- [ ] Pass JWT token via `accessTokenFactory` for all hubs

### Phase 2: Notification Bell/Panel
- [ ] On dashboard load, connect to `/hubs/notifications` and call `JoinPersonalGroup`
- [ ] Listen for `ReceiveNotification` events
- [ ] Show badge count on bell icon (also call `GET /api/Notifications/unread-count` on load)
- [ ] Show notification dropdown/panel with list from `GET /api/Notifications`
- [ ] Implement `PATCH /api/Notifications/{id}/read` on notification click
- [ ] Implement `POST /api/Notifications/read-all` for "Mark all read" button

### Phase 3: Live Application Feed
- [ ] Connect to `/hubs/applications` and call `JoinAdminGroup`
- [ ] Listen for `ApplicationCreated` — prepend new application to list
- [ ] Listen for `ApplicationStatusChanged` — update status badge in list
- [ ] Listen for `ApplicationUpdated` — update row data
- [ ] Listen for `ApplicationDeleted` — remove from list

### Phase 4: Unified Dashboard Feed
- [ ] Connect to `/hubs/dashboard` and call `JoinAdminGroup`
- [ ] Listen for `DashboardUpdate` events
- [ ] Route based on `entityType` + `action` to update relevant dashboard sections
- [ ] Show a live activity feed/sidebar with recent events

### Phase 5: Real-Time Chat
- [ ] Connect to `/hubs/chat`
- [ ] When opening a conversation, call `JoinConversation(conversationId)`
- [ ] Listen for `ReceiveMessage` to display new messages
- [ ] Send messages via `POST /api/Messages` (REST) or `SendMessage` (Hub)
- [ ] Call `MarkAsRead` when viewing a conversation
- [ ] Show unread count badges on conversation list

### Phase 6: Sound/Visual Alerts
- [ ] Play a subtle sound on `ReceiveNotification` (new notification)
- [ ] Show a toast/snackbar on `ApplicationCreated` (new application)
- [ ] Flash the browser tab title when there are unread notifications
- [ ] Consider browser push notifications for critical events

---

## CORS Configuration

The backend allows all origins for SignalR connections:

```csharp
policy.SetIsOriginAllowed(_ => true)
    .AllowAnyHeader()
    .AllowAnyMethod();
```

No CORS issues expected. Connect from any frontend domain.

---

## Error Handling

- SignalR hubs throw `HubException` with descriptive messages for validation errors
- All REST endpoints return standard `ApiResponse<T>` with `success`, `data`, and `message` fields
- HTTP status codes: 200 (success), 400 (bad request), 401 (unauthorized), 404 (not found), 429 (rate limited)
- Rate limiting: 10 requests per minute on public endpoints (`/api/public/*`)

---

## Notification Type Quick Reference

| Type | When | Persistent? | Real-time? |
|------|------|-------------|------------|
| `NewApplication` | Public form submitted | ✅ | ✅ (ApplicationsHub + DashboardHub) |
| `ApplicationStatusChanged` | Status changed | ✅ | ✅ (ApplicationsHub + DashboardHub) |
| `ApplicationAssigned` | Assigned to user | ✅ | ✅ (ApplicationsHub + DashboardHub) |
| `NewMessage` | Visitor sends message | ✅ | ✅ (NotificationHub) |
| `LoadStatusChanged` | Load status updated | ✅ | ✅ (DashboardHub) |
| `LoadCreated` | New load created | ✅ | ✅ (DashboardHub) |
| `CarrierCreated` | New carrier added | ✅ | ✅ (DashboardHub) |
| `CarrierAssigned` | Carrier converted from app | ✅ | ✅ (ApplicationsHub + DashboardHub) |
| `DispatcherAssigned` | Dispatcher assigned to carrier | ✅ | ✅ (DashboardHub) |
| `DriverCreated` | New driver added | ✅ | ✅ (DashboardHub) |
| `TruckCreated` | New truck added | ✅ | ✅ (DashboardHub) |
| `DocumentUploaded` | Document uploaded | ✅ | ✅ (DashboardHub) |
