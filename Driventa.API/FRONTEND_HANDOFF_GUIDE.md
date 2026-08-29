# Driventa Frontend & Public Website Handoff Guide

This document is meant for the dashboard frontend and public website team. It explains how to authenticate, call the REST API, and connect to real-time chat and notification hubs so the backend stays perfectly synced.

## 1) Base API URL

Use the backend base URL from the environment config.

- Local development: http://localhost:5165
- Production: use the deployed backend domain

Examples:

- Auth: /api/Auth
- Notifications: /api/Notifications
- Public chat: /api/public/chat
- SignalR chat hub: /hubs/chat
- SignalR notification hub: /hubs/notifications

---

## 2) Standard API Response Shape

All controllers return a consistent response envelope:

```json
{
  "success": true,
  "message": "Optional message",
  "data": {},
  "errors": null
}
```

For paginated responses:

```json
{
  "success": true,
  "message": null,
  "data": {
    "items": [{ "id": "..." }],
    "page": 1,
    "pageSize": 20,
    "totalCount": 100
  },
  "errors": null
}
```

Important:

- Use `success` to check whether the request was successful.
- `data` contains the actual payload.
- For errors, the backend returns the standard failure response or HTTP error status.

---

## 3) Authentication Flow

### 3.1 Login

Endpoint:

```http
POST /api/Auth/login
```

Body:

```json
{
  "email": "admin@driventa.com",
  "password": "Admin@123"
}
```

Response:

```json
{
  "success": true,
  "message": null,
  "data": {
    "accessToken": "eyJ...",
    "refreshToken": "eyJ...",
    "expiresAt": "2026-08-29T12:00:00Z",
    "userProfile": {
      "id": "<guid>",
      "firstName": "Super",
      "lastName": "Admin",
      "email": "admin@driventa.com",
      "phoneNumber": null,
      "role": "SuperAdmin"
    }
  },
  "errors": null
}
```

### 3.2 Save token in frontend

Store the JWT `accessToken` in memory, secure storage, or a secure cookie depending on your app architecture.

Always send it in every authenticated request:

```http
Authorization: Bearer <accessToken>
```

### 3.3 Refresh token

Endpoint:

```http
POST /api/Auth/refresh
```

Body:

```json
{
  "refreshToken": "<refresh-token>"
}
```

### 3.4 Current user

Endpoint:

```http
GET /api/Auth/me
```

Requires JWT.

### 3.5 Logout

Endpoint:

```http
POST /api/Auth/logout
```

Requires JWT.

---

## 4) Auth and Role Rules

The backend creates JWT claims including:

- `NameIdentifier` = user GUID
- email claim
- name claim
- role claims
- permissions claims

Roles available in the system:

- SuperAdmin
- Admin
- DispatchManager
- Dispatcher
- Sales

Important backend behavior:

- Protected API routes use `[Authorize]`.
- User roles and permissions are embedded into JWT claims.
- The frontend should not trust UI-only role checks alone; the backend is the source of truth.

---

## 5) Core REST Endpoint Groups

These are the main controller route patterns used by the dashboard:

- `/api/Auth`
- `/api/Applications`
- `/api/Billing`
- `/api/Brokers`
- `/api/Carriers`
- `/api/Dashboard`
- `/api/Dispatchers`
- `/api/Documents`
- `/api/Drivers`
- `/api/Loads`
- `/api/Messages`
- `/api/Notifications`
- `/api/Reports`
- `/api/Roles`
- `/api/Trucks`
- `/api/Users`
- `/api/PublicApplications`
- `/api/PublicContact`

The UI should follow the controller name and route pattern exactly. Swagger is the final source of truth when a screen is being built.

---

## 6) Public Website Chat Flow

The public website is not authenticated by JWT.

### 6.1 Create a public chat session

Endpoint:

```http
POST /api/public/chat/session
```

Body:

```json
{
  "visitorName": "Ali Khan",
  "visitorEmail": "ali@example.com",
  "visitorPhone": "+923001234567"
}
```

Response:

```json
{
  "success": true,
  "message": "Chat session created.",
  "data": {
    "conversationId": "<guid>",
    "visitorId": "<string>"
  },
  "errors": null
}
```

