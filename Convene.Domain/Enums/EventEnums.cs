namespace Convene.Domain.Enums
{
    public enum EventStatus
    {
        Draft = 0,
        Published = 1,
        Cancelled = 2
    }

    public enum PricingRuleType
    {
        None = 0,
        EarlyBird = 1,   // discount before a given date range
        LastMinute = 2,  // discount in the last N days before event
        DemandBased = 3  // price increase after X% sold
    }
}
