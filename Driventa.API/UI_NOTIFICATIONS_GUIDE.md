# Driventa Notifications — Complete UI Developer Guide

> **Purpose:** This document explains EXACTLY how to receive live notifications in the dashboard.
> If notifications are not showing, follow every step below — the issue is 100% on the frontend side.

---

## How Notifications Work (Visual Flow)

```
┌──────────────────────────────────────────────────────────────────┐
│                     BACKEND SERVER                                │
│                                                                  │
│  Someone submits an application ──► Controller saves to DB       │
│        │                                  │                      │
│        │                                  ▼                      │
│        │                     NotificationService saves           │
│        │                     a Notification row in DB            │
│        │                                  │                      │
│        │                                  ▼                      │
│        │                     NotificationBroadcaster             │
│        │                     sends to SignalR Hub                │
│        │                                  │                      │
│        ▼                                  ▼                      │
│  ApplicationsHub              NotificationHub                    │
│  broadcasts to                sends to                           │
│  "admins" group               "user_{userId}" group              │
│        │                                  │                      │
└────────┼──────────────────────────────────┼──────────────────────┘
         │                                  │
         ▼                                  ▼
┌──────────────────────────────────────────────────────────────────┐
│                     YOUR DASHBOARD (Frontend)                     │
│                                                                  │
│  AppConn.on("ApplicationCreated")    NotifConn.on("ReceiveNotif")│
│         │                                  │                     │
│         ▼                                  ▼                     │
│  Update table/list              Show toast + update badge        │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

---

## Step 1: Install SignalR Package

### JavaScript / TypeScript (React, Next.js, Vue, Angular)
```bash
npm install @microsoft/signalr
```

### Flutter / Dart
```yaml
dependencies:
  signalr_core: ^1.1.2
```

### C# / Blazor
```bash
dotnet add package Microsoft.AspNetCore.SignalR.Client
```

---

## Step 2: Login and Get JWT Token

```
POST https://api.driventa.us/api/Auth/login
Content-Type: application/json

{
  "email": "admin@driventa.com",
  "password": "Admin@123"
}
```

**Response you get back:**
```json
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "expiresAt": "2026-08-31T12:15:00Z",
    "userProfile": {
      "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "firstName": "Super",
      "lastName": "Admin",
      "email": "admin@driventa.com",
      "role": "SuperAdmin"
    }
  }
}
```

**Save these two tokens:**
- `accessToken` — expires in 15 minutes, use for all API calls
- `refreshToken` — expires in 7 days, use to get new accessToken

---

## Step 3: Connect to All 4 SignalR Hubs

You MUST connect to 4 separate hubs. Each hub has a different purpose.

### Hub 1: Notification Hub (Personal Notifications)

**URL:** `https://api.driventa.us/hubs/notifications?access_token=YOUR_JWT`

```javascript
import * as signalR from "@microsoft/signalr";

const API_URL = "https://api.driventa.us";
const TOKEN = "eyJhbGciOiJIUzI1NiIs..."; // from login

const notifConnection = new signalR.HubConnectionBuilder()
  .withUrl(`${API_URL}/hubs/notifications`, {
    accessTokenFactory: () => TOKEN
  })
  .withAutomaticReconnect()
  .build();
```

### Hub 2: Applications Hub (Live Application Feed)

**URL:** `https://api.driventa.us/hubs/applications?access_token=YOUR_JWT`

```javascript
const appConnection = new signalR.HubConnectionBuilder()
  .withUrl(`${API_URL}/hubs/applications`, {
    accessTokenFactory: () => TOKEN
  })
  .withAutomaticReconnect()
  .build();
```

### Hub 3: Dashboard Hub (Unified Feed)

**URL:** `https://api.driventa.us/hubs/dashboard?access_token=YOUR_JWT`

```javascript
const dashConnection = new signalR.HubConnectionBuilder()
  .withUrl(`${API_URL}/hubs/dashboard`, {
    accessTokenFactory: () => TOKEN
  })
  .withAutomaticReconnect()
  .build();
```

### Hub 4: Chat Hub (Real-Time Messages)

**URL:** `https://api.driventa.us/hubs/chat?access_token=YOUR_JWT`

```javascript
const chatConnection = new signalR.HubConnectionBuilder()
  .withUrl(`${API_URL}/hubs/chat`, {
    accessTokenFactory: () => TOKEN
  })
  .withAutomaticReconnect()
  .build();
```

---

