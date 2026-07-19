namespace Gma.Modules.Administration.Application.Models;

public sealed record AdministrationAuditPage(
    IReadOnlyList<AdministrationAuditEntryDetails> Items,
    string? NextCursor,
    bool HasMore,
    int Limit);
