# Flutter Dashboard Notification Guide

This guide is for the Flutter dashboard app to integrate with the Driventa backend notifications system correctly and in real time.

## 1) Backend contract to follow

The backend is already using:

- JWT-authenticated dashboard requests
- SignalR hub at `/hubs/notifications`
- personal user group: `user_<guid>`
- event name: `ReceiveNotification`
- REST endpoints:
  - `GET /api/Notifications`
  - `GET /api/Notifications/unread-count`
  - `PATCH /api/Notifications/{id}/read`
  - `POST /api/Notifications/read-all`

This is the contract your Flutter app must follow exactly.

---

## 2) Required dependencies

Add these packages in `pubspec.yaml`:

```yaml
dependencies:
  flutter:
    sdk: flutter

  dio: ^5.6.0
  flutter_secure_storage: ^9.2.2
  signalr_netcore: ^1.3.7
  provider: ^6.1.2
  intl: ^0.19.0
```

If you already use another state manager, keep it. The important part is the notification service and a SignalR connection lifecycle.

---

## 3) Auth flow for dashboard

### 3.1 Login call

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
    "accessToken": "...",
    "refreshToken": "...",
    "expiresAt": "2026-08-29T12:00:00Z",
    "userProfile": {
      "id": "<guid>",
      "firstName": "Super",
      "lastName": "Admin",
      "email": "admin@driventa.com",
      "role": "SuperAdmin"
    }
  },
  "errors": null
}
```

### 3.2 Store tokens securely

Use `flutter_secure_storage`:

```dart
final storage = FlutterSecureStorage();

await storage.write(key: 'accessToken', value: token);
await storage.write(key: 'refreshToken', value: refreshToken);
```

### 3.3 Add token to all protected API calls

```dart
final token = await storage.read(key: 'accessToken');

final dio = Dio(
  BaseOptions(
    baseUrl: 'http://localhost:5165',
    headers: {
      'Content-Type': 'application/json',
      if (token != null) 'Authorization': 'Bearer $token',
    },
  ),
);
```

---

## 4) Notification model

Use this model in Flutter:

```dart
class AppNotification {
  final String id;
  final String userId;
  final String title;
  final String message;
  final String? entityType;
  final String? entityId;
  final bool isRead;
  final DateTime timestamp;

  AppNotification({
    required this.id,
    required this.userId,
    required this.title,
    required this.message,
    this.entityType,
    this.entityId,
    required this.isRead,
    required this.timestamp,
  });

  factory AppNotification.fromJson(Map<String, dynamic> json) {
    return AppNotification(
      id: json['id'] ?? '',
      userId: json['userId'] ?? '',
      title: json['title'] ?? '',
      message: json['message'] ?? '',
      entityType: json['entityType'],
      entityId: json['entityId']?.toString(),
      isRead: json['isRead'] ?? false,
      timestamp: DateTime.tryParse(json['createdAt'] ?? '') ?? DateTime.now(),
    );
  }
}
```

For realtime payload, use the same fields, but the timestamp key is `timestamp` instead of `createdAt`.

---

## 5) REST API service for notifications

### 5.1 Get all notifications

```dart
Future<List<AppNotification>> getNotifications({
  int page = 1,
  int pageSize = 20,
  bool? isRead,
}) async {
  final response = await dio.get(
    '/api/Notifications',
    queryParameters: {
      'page': page,
      'pageSize': pageSize,
      if (isRead != null) 'isRead': isRead,
    },
  );

  final result = response.data;
  final items = result['data']['items'] as List;
  return items
      .map((e) => AppNotification.fromJson(e))
      .toList();
}
```

### 5.2 Get unread count

```dart
Future<int> getUnreadCount() async {
  final response = await dio.get('/api/Notifications/unread-count');
  return response.data['data'] ?? 0;
}
```

### 5.3 Mark one as read

```dart
Future<void> markAsRead(String id) async {
  await dio.patch('/api/Notifications/$id/read');
}
```

### 5.4 Mark all as read

```dart
Future<void> markAllAsRead() async {
  await dio.post('/api/Notifications/read-all');
}
```

---

## 6) SignalR notification connection

### 6.1 Connect to the notification hub

Use `signalr_netcore` or any equivalent SignalR client library.

```dart
import 'package:signalr_netcore/hub_connection.dart';

class NotificationHubService {
  final String baseUrl;
  final String token;
  late final HubConnection _hubConnection;

  NotificationHubService({
    required this.baseUrl,
    required this.token,
  });