## Step 4: Start Connections + Join Groups (CRITICAL)

**This is the step most developers miss.** After connecting, you MUST call the join methods:

```javascript
async function initializeRealtime() {
  // Start all 4 connections
  await notifConnection.start();
  await appConnection.start();
  await dashConnection.start();
  await chatConnection.start();

  console.log("All 4 hubs connected!");

  // JOIN GROUPS — THIS IS REQUIRED
  await notifConnection.invoke("JoinPersonalGroup");  // ← Must call this!
  await appConnection.invoke("JoinAdminGroup");        // ← Must call this!
  await dashConnection.invoke("JoinAdminGroup");        // ← Must call this!
  // Chat hub: join per conversation, not on connect

  console.log("All groups joined!");
}
```

**If you don't call `JoinPersonalGroup` and `JoinAdminGroup`, you will NOT receive any notifications.**

---

## Step 5: Register Event Listeners (What Happens When Notification Arrives)

### Listen for Personal Notifications

```javascript
notifConnection.on("ReceiveNotification", (notification) => {
  // notification = {
  //   id: "guid",
  //   userId: "guid",
  //   title: "New Application",
  //   message: "Smith Trucking LLC submitted a new application.",
  //   entityType: "Application",
  //   entityId: "guid",
  //   isRead: false,
  //   timestamp: "2026-08-31T12:00:00Z"
  // }

  console.log("NEW NOTIFICATION:", notification);

  // 1. Show toast/popup
  showToast(notification.title, notification.message);

  // 2. Update notification bell badge
  setUnreadCount(prev => prev + 1);

  // 3. Prepend to notification list
  setNotifications(prev => [notification, ...prev]);
});
```

### Listen for Application Events

```javascript
appConnection.on("ApplicationCreated", (data) => {
  // data = {
  //   applicationId: "guid",
  //   applicationNumber: "APP-260831-A1B2",
  //   companyName: "Smith Trucking LLC",
  //   fullName: "John Smith",
  //   email: "john@company.com",
  //   phone: "+1 (555) 000-0000",
  //   equipmentType: "DryVan",
  //   truckCount: 5,
  //   status: "New",
  //   submittedAt: "2026-08-31T12:00:00Z"
  // }

  console.log("NEW APPLICATION:", data);

  // Add new row to applications table
  setApplications(prev => [data, ...prev]);

  // Show banner
  showBanner(`${data.companyName} submitted a new application!`);
});

appConnection.on("ApplicationStatusChanged", (data) => {
  // data = {
  //   applicationId: "guid",
  //   applicationNumber: "APP-260831-A1B2",
  //   companyName: "Smith Trucking LLC",
  //   oldStatus: "New",
  //   newStatus: "Reviewing",
  //   timestamp: "2026-08-31T12:05:00Z"
  // }

  console.log("STATUS CHANGED:", data);

  // Update status badge in table
  updateApplicationStatus(data.applicationId, data.newStatus);
});

appConnection.on("ApplicationUpdated", (data) => {
  // data = { applicationId, applicationNumber, companyName, fullName, status }
  updateApplicationRow(data);
});

appConnection.on("ApplicationDeleted", (data) => {
  // data = { applicationId, applicationNumber, timestamp }
  removeApplicationRow(data.applicationId);
});
```

### Listen for Dashboard Updates

```javascript
dashConnection.on("DashboardUpdate", (data) => {
  // data = {
  //   entityType: "Application" | "Load" | "Carrier" | "Driver" | "Truck" | "Document" | "Message",
  //   action: "Created" | "Updated" | "Deleted" | "StatusChanged" | "Assigned" | "DispatcherAssigned" | "Uploaded" | "NewMessage",
  //   entity: { ... fields depend on entityType and action ... }
  // }

  console.log("DASHBOARD UPDATE:", data);

  // Add to activity feed
  addToActivityFeed(data);

  // Update stats if needed
  if (data.entityType === "Application" && data.action === "Created") {
    incrementStat("newApplications");
  }
});
```

### Listen for Chat Messages

```javascript
chatConnection.on("ReceiveMessage", (msg) => {
  // msg = {
  //   messageId: "guid",
  //   message: "Hello, I need help",
  //   senderUserId: "guid" or null,
  //   senderType: 0,
  //   timestamp: "2026-08-31T12:00:00Z"
  // }

  console.log("NEW MESSAGE:", msg);
  appendMessageToChat(msg);
});

chatConnection.on("MessagesRead", (conversationId) => {
  console.log("Messages read in:", conversationId);
  markMessagesAsRead(conversationId);
});
```

