# Architecture — Notifications

## Overview

The notifications service is a pure consumer. It has no domain aggregates of its own — it reacts to domain events published by the `bills` and `forum` services, persists notifications to PostgreSQL, and delivers them to clients in real time over SSE.

```
bills / forum
    │  (RabbitMQ domain events)
    ▼
MassTransit Consumers
    │  persist + fan-out
    ▼
INotificationPublisher
    ├── INotificationRepository  →  PostgreSQL (persisted_notifications)
    └── INotificationDispatcher  →  in-memory channel per connected user (SSE)
```

## Persistence model

| Table | Purpose |
|---|---|
| `persisted_notifications` | One row per notification per recipient; stores type, JSON payload, read flag |
| `household_member_projections` | Read-model of active household memberships, built from bills events, used to fan-out bill notifications to all members |
| `thread_author_projections` | Maps `ThreadId → AuthorId + CommunitySlug`; built from `forum.thread.created`, used to notify thread authors of new comments |
| `comment_author_projections` | Maps `CommentId → AuthorId`; built from `forum.comment.created`, used to notify comment authors of replies |
| `processed_events` | Idempotency table — deduplicates MassTransit message redeliveries by `MessageId` |

## Service interfaces

| Interface | Implementation | Registration |
|---|---|---|
| `INotificationRepository` | `NotificationRepository` | Scoped |
| `INotificationDispatcher` | `PersistingNotificationDispatcher` (wraps `InMemoryNotificationDispatcher`) | Singleton |
| `INotificationPublisher` | `NotificationPublisher` | Scoped |

## Real-time delivery (SSE)

`GET /api/notifications/stream` holds a long-lived HTTP response with `Content-Type: text/event-stream`. On connect it replays all unread persisted notifications (so clients recover state after a page reload), then streams live events as they arrive. A heartbeat comment (`: heartbeat`) is sent every 20 seconds to keep the connection alive through proxies.

## Idempotency

Every consumer checks `processed_events` before acting. If a row with the same `MessageId` already exists the message is silently dropped. The unique constraint on `(event_id)` means a duplicate `INSERT` raises a `UniqueViolation` which is caught and swallowed, making all consumers safe to retry.

## Event catalogue

### Bills events consumed

| Event | Consumer action |
|---|---|
| `BillsHouseholdCreatedEvent` | Seed `household_member_projections` with the owner |
| `BillsHouseholdMemberJoinedEvent` | Upsert member as active in projection |
| `BillsHouseholdMemberLeftEvent` | Mark member inactive in projection |
| `BillsHouseholdMemberRemovedEvent` | Mark member inactive; notify removed user (`bills.member.removed`) |
| `BillsHouseholdMemberRoleChangedEvent` | Notify affected user (`bills.member.role_changed`) |
| `BillsHouseholdOwnershipTransferredEvent` | Notify new owner (`bills.household.ownership_transferred`) |
| `BillsBillCreatedEvent` | Fan-out `bills.bill.created` to all active household members except the creator |
| `BillsBillSplitCreatedEvent` | Notify assigned member (`bills.split.created`) |

### Forum events consumed

| Event | Consumer action |
|---|---|
| `ForumThreadCreatedEvent` | Upsert `thread_author_projections` |
| `ForumCommentCreatedEvent` | Upsert `comment_author_projections`; notify thread author (`forum.comment.created`); notify parent comment author if a reply (`forum.comment.reply`) — skips self-posts and deduplicates thread/comment author |
| `ForumMembershipInvitedEvent` | Notify invitee (`forum.membership.invited`) |
| `ForumModeratorAppointedEvent` | Notify user (`forum.moderator.appointed`) |
| `ForumModeratorRemovedEvent` | Notify user (`forum.moderator.removed`) |
| `ForumUserBannedEvent` | Notify banned user (`forum.user.banned`) |
| `ForumUserUnbannedEvent` | Notify unbanned user (`forum.user.unbanned`) |
| `ForumThreadLockedEvent` | Notify thread author (`forum.thread.locked`) |
| `ForumCommunityOwnershipTransferredEvent` | Notify new owner (`forum.community.ownership_transferred`) |
