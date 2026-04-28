# Use Case: Real-Time Notification Delivery

## Goal
A logged-in user receives notifications immediately as relevant events occur in other services, without polling.

## Flow

1. Client opens `GET /api/notifications/stream` (SSE connection).
2. Service replays all unread persisted notifications so the client bell count is accurate from the moment of connect.
3. Service subscribes the user to the `INotificationDispatcher` in-memory channel.
4. When a domain event arrives over RabbitMQ (e.g. someone comments on the user's thread), the consumer calls `INotificationPublisher.PublishAsync`.
5. `NotificationPublisher` persists the notification to `persisted_notifications` then calls `INotificationDispatcher.Dispatch`.
6. The dispatcher pushes the JSON payload over the open SSE connection.
7. A `: heartbeat` comment is sent every 20 seconds; the client reconnects automatically if the connection drops.

## Endpoints

| Method | Route | Description |
|---|---|---|
| `GET` | `/api/notifications/stream` | Open SSE stream; replays unread on connect then pushes live |
| `GET` | `/api/notifications` | Fetch up to 50 most recent notifications (REST fallback) |
| `PUT` | `/api/notifications/{eventId}/read` | Mark a single notification read |
| `PUT` | `/api/notifications/read-all` | Mark all notifications read |