---

## Step 6: Handle Reconnection

When the connection drops and reconnects, you MUST re-join the groups:

```javascript
notifConnection.onreconnected(async () => {
  console.log("NotificationHub reconnected, rejoining group...");
  await notifConnection.invoke("JoinPersonalGroup");
});

appConnection.onreconnected(async () => {
  console.log("ApplicationsHub reconnected, rejoining group...");
  await appConnection.invoke("JoinAdminGroup");
});

dashConnection.onreconnected(async () => {
  console.log("DashboardHub reconnected, rejoining group...");
  await dashConnection.invoke("JoinAdminGroup");
});
```

---

## Step 7: Fetch Initial Data (REST API)

On dashboard load, fetch existing data via REST:

```javascript
const API_BASE = "https://api.driventa.us";

// Helper functions
async function apiGet(path) {
  const res = await fetch(`${API_BASE}${path}`, {
    headers: { "Authorization": `Bearer ${TOKEN}` }
  });
  return res.json();
}

async function apiPatch(path, body) {
  const res = await fetch(`${API_BASE}${path}`, {
    method: "PATCH",
    headers: {
      "Authorization": `Bearer ${TOKEN}`,
      "Content-Type": "application/json"
    },
    body: JSON.stringify(body)
  });
  return res.json();
}

async function apiPost(path, body) {
  const res = await fetch(`${API_BASE}${path}`, {
    method: "POST",
    headers: {
      "Authorization": `Bearer ${TOKEN}`,
      "Content-Type": "application/json"
    },
    body: JSON.stringify(body)
  });
  return res.json();
}

// Fetch initial data
const unreadCount = await apiGet("/api/Notifications/unread-count");
// Response: { success: true, data: 7 }

const notifications = await apiGet("/api/Notifications?page=1&pageSize=20");
// Response: { success: true, data: { items: [...], page: 1, pageSize: 20, totalCount: 45 } }

const applications = await apiGet("/api/Applications?page=1&pageSize=20");
// Response: { success: true, data: { items: [...], page: 1, pageSize: 20, totalCount: 12 } }

const summary = await apiGet("/api/Dashboard/summary");
// Response: { success: true, data: { newApplications: 12, activeCarriers: 48, ... } }
```

---

## Step 8: Mark Notifications as Read

```javascript
// Mark single notification as read
async function markNotificationRead(notificationId) {
  await apiPatch(`/api/Notifications/${notificationId}/read`);
  // Response: { success: true, message: "Notification marked as read." }

  // Update UI
  setUnreadCount(prev => prev - 1);
  updateNotificationInList(notificationId, { isRead: true });
}

// Mark ALL notifications as read
async function markAllNotificationsRead() {
  await apiPost("/api/Notifications/read-all");
  // Response: { success: true, data: { count: 7 }, message: "All notifications marked as read." }

  // Update UI
  setUnreadCount(0);
  markAllAsReadInList();
}
```

---

## Step 9: Join a Chat Conversation

```javascript
async function openConversation(conversationId) {
  // Join the conversation room
  await chatConnection.invoke("JoinConversation", conversationId);

  // Fetch conversation history
  const conv = await apiGet(`/api/Messages/conversations/${conversationId}`);
  // Response: { success: true, data: { id, visitorName, messages: [...], ... } }

  setMessages(conv.data.messages);
}

// Send a message
async function sendMessage(conversationId, content) {
  await chatConnection.invoke("SendMessage", conversationId, content);

  // OR via REST:
  // await apiPost("/api/Messages", { conversationId, content });
}

// Mark conversation as read
async function markConversationRead(conversationId) {
  await chatConnection.invoke("MarkAsRead", conversationId);

  // OR via REST:
  // await apiPatch(`/api/Messages/conversations/${conversationId}/read`);
}
```

---

## Complete Working Example (React)

