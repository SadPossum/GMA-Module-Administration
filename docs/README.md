# Administration Module

The Administration module is optional. It owns persisted admin audit and empty CLI/API shell modules that let hosts opt into audit storage without also opting into persisted RBAC.

It does not own roles, permission grants, subject assignments, or feature-specific administration behavior. Persisted RBAC lives in `Gma.Modules.AccessControl`. Feature modules expose their own `.AdminCli` and `.AdminApi` front doors and declare their own permission codes.

## Projects

```text
Gma.Modules.Administration.Contracts
Gma.Modules.Administration.Application
Gma.Modules.Administration.Persistence
Gma.Modules.Administration.AdminCli
Gma.Modules.Administration.AdminApi
```

## Public Contracts

`Gma.Modules.Administration.Contracts` contains Administration module metadata only. Shared administration contracts live in `Gma.Framework.Administration`.

The Administration admin CLI/API projects intentionally map no commands or endpoints in the current slice. They register audit persistence and keep a stable composition point for future audit read/export surfaces.

## CLI Commands

This module does not declare CLI commands.

Hosts that want persisted role management compose `Gma.Modules.AccessControl.AdminCli`, which owns the compatibility `admin bootstrap` and `admin roles ...` commands.

## Admin API

This module does not declare HTTP endpoints.

Hosts that want persisted role management compose `Gma.Modules.AccessControl.AdminApi`, which owns the compatibility `/api/admin/roles` routes. Bootstrap remains CLI-only.

## Permissions

This module does not declare permissions.

AccessControl declares the compatibility admin permissions:

| Permission | Purpose |
| --- | --- |
| `admin.bootstrap` | First owner bootstrap operation. |
| `admin.roles.read` | List roles and assignments. |
| `admin.roles.manage` | Create roles, grant permissions, and assign roles. |

## Application Layer

Application code currently registers the admin audit sink and keeps administration options close to the module. RBAC use cases and persistence ports live in `Gma.Modules.AccessControl.Application`.

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

Legacy Administration RBAC migrations are retained for compatibility, but the current Administration model maps audit only. New RBAC storage belongs to the `access` schema owned by `Gma.Modules.AccessControl`.

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

## Integration Events

The Administration module does not publish integration events in this milestone.

## Tests

Relevant coverage:

- administration application/audit registration in `Gma.Modules.Administration.Tests`;
- generic admin contract and deny-by-default behavior in `Gma.Framework.Tests`;
- AccessControl bootstrap, role, assignment, and persisted decision behavior in `Gma.Modules.AccessControl.Tests`;
- CLI AccessControl and Auth admin flows in `AdminCliIntegrationTests`;
- HTTP AccessControl and Auth admin flows in `AdminApiIntegrationTests`;
- architecture tests for `System.CommandLine` isolation, admin core dependency neutrality, and module boundaries.

## Extension Points

Likely future additions:

- audit export/read APIs;
- audit retention policies;
- operator activity reports.

Keep the module generic. It should know admin operation metadata and audit, not Auth internals, RBAC table internals, or product-specific user concepts.
