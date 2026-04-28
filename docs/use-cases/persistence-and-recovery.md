# Use Case: Notification Persistence & Recovery

## Goal
Notifications survive SSE disconnects. When a user reconnects (page reload, network drop), they see all unread notifications they missed while offline.

## Flow

1. `INotificationPublisher.PublishAsync` always writes to `persisted_notifications` **before** calling the dispatcher.
2. If no SSE connection is open for the recipient, the row sits in the DB until the next connect.
3. On next SSE connect, `GET /api/notifications/stream` queries `INotificationRepository.GetRecentAsync` for the 50 most recent notifications, filters to unread ones, and replays them in chronological order before subscribing to the live channel.
4. The client can also call `GET /api/notifications` at any time for the full recent list (read and unread).

## Idempotency

MassTransit may redeliver messages. Each consumer checks `processed_events` by `MessageId` before acting. A unique constraint on `event_id` means a concurrent duplicate is silently dropped.