```jsx
import { useEffect, useState, useRef } from "react";
import * as signalR from "@microsoft/signalr";

const API = "https://api.driventa.us";

export function useNotifications(token) {
  const [notifications, setNotifications] = useState([]);
  const [unreadCount, setUnreadCount] = useState(0);
  const [connected, setConnected] = useState(false);
  const connections = useRef({});

  useEffect(() => {
    if (!token) return;

    async function init() {
      // 1. Create connections
      const notifConn = new signalR.HubConnectionBuilder()
        .withUrl(`${API}/hubs/notifications`, {
          accessTokenFactory: () => token
        })
        .withAutomaticReconnect()
        .build();

      const appConn = new signalR.HubConnectionBuilder()
        .withUrl(`${API}/hubs/applications`, {
          accessTokenFactory: () => token
        })
        .withAutomaticReconnect()
        .build();

      const dashConn = new signalR.HubConnectionBuilder()
        .withUrl(`${API}/hubs/dashboard`, {
          accessTokenFactory: () => token
        })
        .withAutomaticReconnect()
        .build();

      connections.current = { notifConn, appConn, dashConn };

      // 2. Register listeners BEFORE starting
      notifConn.on("ReceiveNotification", (n) => {
        setNotifications(prev => [n, ...prev]);
        setUnreadCount(prev => prev + 1);
      });

      appConn.on("ApplicationCreated", (data) => {
        console.log("New application:", data);
        // Update your applications list
      });

      appConn.on("ApplicationStatusChanged", (data) => {
        console.log("Status changed:", data);
        // Update status in your list
      });

      dashConn.on("DashboardUpdate", (data) => {
        console.log("Dashboard update:", data);
        // Update activity feed
      });

      // 3. Start connections
      await notifConn.start();
      await appConn.start();
      await dashConn.start();

      // 4. Join groups (CRITICAL!)
      await notifConn.invoke("JoinPersonalGroup");
      await appConn.invoke("JoinAdminGroup");
      await dashConn.invoke("JoinAdminGroup");

      setConnected(true);

      // 5. Handle reconnects
      notifConn.onreconnected(async () => {
        await notifConn.invoke("JoinPersonalGroup");
      });
      appConn.onreconnected(async () => {
        await appConn.invoke("JoinAdminGroup");
      });
      dashConn.onreconnected(async () => {
        await dashConn.invoke("JoinAdminGroup");
      });

      // 6. Fetch initial unread count
      const res = await fetch(`${API}/api/Notifications/unread-count`, {
        headers: { Authorization: `Bearer ${token}` }
      });
      const json = await res.json();
      setUnreadCount(json.data || 0);
    }

    init();

    return () => {
      Object.values(connections.current).forEach(c => c.stop());
    };
  }, [token]);

  return { notifications, unreadCount, connected };
}
```

---

## Notification Bell UI Component

```jsx
function NotificationBell({ token }) {
  const { notifications, unreadCount } = useNotifications(token);
  const [isOpen, setIsOpen] = useState(false);

  return (
    <div className="relative">
      {/* Bell Button */}
      <button onClick={() => setIsOpen(!isOpen)} className="relative">
        🔔
        {unreadCount > 0 && (
          <span className="absolute -top-1 -right-1 bg-red-500 text-white
                           rounded-full w-5 h-5 text-xs flex items-center justify-center">
            {unreadCount}
          </span>
        )}
      </button>

      {/* Dropdown */}
      {isOpen && (
        <div className="absolute right-0 mt-2 w-80 bg-white rounded-lg shadow-lg
                        border max-h-96 overflow-y-auto z-50">
          <div className="p-3 border-b font-bold">Notifications</div>
          {notifications.length === 0 ? (
            <div className="p-3 text-gray-500">No notifications</div>
          ) : (
            notifications.map(n => (
              <div key={n.id} className={`p-3 border-b cursor-pointer hover:bg-gray-50
                                          ${!n.isRead ? 'bg-blue-50' : ''}`}
                   onClick={() => {
                     markNotificationRead(n.id);
                     // navigate to entity based on n.entityType and n.entityId
                   }}>
                <div className="font-semibold text-sm">{n.title}</div>
                <div className="text-xs text-gray-600">{n.message}</div>
                <div className="text-xs text-gray-400 mt-1">
                  {new Date(n.timestamp).toLocaleString()}
                </div>
              </div>
            ))
          )}
          <div className="p-2 text-center border-t">
            <button className="text-blue-600 text-sm"
                    onClick={markAllNotificationsRead}>
              Mark all as read
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
```

---

## Flutter Complete Example

