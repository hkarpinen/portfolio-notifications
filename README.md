# portfolio-notifications

Real-time notification service. Sits downstream of the finance and forum services — it doesn't originate data, it reacts to it. When a bill is split or a comment is posted, those services publish a domain event over RabbitMQ. This service consumes those events, persists a notification record, and fans the update out to any connected browser clients via Server-Sent Events (SSE).

No polling. No WebSocket upgrade negotiation. SSE is a plain HTTP response that stays open — simpler to proxy through Nginx, works through most firewalls, and sufficient for one-directional server → client push.

## What it does

- **Event consumption** — MassTransit consumers listen for events published by finance (`bill.split.created`, `expense.added`) and forum (`thread.created`, `comment.posted`, `vote.cast`)
- **Persistence** — each consumed event is stored as a `Notification` row (userId, type, payload, read/unread, timestamp)
- **SSE fan-out** — authenticated clients open a long-lived GET to `/api/notifications/stream`; the dispatcher pushes new notifications down the open connection in real time
- **Read/unread state** — mark individual notifications or all notifications as read
- **Notification list** — paginated fetch of past notifications with unread count

## Stack

- .NET 8 / ASP.NET Core Web API
- PostgreSQL 17 (EF Core)
- RabbitMQ (event consumption via MassTransit)
- Server-Sent Events (SSE) for real-time push
- Clean Architecture: Application → Infrastructure → Client

## Running locally

```bash
# From repo root — requires postgres + rabbitmq (see infra/)
dotnet run --project src/Client
```

Or via the full stack:

```bash
docker compose -f infra/compose.dev.yaml up notifications
```

## Structure

```
src/
  Application/     Contracts (DTOs, event payloads), service interfaces
                   (INotificationRepository, INotificationDispatcher, INotificationPublisher)
  Infrastructure/  EF Core, notification persistence, MassTransit consumers,
                   SSE dispatcher (concurrent connection registry)
  Client/          ASP.NET Core controller, DI wiring
```

## API surface

| Endpoint | Method | Purpose |
|---|---|---|
| `/api/notifications` | `GET` | Paginated notification list + unread count |
| `/api/notifications/stream` | `GET` | SSE stream (keep-alive, auth required) |
| `/api/notifications/{id}/read` | `PUT` | Mark one notification read |
| `/api/notifications/read-all` | `PUT` | Mark all notifications read |

## Environment variables

| Variable | Description |
|---|---|
| `ConnectionStrings__Notifications` | PostgreSQL connection string |
| `Jwt__Secret` | JWT signing key (≥ 32 chars, shared with identity service) |
| `RabbitMq__Host` | RabbitMQ hostname |
| `RabbitMq__Username` | RabbitMQ username |
| `RabbitMq__Password` | RabbitMQ password |

## CI/CD

Two workflows run on push to `main`:

| Workflow | File | What it does |
|---|---|---|
| **Build & Publish** | `.github/workflows/docker-publish.yml` | Runs `dotnet test`, builds the Docker image, pushes to `ghcr.io/hkarpinen/portfolio-notifications:latest` |
| **Deploy** | `.github/workflows/deploy.yml` | Triggers after Build & Publish succeeds; SSHes into the server, pulls the new image, and restarts only the `notifications` container |

### Required GitHub Actions secrets

| Secret | Description |
|---|---|
| `DEPLOY_HOST` | VPS IP address or hostname |
| `DEPLOY_USER` | SSH user on the server |
| `DEPLOY_KEY` | Private SSH key for that user |
| `DEPLOY_PATH` | Absolute path to the infra directory on the server |

## Docs

- [Architecture & event catalogue](docs/Architecture.md)
- Use cases: [`docs/use-cases/`](docs/use-cases/)

