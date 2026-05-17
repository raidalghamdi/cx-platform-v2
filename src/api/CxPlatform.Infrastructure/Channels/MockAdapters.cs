using CxPlatform.Domain.Enums;

namespace CxPlatform.Infrastructure.Channels;

// Phase 0 mock adapters — simulate 600-1200ms latency + 95% success.
// Production: replace with real SMTP / WhatsApp Business / web-socket clients.

internal static class Sim
{
    private static readonly Random Rng = new();

    public static async Task<SendResult> Roll(InboxChannel channel, long threadId, CancellationToken ct)
    {
        int delay;
        bool ok;
        lock (Rng)
        {
            delay = Rng.Next(600, 1201);
            ok = Rng.NextDouble() < 0.95;
        }
        await Task.Delay(delay, ct);
        if (!ok)
        {
            var reason = channel switch
            {
                InboxChannel.Email    => "SMTP relay rejected (temporary)",
                InboxChannel.WhatsApp => "WhatsApp Business API returned 503",
                _                     => "Chat socket disconnected before ack",
            };
            return new SendResult(false, Error: reason);
        }
        var ext = $"{channel.ToString().ToLowerInvariant()}_{threadId}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds():x}";
        return new SendResult(true, ExternalId: ext);
    }
}

public class EmailAdapter : IChannelAdapter
{
    public InboxChannel Channel => InboxChannel.Email;
    public Task<SendResult> SendAsync(long threadId, SendPayload payload, CancellationToken ct = default)
        => Sim.Roll(Channel, threadId, ct);
}

public class WhatsAppAdapter : IChannelAdapter
{
    public InboxChannel Channel => InboxChannel.WhatsApp;
    public async Task<SendResult> SendAsync(long threadId, SendPayload payload, CancellationToken ct = default)
    {
        // WhatsApp Business caps messages at 4096 characters — fail fast.
        if (payload.Body.Length > 4096)
            return new SendResult(false, Error: "Message exceeds 4096 characters");
        return await Sim.Roll(Channel, threadId, ct);
    }
}

public class ChatAdapter : IChannelAdapter
{
    public InboxChannel Channel => InboxChannel.Chat;
    public Task<SendResult> SendAsync(long threadId, SendPayload payload, CancellationToken ct = default)
        => Sim.Roll(Channel, threadId, ct);
}
