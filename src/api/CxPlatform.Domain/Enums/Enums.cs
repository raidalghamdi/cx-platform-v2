namespace CxPlatform.Domain.Enums;

public enum ComplaintStatus
{
    New = 0,
    InProgress = 1,
    Resolved = 2,
    Closed = 3,
}

public enum Priority
{
    Low = 0,
    Normal = 1,
    High = 2,
}

public enum InboxChannel
{
    Email = 0,
    WhatsApp = 1,
    Chat = 2,
}

public enum InboxStatus
{
    New = 0,
    Open = 1,
    Replied = 2,
    Closed = 3,
}