### 6.2 Public website usage pattern

1. Open the chat widget.
2. Call `POST /api/public/chat/session`.
3. Save the returned `conversationId`.
4. Connect to SignalR hub `/hubs/chat` without JWT.
5. Call `JoinConversation(conversationId)`.
6. Send messages using `SendMessage(conversationId, message)`.
7. Listen for `ReceiveMessage`.

This allows visitor chat to work even though the public website is not authenticated.

---

## 7) Real-Time Chat with SignalR

Hub URL:

```text
/hubs/chat
```

### 7.1 Connect from frontend

For authenticated dashboard users:

```js
const connection = new signalR.HubConnectionBuilder()
  .withUrl("http://localhost:5165/hubs/chat", {
    accessTokenFactory: () => token,
    withCredentials: false,
  })
  .withAutomaticReconnect()
  .build();
```

For public website visitors:

```js
const connection = new signalR.HubConnectionBuilder()
  .withUrl("http://localhost:5165/hubs/chat")
  .withAutomaticReconnect()
  .build();
```

### 7.2 Join a conversation

Client method:

```js
await connection.invoke("JoinConversation", conversationId);
```

Server validation:

- conversation must exist
- conversation must be active
- then the client is added to the SignalR group for that conversation

### 7.3 Send a message

Client method:

```js
await connection.invoke("SendMessage", conversationId, message);
```

Server-side behavior:

- creates a new message row
- sets `ConversationId`
- sets `SenderType` based on user auth status
- `SenderUserId` is set from the JWT claim when authenticated
- sets `LastMessageAt` on the conversation
- broadcasts to all clients in that conversation group

### 7.4 Receive message event

Server sends:

```js
connection.on("ReceiveMessage", (payload) => {
  console.log(payload);
});
```

Payload example:

```json
{
  "messageId": "<guid>",
  "message": "Hello team",
  "senderUserId": "<guid>",
  "senderType": "Admin",
  "timestamp": "2026-08-29T12:00:00Z"
}
```

### 7.5 Mark messages as read

Client method:

```js
await connection.invoke("MarkAsRead", conversationId);
```

Server broadcasts:

```js
connection.on("MessagesRead", (conversationId) => {
  // update UI message state
});
```

### 7.6 Important chat notes

- The conversation group is identified by the `conversationId` string.
- All participants in the same conversation receive updates instantly.
- The hub does not create custom notification messages; it broadcast message rows.
- For admin dashboard chat, the authenticated user is treated as admin if JWT is present.

---

## 8) Real-Time Notifications with SignalR

Hub URL:

```text
/hubs/notifications
```

This hub is protected with `[Authorize]`.

### 8.1 Connect from authenticated dashboard

```js
const notificationConnection = new signalR.HubConnectionBuilder()
  .withUrl("http://localhost:5165/hubs/notifications", {
    accessTokenFactory: () => token,
    withCredentials: false,
  })
  .withAutomaticReconnect()
  .build();
```

### 8.2 Join personal notification group

```js
await notificationConnection.invoke("JoinPersonalGroup");
```

Server behavior:

- reads authenticated user ID from JWT
- adds the connection to a group such as `user_<guid>`

### 8.3 Receive notifications event

Server sends to that user’s personal group:

```js
notificationConnection.on("ReceiveNotification", (payload) => {
  console.log(payload);
});
```

Payload example:

```json
{
  "title": "New Load Assigned",
  "message": "A new load was assigned to you.",
  "timestamp": "2026-08-29T12:00:00Z"
}
```

### 8.4 Notification REST endpoints

#### Get all notifications

```http
GET /api/Notifications?page=1&pageSize=20&isRead=false
```

Response example:

```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": "<guid>",
        "userId": "<guid>",
        "type": "LoadAssigned",
        "title": "New Load Assigned",
        "message": "A new load was assigned to you.",
        "entityType": "Load",
        "entityId": "<guid>",
        "isRead": false,
        "createdAt": "2026-08-29T11:00:00Z"
      }
    ],
    "page": 1,
    "pageSize": 20,
    "totalCount": 25
  }
}
```

