namespace Gma.Modules.Administration.Application;

public sealed class AdministrationAuditOptions
{
    public const string SectionName = "Administration:Audit";
    public const int HardMaxPageSize = 500;
    public const int HardMaxPurgeBatchSize = 5000;
    public const string InvalidConfigurationMessage =
        "Administration audit page and purge batch sizes must be positive, ordered, and within their hard limits.";

    public int DefaultPageSize { get; set; } = 50;
    public int MaxPageSize { get; set; } = 200;
    public int DefaultPurgeBatchSize { get; set; } = 500;
    public int MaxPurgeBatchSize { get; set; } = 2000;

    public static bool IsValid(AdministrationAuditOptions options) =>
        options.DefaultPageSize > 0 &&
        options.MaxPageSize >= options.DefaultPageSize &&
        options.MaxPageSize <= HardMaxPageSize &&
        options.DefaultPurgeBatchSize > 0 &&
        options.MaxPurgeBatchSize >= options.DefaultPurgeBatchSize &&
        options.MaxPurgeBatchSize <= HardMaxPurgeBatchSize;
}
