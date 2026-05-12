using Infrastructure.Managers;
using Infrastructure.Messaging.Consumers;
using Infrastructure.Notifications;
using Infrastructure.Persistence;
using Infrastructure.Queries;
using Infrastructure.Repositories;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notifications.Application.Managers;
using Notifications.Application.Queries;
using Notifications.Application.Repositories;
using Notifications.Application.Services;

namespace Infrastructure;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<NotificationsDbContext>(options =>
            options.UseNpgsql(
                    configuration.GetConnectionString("Notifications"),
                    npgsql => npgsql.MigrationsAssembly("Infrastructure"))
                .UseSnakeCaseNamingConvention());

        var rabbitConfig = configuration.GetSection("RabbitMq");
        services.AddMassTransit(x =>
        {
            x.SetKebabCaseEndpointNameFormatter();

            // Forum consumers
            x.AddConsumer<ForumThreadCreatedConsumer>();
            x.AddConsumer<ForumCommentCreatedConsumer>();
            x.AddConsumer<ForumMembershipInvitedConsumer>();
            x.AddConsumer<ForumModeratorAppointedConsumer>();
            x.AddConsumer<ForumModeratorRemovedConsumer>();
            x.AddConsumer<ForumUserBannedConsumer>();
            x.AddConsumer<ForumUserUnbannedConsumer>();
            x.AddConsumer<ForumThreadLockedConsumer>();
            x.AddConsumer<ForumCommunityOwnershipTransferredConsumer>();

            // Finance consumers
            x.AddConsumer<FinanceHouseholdCreatedConsumer>();
            x.AddConsumer<FinanceHouseholdMemberJoinedConsumer>();
            x.AddConsumer<FinanceHouseholdMemberLeftConsumer>();
            x.AddConsumer<FinanceHouseholdMemberRemovedConsumer>();
            x.AddConsumer<FinanceHouseholdMemberRoleChangedConsumer>();
            x.AddConsumer<FinanceHouseholdOwnershipTransferredConsumer>();
            x.AddConsumer<FinanceExpenseCreatedConsumer>();
            x.AddConsumer<FinanceExpenseSplitCreatedConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbitConfig["Host"] ?? "localhost", h =>
                {
                    var username = rabbitConfig["Username"];
                    var password = rabbitConfig["Password"];
                    if (!string.IsNullOrWhiteSpace(username)) h.Username(username);
                    if (!string.IsNullOrWhiteSpace(password)) h.Password(password);
                });

                // Retry with exponential backoff so consumers that depend on
                // projections built by earlier events (e.g. CommentCreated needs
                // the ThreadAuthor projection from ThreadCreated) get a second
                // chance if consumed out of order.
                cfg.UseMessageRetry(r => r.Exponential(
                    retryLimit: 5,
                    minInterval: TimeSpan.FromSeconds(1),
                    maxInterval: TimeSpan.FromSeconds(30),
                    intervalDelta: TimeSpan.FromSeconds(2)));

                cfg.ConfigureEndpoints(context);
            });
        });

        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<INotificationQuery, NotificationQuery>();
        services.AddScoped<INotificationManager, NotificationManager>();
        services.AddScoped<INotificationPublisher, NotificationPublisher>();
        services.AddSingleton<INotificationDispatcher, InMemoryNotificationDispatcher>();

        return services;
    }
}
