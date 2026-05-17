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

// ── Round 5: maturity-model enums ──────────────────────────────────────────

public enum WcagLevel { AA = 0, AAA = 1 }

public enum AccessibilitySeverity { Low = 0, Medium = 1, High = 2, Critical = 3 }

public enum AccessibilityItemStatus { Open = 0, InProgress = 1, Resolved = 2, Deferred = 3 }

public enum IncidentSeverity { Sev1 = 0, Sev2 = 1, Sev3 = 2, Sev4 = 3 }

public enum IncidentStatus { Open = 0, Mitigating = 1, Resolved = 2 }

public enum CheckStatus { Pass = 0, Fail = 1 }

public enum ThresholdComparison { LessThan = 0, GreaterThan = 1 }

public enum ThresholdBreachAction { CreateImprovementItem = 0, NotifyOnly = 1, Both = 2 }

public enum ImprovementSource { KpiBreach = 0, AccessibilityAudit = 1, ContentReview = 2, Manual = 3 }

public enum ImprovementPriority { Low = 0, Medium = 1, High = 2, Critical = 3 }

public enum PdcaStage { Plan = 0, Do = 1, Check = 2, Act = 3, Closed = 4 }

public enum ContentReviewStatus { Pending = 0, InReview = 1, Approved = 2, Rejected = 3 }
