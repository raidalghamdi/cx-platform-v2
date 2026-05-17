namespace CxPlatform.Application.Services;

// Thrown by IPdcaTransitionService when the requested stage change is not
// allowed (illegal jump, item not found, etc.). Carries EN+AR messages so
// the controller can surface bilingual ProblemDetails without re-translating.
public sealed class PdcaTransitionException : Exception
{
    public string MessageEn { get; }
    public string MessageAr { get; }

    public PdcaTransitionException(string messageEn, string messageAr)
        : base(messageEn)
    {
        MessageEn = messageEn;
        MessageAr = messageAr;
    }
}
