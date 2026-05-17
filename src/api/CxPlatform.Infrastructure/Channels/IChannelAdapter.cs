using CxPlatform.Domain.Enums;

namespace CxPlatform.Infrastructure.Channels;

// Channel adapter contract — Email / WhatsApp / Chat all share the same shape
// so a real SMTP / WhatsApp Business / web-chat implementation can drop in
// via DI without touching the use case.
public interface IChannelAdapter
{
    InboxChannel Channel { get; }
    Task<SendResult> SendAsync(long threadId, SendPayload payload, CancellationToken ct = default);
}

public record SendPayload(string Body, string? Subject = null);

public record SendResult(bool Ok, string? ExternalId = null, string? Error = null);

// Resolver — Program.cs registers concrete adapters keyed by channel.
public interface IChannelAdapterRegistry
{
    IChannelAdapter Get(InboxChannel channel);
}

public class ChannelAdapterRegistry : IChannelAdapterRegistry
{
    private readonly IReadOnlyDictionary<InboxChannel, IChannelAdapter> _map;
    public ChannelAdapterRegistry(IEnumerable<IChannelAdapter> adapters)
        => _map = adapters.ToDictionary(a => a.Channel);
    public IChannelAdapter Get(InboxChannel channel) => _map[channel];
}