```dart
import 'package:signalr_core/signalr_core.dart';

class NotificationService {
  final String apiUrl = 'https://api.driventa.us';
  late HubConnection _notifConnection;
  late HubConnection _appConnection;
  late HubConnection _dashConnection;
  late HubConnection _chatConnection;

  Function(String title, String message)? onNotification;
  Function(Map<String, dynamic> data)? onApplicationCreated;
  Function(Map<String, dynamic> data)? onDashboardUpdate;
  Function(Map<String, dynamic> msg)? onMessage;

  Future<void> connect(String jwtToken) async {
    // Notification Hub
    _notifConnection = HubConnectionBuilder()
        .withUrl('$apiUrl/hubs/notifications',
            options: HttpConnectionOptions(
              accessTokenFactory: () async => jwtToken,
            ))
        .withAutomaticReconnect()
        .build();

    _notifConnection.on('ReceiveNotification', (List<dynamic>? args) {
      final n = args![0] as Map<String, dynamic>;
      onNotification?.call(n['title'], n['message']);
    });

    await _notifConnection.start();
    await _notifConnection.invoke('JoinPersonalGroup');

    // Applications Hub
    _appConnection = HubConnectionBuilder()
        .withUrl('$apiUrl/hubs/applications',
            options: HttpConnectionOptions(
              accessTokenFactory: () async => jwtToken,
            ))
        .withAutomaticReconnect()
        .build();

    _appConnection.on('ApplicationCreated', (List<dynamic>? args) {
      final data = args![0] as Map<String, dynamic>;
      onApplicationCreated?.call(data);
    });

    _appConnection.on('ApplicationStatusChanged', (List<dynamic>? args) {
      final data = args![0] as Map<String, dynamic>;
      print('Status changed: ${data['oldStatus']} → ${data['newStatus']}');
    });

    await _appConnection.start();
    await _appConnection.invoke('JoinAdminGroup');

    // Dashboard Hub
    _dashConnection = HubConnectionBuilder()
        .withUrl('$apiUrl/hubs/dashboard',
            options: HttpConnectionOptions(
              accessTokenFactory: () async => jwtToken,
            ))
        .withAutomaticReconnect()
        .build();

    _dashConnection.on('DashboardUpdate', (List<dynamic>? args) {
      final data = args![0] as Map<String, dynamic>;
      onDashboardUpdate?.call(data);
    });

    await _dashConnection.start();
    await _dashConnection.invoke('JoinAdminGroup');

    // Chat Hub
    _chatConnection = HubConnectionBuilder()
        .withUrl('$apiUrl/hubs/chat',
            options: HttpConnectionOptions(
              accessTokenFactory: () async => jwtToken,
            ))
        .withAutomaticReconnect()
        .build();

    _chatConnection.on('ReceiveMessage', (List<dynamic>? args) {
      final msg = args![0] as Map<String, dynamic>;
      onMessage?.call(msg);
    });

    await _chatConnection.start();
  }

  Future<void> joinConversation(String conversationId) async {
    await _chatConnection.invoke('JoinConversation', arguments: [conversationId]);
  }

  Future<void> sendMessage(String conversationId, String message) async {
    await _chatConnection.invoke('SendMessage', arguments: [conversationId, message]);
  }

  Future<void> disconnect() async {
    await _notifConnection.stop();
    await _appConnection.stop();
    await _dashConnection.stop();
    await _chatConnection.stop();
  }
}
```

---

## Common Mistakes That Break Notifications

### Mistake 1: Not Joining Groups
```javascript
// WRONG — won't receive anything
await notifConnection.start();
await appConnection.start();

// CORRECT
await notifConnection.start();
await notifConnection.invoke("JoinPersonalGroup");  // ← MISSING THIS

await appConnection.start();
await appConnection.invoke("JoinAdminGroup");        // ← MISSING THIS
```

### Mistake 2: Starting Connections After Registering Listeners
```javascript
// WRONG — may miss events
notifConnection.on("ReceiveNotification", handler);
await notifConnection.start();

// CORRECT
await notifConnection.start();                      // start first
notifConnection.on("ReceiveNotification", handler); // then listen
```

### Mistake 3: Not Handling Reconnection
```javascript
// WRONG — after reconnect, groups are lost
// No rejoin logic

// CORRECT
notifConnection.onreconnected(async () => {
  await notifConnection.invoke("JoinPersonalGroup");
});
```

### Mistake 4: Wrong Token in URL
```javascript
// WRONG — token not passed
const conn = new signalR.HubConnectionBuilder()
  .withUrl("https://api.driventa.us/hubs/notifications")
  .build();

// CORRECT — token via accessTokenFactory
const conn = new signalR.HubConnectionBuilder()
  .withUrl("https://api.driventa.us/hubs/notifications", {
    accessTokenFactory: () => JWT_TOKEN
  })
  .build();
```

