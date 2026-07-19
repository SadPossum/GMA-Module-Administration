namespace Gma.Modules.Administration.Application.Validation;

using Gma.Framework.Cqrs;
using Gma.Modules.Administration.Application.Commands;

internal sealed class PurgeAdministrationAuditEntriesCommandValidator
    : ICommandValidator<PurgeAdministrationAuditEntriesCommand>
{
    public IEnumerable<string> Validate(PurgeAdministrationAuditEntriesCommand command)
    {
        if (command.BatchSize is <= 0)
        {
            yield return "Administration audit purge batch size must be positive when supplied.";
        }
    }
}
