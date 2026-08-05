# Administration Global Resource-Scope Read Path Task

Status: complete
Date: 2026-08-05

## Goal

Keep the advertised resource-scope audit filter efficient whether or not an operator also supplies a tenant filter.

## Finding

Administration accepts `resourceScope` as an independent optional audit filter. Persistence stores a fixed SHA-256 lookup key and verifies the canonical scope exactly, but the only resource-scope index begins with `TenantId`. A resource-only newest-first query therefore cannot seek by the hash or satisfy cursor ordering efficiently at audit-log scale.

## Delivery

- retain the existing tenant plus resource-scope traversal index;
- add a global `(ResourceScopeHash, CreatedAtUtc, Id)` traversal index;
- generate matching PostgreSQL and SQL Server migrations;
- prove both index shapes in the provider-neutral model and resource-only isolation through the relational contract;
- keep the public filter, authorization model, hash collision check, and ownership boundaries unchanged.

## Verification

- synchronized zero-warning solution build;
- focused non-Docker tests and provider migration-drift checks;
- one exact PostgreSQL and SQL Server relational lane because the slice changes provider schema;
- boundary, repository security/release, package vulnerability, and diff checks.

## Completion Evidence

- The synchronized solution builds with zero warnings and zero errors; all 32 non-Docker tests pass.
- PostgreSQL and SQL Server migration-drift checks pass with matching generated indexes.
- The exact relational lane passes all 10 provider scenarios with no skips, including resource-only isolation through the shared repository contract.
- Boundary, repository security/release, transitive package vulnerability, solution synchronization, and diff checks pass.
- No Framework or consumer contract changed; the existing API and CLI filter now have the provider-owned read path they advertise.
