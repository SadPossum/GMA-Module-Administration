# Administration Module

Current development task: [Administration domain completion](administration-domain-completion-task.md).

Completed foundation: [Administration production hardening](administration-production-hardening-task.md).

The Administration module is optional. It owns persisted admin audit, bounded audit discovery, and explicit retention operations without also owning persisted RBAC.

It does not own roles, permission grants, subject assignments, or feature-specific administration behavior. Persisted RBAC lives in `Gma.Modules.AccessControl`. Feature modules expose their own `.AdminCli` and `.AdminApi` front doors and declare their own permission codes.

## Projects

```text
Gma.Modules.Administration.Contracts
Gma.Modules.Administration.Admin.Contracts
Gma.Modules.Administration.Application
Gma.Modules.Administration.Persistence
Gma.Modules.Administration.AdminCli
Gma.Modules.Administration.AdminApi
```

## Public Contracts

`Gma.Modules.Administration.Contracts` contains Administration module metadata and audit permission codes. Shared administration execution and audit-write contracts live in `Gma.Framework.Administration`.

`Gma.Modules.Administration.Admin.Contracts` contains typed audit permissions and stable operation names. Query models, cursors, and persistence ports remain inside Application because the supported external surfaces are the Administration API and CLI.

## CLI Commands

The Administration CLI maps:

```text
administration audit list
administration audit purge --before <utc> --yes
```

`list` supports exact tenant, recorded actor, operation, permission, result, error-code, UTC range, cursor, and limit filters. Result values are `succeeded`, `denied`, `failed`, and `canceled`. The tenant value is a data filter only; audit discovery still requires a global permission. JSON output includes `nextCursor`; table output prints it separately when another page exists.

`purge` removes one bounded batch older than an explicit cutoff. It requires `--yes`; repeat it only while the response reports more eligible records.

Hosts that want persisted role management compose `Gma.Modules.AccessControl.AdminCli`, which owns the compatibility `admin bootstrap` and `admin roles ...` commands.

## Admin API

The Administration API maps:

```text
GET  /api/admin/audit
POST /api/admin/audit/purge
```

Reads are newest-first and use an opaque `(CreatedAtUtc, Id)` keyset cursor. They do not count the table. Purge requires an explicit UTC cutoff and `confirmed: true`, and never deletes more than one configured batch.

Hosts that want persisted role management compose `Gma.Modules.AccessControl.AdminApi`, which owns the compatibility `/api/admin/roles` routes. Bootstrap remains CLI-only.

## Permissions

Administration declares global audit permissions:

| Permission | Purpose |
| --- | --- |
| `administration.audit.read` | Read administrative audit records. |
| `administration.audit.purge` | Purge one confirmed, bounded batch using an explicit cutoff. |

AccessControl separately declares compatibility bootstrap and role-management permissions.

## Application Layer

Application code registers the generic admin runner, validates audit filters and cursors, and owns bounded query and retention use cases. RBAC use cases and persistence ports live in `Gma.Modules.AccessControl.Application`.

Authorization itself uses the shared `IAdminAuthorizationService`. `Gma.Framework.Administration` denies by default. Hosts that compose AccessControl admin front doors also compose the `Gma.Framework.Administration.AccessControl` bridge, which adapts admin operations to the generic access-control decision pipeline.

## Persistence

Schema:

```text
admin
```

Migration history table:

```text
admin.__ef_migrations_history
```

Tables:

- `audit_entries`

Traversal indexes cover global, tenant, actor, operation, and permission reads with `CreatedAtUtc` plus `Id` as the deterministic cursor key. Result and error-code filters use the chronological path to avoid unnecessary append-time index amplification.

Legacy Administration RBAC migrations and tables are retained for upgrade compatibility, but the current Administration model neither maps nor reads them. New RBAC storage belongs to the `access` schema owned by `Gma.Modules.AccessControl`. Follow the [v0.1 RBAC migration guide](v0.1-access-control-migration.md), and do not drop historical tables until the composing product has explicitly verified its AccessControl migration and recovery plan.

## Configuration

```json
{
  "Administration": {
    "Audit": {
      "DefaultPageSize": 50,
      "MaxPageSize": 200,
      "DefaultPurgeBatchSize": 500,
      "MaxPurgeBatchSize": 2000
    }
  }
}
```

Hard limits cap reads at 500 records and purge calls at 5000 records even when configuration asks for more.

## Audit

`AdminAuditSink` writes records for admin operations:

- actor id;
- tenant id;
- operation;
- permission;
- result;
- error code;
- timestamp.

Audit data is intentionally small and secret-free. Do not add command payloads, passwords, tokens, hashes, or raw exception details to audit records.
Actor ids and audit error codes are bounded operation metadata. Actor ids are case-preserving external identifiers, but they cannot contain whitespace or control characters. Error codes should be stable application or domain codes, not free-form messages.

No automatic retention policy is enabled. Retention periods, legal holds, archival, and purge scheduling belong to the composing product or operator. A missing durable sink reports audit failure by default; `NullAdminAuditSink` is an explicit deployment opt-out. Terminal writes use the Framework's bounded, request-independent audit token. Audit sink failures remain visible through the API header or CLI error output, and CLI exit code `4` means the mutation completed without a confirmed audit write. The generic runner cannot transactionally roll back an operation owned by another module.

## Integration Events

The Administration module does not publish integration events in this milestone.

## Tests

Relevant coverage:

- administration metadata, normalization, cursor, handlers, front doors, and audit registration in `Gma.Modules.Administration.Tests`;
- append, UTC normalization, filter isolation, cursor traversal, retention, and legacy-table preservation against real PostgreSQL and SQL Server in `Gma.Modules.Administration.IntegrationTests`;
- generic admin contract and deny-by-default behavior in `Gma.Framework.Tests`;
- AccessControl bootstrap, role, assignment, and persisted decision behavior in `Gma.Modules.AccessControl.Tests`;
- CLI AccessControl and Auth admin flows in `AdminCliIntegrationTests`;
- HTTP AccessControl and Auth admin flows in `AdminApiIntegrationTests`;
- architecture tests for `System.CommandLine` isolation, admin core dependency neutrality, and module boundaries.

## Extension Points

Likely future additions:

- external audit archive and SIEM adapters;
- legal-hold and product retention policy workflows;
- operator activity reports.

Keep the module generic. It should know admin operation metadata and audit, not Auth internals, RBAC table internals, or product-specific user concepts.
