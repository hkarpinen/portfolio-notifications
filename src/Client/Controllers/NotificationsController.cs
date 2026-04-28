using System.Text.Json;
using Client.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Notifications.Application.Contracts;
using Notifications.Application.Services;

namespace Client.Controllers;

[ApiController]
[Route("api/notifications")]
[EnableRateLimiting("api")]
public sealed class NotificationsController : ControllerBase
{
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> List(
        [FromServices] INotificationRepository repo,
        CancellationToken cancellationToken)
    {
        var userId = User.GetRequiredUserId();
        var items = await repo.GetRecentAsync(userId, limit: 50, cancellationToken);
        return Ok(new NotificationStreamDto(items));
    }

    [HttpPut("{eventId:guid}/read")]
    [Authorize]
    public async Task<IActionResult> MarkRead(
        [FromRoute] Guid eventId,
        [FromServices] INotificationRepository repo,
        CancellationToken cancellationToken)
    {
        var userId = User.GetRequiredUserId();
        await repo.MarkReadAsync(eventId, userId, cancellationToken);
        return NoContent();
    }

    [HttpPut("read-all")]
    [Authorize]
    public async Task<IActionResult> MarkAllRead(
        [FromServices] INotificationRepository repo,
        CancellationToken cancellationToken)
    {
        var userId = User.GetRequiredUserId();
        await repo.MarkAllReadAsync(userId, cancellationToken);
        return NoContent();
    }

    [HttpGet("stream")]
    [Authorize]
    [EnableRateLimiting("notification-stream")]
    public async Task Stream(
        [FromServices] INotificationDispatcher dispatcher,
        [FromServices] INotificationRepository repo,
        CancellationToken cancellationToken)
    {
        var userId = User.GetRequiredUserId();

        HttpContext.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();
        Response.Headers.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";
        Response.Headers.Append("X-Accel-Buffering", "no");

        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var writeLock = new SemaphoreSlim(1, 1);

        async Task WriteEventAsync(NotificationStreamEventDto dto, CancellationToken ct)
        {
            var payload = JsonSerializer.Serialize(dto, jsonOptions);
            await writeLock.WaitAsync(ct);
            try
            {
                await Response.WriteAsync("event: notification\n", ct);
                await Response.WriteAsync($"data: {payload}\n\n", ct);
                await Response.Body.FlushAsync(ct);
            }
            finally
            {
                writeLock.Release();
            }
        }

        async Task WriteHeartbeatAsync(CancellationToken ct)
        {
            await writeLock.WaitAsync(ct);
            try
            {
                await Response.WriteAsync(": heartbeat\n\n", ct);
                await Response.Body.FlushAsync(ct);
            }
            finally
            {
                writeLock.Release();
            }
        }

        // Replay unread persisted notifications so the client recovers its bell
        // state after a page reload or reconnect gap (notifications dispatched
        // while the SSE was down are already in the DB).
        var history = await repo.GetRecentAsync(userId, limit: 50, cancellationToken);
        foreach (var n in history.Where(n => !n.IsRead).Reverse())
            await WriteEventAsync(n, cancellationToken);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        async Task SendHeartbeatsAsync()
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(20));
            try
            {
                while (await timer.WaitForNextTickAsync(cts.Token))
                    await WriteHeartbeatAsync(cts.Token);
            }
            catch (OperationCanceledException) { }
        }

        var heartbeat = SendHeartbeatsAsync();

        try
        {
            await foreach (var notification in dispatcher.SubscribeAsync(userId, cts.Token))
                await WriteEventAsync(notification, cts.Token);
        }
        catch (OperationCanceledException) { }
        finally
        {
            cts.Cancel();
            await heartbeat;
        }
    }
}