  Future<void> connect() async {
    _hubConnection = HubConnectionBuilder()
        .withUrl(
          '$baseUrl/hubs/notifications',
          options: HttpConnectionOptions(
            accessTokenFactory: () async => token,
          ),
        )
        .withAutomaticReconnect()
        .build();

    await _hubConnection.start();
    await _hubConnection.invoke('JoinPersonalGroup');
  }

  void listenToNotifications(Function(AppNotification) onNewNotification) {
    _hubConnection.on('ReceiveNotification', (arguments) {
      if (arguments == null || arguments.isEmpty) return;

      final payload = arguments.first as Map<String, dynamic>;

      final notification = AppNotification(
        id: payload['id']?.toString() ?? '',
        userId: payload['userId']?.toString() ?? '',
        title: payload['title'] ?? '',
        message: payload['message'] ?? '',
        entityType: payload['entityType']?.toString(),
        entityId: payload['entityId']?.toString(),
        isRead: payload['isRead'] ?? false,
        timestamp: DateTime.tryParse(payload['timestamp'] ?? '') ?? DateTime.now(),
      );

      onNewNotification(notification);
    });
  }

  Future<void> stop() async {
    if (_hubConnection.state != HubConnectionState.disconnected) {
      await _hubConnection.stop();
    }
  }
}
```

### 6.2 Important note

The backend expects the client to call:

```dart
await _hubConnection.invoke('JoinPersonalGroup');
```

Without this, the connection is not added to the user-specific group and notifications will not arrive.

---

## 7) Notification state management pattern

Use a state manager to keep the app consistent.

### Recommended flow

1. App starts
2. User logs in
3. Connect notification hub
4. Join personal group
5. Fetch unread notification count from REST
6. Fetch notifications list
7. Listen for `ReceiveNotification`
8. When event arrives:
   - insert into list
   - increment unread badge count
   - show toast/snackbar
9. When user opens notification list:
   - call mark-as-read APIs

---

## 8) Real-time update flow in Flutter

This should be your logic:

```dart
Future<void> initializeNotifications() async {
  final token = await storage.read(key: 'accessToken');
  if (token == null) return;

  final hub = NotificationHubService(
    baseUrl: 'http://localhost:5165',
    token: token,
  );

  await hub.connect();

  hub.listenToNotifications((notification) {
    // add to local list
    notifications.insert(0, notification);

    // update unread badge count
    unreadCount.value += notification.isRead ? 0 : 1;

    // optional toast
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(content: Text(notification.title)),
    );
  });
}
```

Important: when a notification is received, do not blindly assume it is unread; the backend sends `isRead = false` for new messages, so you can increment badge count only when it is not already read.

---

## 9) Recommended UI behavior

### Badge count
Use:

```http
GET /api/Notifications/unread-count
```

and refresh it on:
- app startup
- notification received
- notification read
- all notifications marked read

### Notification list page
Use:

```http
GET /api/Notifications?page=1&pageSize=20
```

and show:
- title
- message
- timestamp
- read/unread state

### Swipe or tap to mark read
When a user opens or taps a notification:

```dart
await dio.patch('/api/Notifications/$id/read');
```

Then update the local list to `isRead = true`.

---

## 10) Best practices

1. Always use the backend JWT token for the notification hub.
2. Always join `JoinPersonalGroup` after connect.
3. Use REST for history and unread count.
4. Use SignalR only for instant push updates.
5. Refresh unread count after every new event.
6. Never trust only local state; the API is the source of truth.
7. Handle reconnects gracefully with automatic reconnect.
8. Use a singleton service for the notification hub across the app.

---

## 11) Recommended app architecture

Use a single service layer like:

- `AuthService`
- `NotificationApiService`
- `NotificationHubService`
- `NotificationController` / `Provider` / `Notifier`

This keeps notification logic clean and prevents duplicate listeners.

---

## 12) Final practical checklist

### Flutter dashboard checklist

- [ ] Login and store JWT token securely
- [ ] Connect to `/hubs/notifications` after login
- [ ] Call `JoinPersonalGroup()`
- [ ] Listen for `ReceiveNotification`
- [ ] Update local notification list in real time
- [ ] Refresh unread count after each new notification
- [ ] Fetch notification history via REST
- [ ] Mark notifications as read via REST
- [ ] Handle reconnect and token refresh properly

---

## 13) Final note

The backend notification system is designed for this exact architecture:

- REST = history + unread count + read actions
- SignalR = real-time delivery
- user-specific group = correct user only

So your Flutter dashboard should not try to invent its own notification rules. It should simply:

- auth
- connect to `/hubs/notifications`
- join personal group
- listen to `ReceiveNotification`
- sync unread state via REST

That gives you the clean, real-time notification behavior the backend expects.