#### Unread count

```http
GET /api/Notifications/unread-count
```

#### Mark single notification as read

```http
PATCH /api/Notifications/{id}/read
```

#### Mark all notifications as read

```http
POST /api/Notifications/read-all
```

#### Get single notification

```http
GET /api/Notifications/{id}
```

### 8.5 Important notification notes

- Real-time notifications are user-specific, not global.
- The backend authorizes a user to only receive notifications for their own user group.
- Notification REST endpoints are the reliable source for notification history and unread counts.
- SignalR is the real-time channel for instant updates; REST is the persistent history layer.

---

## 9) Recommended Frontend Architecture

### 9.1 Dashboard frontend

Use:

- JWT auth with refresh flow
- SignalR connection to `/hubs/chat` and `/hubs/notifications`
- REST polling or cache refresh after realtime events
- `unread-count` endpoint for sidebar badges
- notification list page using `/api/Notifications`

### 9.2 Public website

Use:

- `POST /api/public/chat/session` to create chat session
- SignalR connection to `/hubs/chat`
- conversation ID stored in local state or session storage
- no JWT required for public chat

---

## 10) Frontend Implementation Checklist

### Dashboard login checklist

- [ ] Login and store accessToken + refreshToken
- [ ] Refresh token before expiry
- [ ] Add Bearer token to all protected requests
- [ ] Connect to `/hubs/chat` after login
- [ ] Connect to `/hubs/notifications` after login
- [ ] Call `JoinConversation` before message send
- [ ] Attach `ReceiveMessage` listener
- [ ] Attach `ReceiveNotification` listener
- [ ] Fetch unread count on app load
- [ ] Refresh notification list after read actions

### Public website checklist

- [ ] Create chat session via `/api/public/chat/session`
- [ ] Save conversationId
- [ ] Connect to `/hubs/chat`
- [ ] Call `JoinConversation(conversationId)`
- [ ] Send and receive messages via the hub
- [ ] Optionally keep a conversation session for page reloads

---

## 11) CORS and Connectivity Rules

The API uses a CORS policy named `Dashboard` that allows any origin, any method, and any header.

This is useful for frontend apps running on different ports or domains during development, but production should still be restricted to known allowed origins.

---

## 12) Best Practices for Syncing Chat and Notifications

1. Use REST for history and persistence.
2. Use SignalR for live updates.
3. Keep message IDs and notification IDs stable in the client state.
4. After a realtime `ReceiveNotification`, call the unread count endpoint to keep badges accurate.
5. After a realtime `ReceiveMessage`, append the message to state and refresh the conversation list if needed.
6. Mark conversation messages as read on the client when the user opens the chat.
7. Always rely on backend validation instead of trusting frontend-only state.

---

## 13) Quick Reference

### Auth

```http
POST /api/Auth/login
POST /api/Auth/refresh
POST /api/Auth/logout
GET /api/Auth/me
```

### Public chat

```http
POST /api/public/chat/session
```

### Real-time chat hub

```text
/hubs/chat
```

Methods:

```js
JoinConversation(conversationId);
SendMessage(conversationId, message);
MarkAsRead(conversationId);
```

Events:

```js
ReceiveMessage;
MessagesRead;
```

### Real-time notification hub

```text
/hubs/notifications
```

Methods:

```js
JoinPersonalGroup();
```

Events:

```js
Connected;
ReceiveNotification;
```

### Notification REST

```http
GET /api/Notifications
GET /api/Notifications/{id}
GET /api/Notifications/unread-count
PATCH /api/Notifications/{id}/read
POST /api/Notifications/read-all
```

---

## 14) Final Note

The backend is already designed to support:

- real-time chat between visitors and dashboard users
- real-time personal notifications
- secure JWT-protected dashboard APIs
- public website chat sessions without auth

For the frontend implementation, the most important rule is simple:

- REST = persistent data/history
- SignalR = live updates and instant sync
- JWT = authenticated dashboard access
- public chat = session-based, no JWT required

If you want, I can also turn this into a shorter frontend-ready checklist or generate a full React/Next.js example for the dashboard and public chat connection code.