### Mistake 5: Token Expired
```javascript
// JWT expires in 15 minutes. After that, SignalR will disconnect.
// You MUST refresh the token and reconnect.

notifConnection.onclose(async (error) => {
  console.log("Connection closed, refreshing token...");
  const newToken = await refreshToken();
  TOKEN = newToken;
  // Reconnect with new token
  await notifConnection.start();
  await notifConnection.invoke("JoinPersonalGroup");
});
```

---

## Debugging Checklist

If notifications aren't working, check these in order:

1. **Login succeeds?** — Check that `accessToken` is returned from `/api/Auth/login`
2. **Token stored?** — Make sure TOKEN variable holds the JWT string
3. **Connection starts?** — Check console for SignalR connection errors
4. **Groups joined?** — Add `console.log` after each `invoke("JoinPersonalGroup")`
5. **Listeners registered?** — Check `.on("ReceiveNotification", ...)` is called
6. **Token not expired?** — JWT expires in 15 min, refresh before that
7. **Test manually?** — Use Swagger to call a POST endpoint that triggers notifications
8. **Check browser DevTools:**
   - Network tab → filter `ws://` → look for WebSocket connections
   - Should see connections to `/hubs/notifications`, `/hubs/applications`, `/hubs/dashboard`
   - Messages tab → look for incoming frames

### Quick Test (Paste in Browser Console)

```javascript
// After login, paste this in browser console to test:
const token = "YOUR_JWT_TOKEN";

const conn = new signalR.HubConnectionBuilder()
  .withUrl("https://api.driventa.us/hubs/notifications", {
    accessTokenFactory: () => token
  })
  .withAutomaticReconnect()
  .build();

conn.on("ReceiveNotification", (n) => {
  console.log("GOT NOTIFICATION:", n);
  alert(`Notification: ${n.title} - ${n.message}`);
});

conn.start().then(() => {
  console.log("Connected!");
  return conn.invoke("JoinPersonalGroup");
}).then(() => {
  console.log("Joined group! Waiting for notifications...");
}).catch(err => console.error("Error:", err));
```

After running this, trigger any action (submit an application, change a load status, etc.) and you should see the alert.

---

## All Notification Payloads

### ReceiveNotification (Personal)
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "title": "New Application",
  "message": "Smith Trucking LLC (John Smith) submitted a new application.",
  "entityType": "Application",
  "entityId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "isRead": false,
  "timestamp": "2026-08-31T12:00:00+00:00"
}
```

### ApplicationCreated
```json
{
  "applicationId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "applicationNumber": "APP-260831-A1B2",
  "companyName": "Smith Trucking LLC",
  "fullName": "John Smith",
  "email": "john@smithtrucking.com",
  "phone": "+1 (555) 123-4567",
  "equipmentType": "DryVan",
  "truckCount": 5,
  "status": "New",
  "submittedAt": "2026-08-31T12:00:00+00:00"
}
```

### ApplicationStatusChanged
```json
{
  "applicationId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "applicationNumber": "APP-260831-A1B2",
  "companyName": "Smith Trucking LLC",
  "fullName": "John Smith",
  "oldStatus": "New",
  "newStatus": "Reviewing",
  "timestamp": "2026-08-31T12:05:00+00:00"
}
```

### ApplicationUpdated
```json
{
  "applicationId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "applicationNumber": "APP-260831-A1B2",
  "companyName": "Smith Trucking LLC",
  "fullName": "John Smith",
  "status": "Reviewing"
}
```

### ApplicationDeleted
```json
{
  "applicationId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "applicationNumber": "APP-260831-A1B2",
  "timestamp": "2026-08-31T12:10:00+00:00"
}
```

### DashboardUpdate (Any Entity)
```json
{
  "entityType": "Load",
  "action": "StatusChanged",
  "entity": {
    "loadId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "loadNumber": "LD-260831-A1B2",
    "carrierName": "Smith Trucking LLC",
    "oldStatus": "Booked",
    "newStatus": "PickedUp",
    "timestamp": "2026-08-31T12:05:00+00:00"
  }
}
```

### ReceiveMessage (Chat)
```json
{
  "messageId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "message": "Hello, I need help with my shipment",
  "senderUserId": null,
  "senderType": 0,
  "timestamp": "2026-08-31T12:00:00+00:00"
}
```

---

*Send this document to your UI developer. It contains everything they need.*
