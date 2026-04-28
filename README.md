# portfolio-notifications

Real-time notification service. Consumes domain events from the bills and forum services over RabbitMQ, persists them, and fans them out to connected clients over Server-Sent Events (SSE).

## Stack

- .NET 8 / ASP.NET Core Web API
- PostgreSQL 17 (EF Core — notifications + projections)
- RabbitMQ (event consumption via MassTransit)
- Server-Sent Events for real-time push to clients
- Clean Architecture: Application → Infrastructure → Client

## Running locally

```bash
dotnet run --project src/Client
```

Or via the full stack in `portfolio-infra`:

```bash
docker compose up notifications
```

## Structure

```
src/
  Application/     Contracts (DTOs), service interfaces (INotificationRepository, INotificationDispatcher, INotificationPublisher)
  Infrastructure/  EF Core, persistence, consumers, SSE dispatcher, repository
  Client/          ASP.NET Core controller, DI wiring
```

## Docs

- [Architecture & event catalogue](docs/Architecture.md)
- Use cases: [`docs/use-cases/`](docs/use-cases/)

## Environment variables

| Variable | Description |
|---|---|
| `ConnectionStrings__Notifications` | PostgreSQL connection string |
| `Jwt__Secret` | JWT signing key (≥ 32 chars, shared with identity service) |
| `RabbitMq__Host` | RabbitMQ hostname |
| `RabbitMq__Username` | RabbitMQ username |
| `RabbitMq__Password` | RabbitMQ password |
