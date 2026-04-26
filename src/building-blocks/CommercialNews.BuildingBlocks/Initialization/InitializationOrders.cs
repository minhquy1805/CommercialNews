namespace CommercialNews.BuildingBlocks.Initialization;

/// <summary>
/// Suggested initialization order values for future unified startup orchestration.
/// 
/// These values are intentionally lightweight and can evolve as more modules
/// introduce initialization steps.
/// 
/// Current note:
/// - This file is added in advance for consistency.
/// - It does not force immediate adoption.
/// </summary>
public static class InitializationOrders
{
    public const int Identity = 100;
    public const int Authorization = 200;
    public const int Content = 300;
    public const int Seo = 400;
    public const int Media = 500;
    public const int Notifications = 600;
    public const int Audit = 700;
}