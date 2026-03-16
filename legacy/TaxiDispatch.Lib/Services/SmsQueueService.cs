using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System.Text;
using TaxiDispatch.Configuration;
using TaxiDispatch.Data;
using TaxiDispatch.DTOs.Booking;

namespace TaxiDispatch.Services;

public sealed class SmsQueueService
{
    private readonly TaxiDispatchContext _db;
    private readonly AceMessagingService _messaging;
    private readonly UserActionsService _actions;
    private readonly UserManager<AppUser> _userManager;
    private readonly SmsQueueConfig _config;
    private readonly ILogger<SmsQueueService> _logger;

    public SmsQueueService(
        TaxiDispatchContext db,
        AceMessagingService messaging,
        UserActionsService actions,
        UserManager<AppUser> userManager,
        IOptions<SmsQueueConfig> options,
        ILogger<SmsQueueService> logger)
    {
        _db = db;
        _messaging = messaging;
        _actions = actions;
        _userManager = userManager;
        _logger = logger;
        _config = options.Value;
    }

    public async Task<RabbitMessagePacket?> GetNextMessageAsync(CancellationToken cancellationToken = default)
    {
        await UpdateHeartbeatAsync(cancellationToken);

        var factory = CreateFactory();

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.ExchangeDeclare(_config.Exchange, ExchangeType.Direct);
        channel.QueueDeclare(_config.QueueName, false, false, false);
        channel.QueueBind(_config.QueueName, _config.Exchange, _config.RoutingKey, null);
        channel.BasicQos(0, 1, false);

        var messageEvent = channel.BasicGet(_config.QueueName, true);

        if (messageEvent == null)
        {
            return null;
        }

        var payload = Encoding.UTF8.GetString(messageEvent.Body.ToArray());
        return JsonConvert.DeserializeObject<RabbitMessagePacket>(payload);
    }

    public async Task SendTextMessageAsync(string message, string telephone, string? username, CancellationToken cancellationToken = default)
    {
        await _messaging.SendSmsAsync(message, telephone);

        var fullName = string.Empty;

        if (!string.IsNullOrWhiteSpace(username))
        {
            var user = await _userManager.FindByNameAsync(username);

            if (user != null)
            {
                fullName = user.FullName;
            }
        }

        await _actions.LogSendText(telephone, fullName);
    }

    private ConnectionFactory CreateFactory()
    {
        if (string.IsNullOrWhiteSpace(_config.Username) ||
            string.IsNullOrWhiteSpace(_config.Password) ||
            string.IsNullOrWhiteSpace(_config.Host) ||
            string.IsNullOrWhiteSpace(_config.QueueName) ||
            string.IsNullOrWhiteSpace(_config.RoutingKey))
        {
            throw new InvalidOperationException("SmsQueue configuration is missing.");
        }

        return new ConnectionFactory
        {
            Uri = new Uri($"amqp://{_config.Username}:{_config.Password}@{_config.Host}:{_config.Port}"),
            ClientProvidedName = _config.ClientProvidedName
        };
    }

    private async Task UpdateHeartbeatAsync(CancellationToken cancellationToken)
    {
        try
        {
            var time = DateTime.Now.ToUKTime();
            await _db.MessagingNotifyConfig
                .ExecuteUpdateAsync(o => o.SetProperty(u => u.SmsPhoneHeartBeat, time), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not update SmsPhoneHeartBeat timestamp.");
        }
    }
}
