# Administration Production Hardening Task

Status: in progress
Date: 2026-07-19

## Goal

Make the optional Administration module a production-ready persistence and operations surface for generic administrative audit records without moving RBAC, identity, tenant membership, product terminology, or feature-specific administration behavior into the module.

The framework continues to own generic admin execution, authorization, actor context, and audit-record contracts. Administration owns durable audit storage, bounded audit discovery, explicit retention operations, and the module's own Admin API and CLI front doors.

## Audit Baseline

- Administration persists validated, secret-free `AdminAuditRecord` values in its own `admin.audit_entries` table;
- framework administration denies by default, and the AccessControl bridge supplies persisted authorization only when explicitly composed;
- the module no longer maps or reads RBAC entities;
- SQL Server and PostgreSQL migrations retain historical RBAC tables for upgrade compatibility while the current model contains audit entries only;
- the zero-warning solution build, 8 unit tests, and package vulnerability audit pass.

## Findings

1. The module is write-only. Operators and support tooling must read audit data through `AdminDbContext`, which bypasses the Application boundary and cannot be exposed safely to remote administration clients.
2. The Admin API and CLI projects are empty composition shells. They provide no audit discovery or retention workflow despite those being the module's remaining domain responsibilities.
3. The append-only table has no explicit retention operation. An active installation can grow indefinitely, but automatic deletion would be unsafe without a product or operator retention decision.
4. Existing actor and tenant indexes do not support efficient global newest-first traversal, deterministic ties, or exact operation filtering at scale.
5. Offset pagination would become increasingly expensive on a large audit log. Audit reads need a stable `(CreatedAtUtc, Id)` keyset cursor, a strict maximum page size, and one-extra-row `HasMore` detection without counts.
6. Persistence behavior is covered only through entity constructor tests. There is no real-database proof for append, scope/filter isolation, cursor traversal, or bounded deletion.
7. CI restores, builds, and runs tests on Windows only. The repository has no boundary guard, migration-drift check, vulnerability gate, or PostgreSQL integration job.
8. The root README still describes Administration as owning persisted RBAC even though that responsibility moved to AccessControl.
9. Historical `admin` RBAC tables are intentionally inert but their upgrade and cleanup status is not documented clearly enough for operators.

## Delivery Slices

### 1. Contracts And Permissions

- declare global `administration.audit.read` and `administration.audit.purge` permission descriptors in module metadata;
- add a narrow `.Admin.Contracts` project for typed admin permissions and stable operation names;
- keep query DTOs and persistence ports out of Contracts unless a non-front-door consumer is proven;
- retain framework `AdminAuditRecord` as the write contract instead of duplicating generic operation metadata in the module.

### 2. Bounded Audit Discovery

- add an Application query and persistence read port for newest-first audit traversal;
- support exact optional tenant, actor, operation, permission, result, error-code, and UTC time-range filters;
- normalize and validate every filter before persistence access;
- use a module-owned opaque cursor over `(CreatedAtUtc, Id)` with a bounded default and maximum limit;
- fetch one extra record to report `HasMore` and never issue an unbounded count;
- return only the validated audit metadata already stored by the framework contract.

### 3. Explicit Retention

- add a confirmed admin command that deletes only records older than an explicit UTC cutoff;
- cap each deletion batch and return the deleted count plus whether more eligible rows remain;
- append an audit record for the retention operation after the bounded deletion completes;
- provide no automatic schedule or default retention period; hosts must make that policy decision explicitly.

### 4. Admin Front Doors

- map authorized `GET /api/admin/audit` and confirmed `POST /api/admin/audit/purge` operations;
- add `administration audit list` and `administration audit purge` CLI commands with stable machine-readable output support;
- route every operation through `AdminApiExecutor` or `AdminCliExecutor` so deny-by-default authorization and audit behavior stay uniform;
- keep these surfaces in the administration hosts, not the normal product API.

### 5. Persistence And Indexes

- add global, tenant, actor, and operation traversal indexes with the cursor tie-breaker;
- use no-tracking projected reads and provider-translatable keyset predicates;
- delete retention candidates by a bounded id set inside the module transaction boundary;
- add drift-free SQL Server and PostgreSQL migrations;
- leave historical RBAC tables untouched and document an explicit future cleanup/migration decision rather than dropping possibly unmigrated security data.

### 6. Proof And Canonical Composition

- add unit tests for cursor validation, filter normalization, handlers, front-door registration, and deterministic result shaping;
- add real PostgreSQL tests for append, filter isolation, complete cursor traversal without duplicates, and bounded retention;
- add boundary, migration-drift, zero-warning build, test, vulnerability, and PostgreSQL verification scripts;
- require Windows validation and Linux PostgreSQL jobs in module CI;
- compose and exercise the Administration audit API and CLI in GMA Skeleton;
- align BunkFy Admin API, Admin CLI, migration host, architecture guards, and integration tests without exposing audit data through the product web API;
- move every parent pin only after the exact upstream commit is green.

## Ownership Boundaries

Framework continues to own:

- generic admin actors, operations, permissions, authorization, execution, and audit-write contracts;
- deny-by-default behavior and API/CLI execution adapters;
- generic CQRS, result, naming, and time primitives.

Administration owns:

- durable audit-entry persistence and migrations;
- audit query, cursor, filtering, and retention use cases;
- audit-specific permission descriptors and operation names;
- Administration Admin API and Admin CLI front doors.

AccessControl owns:

- persisted roles, permissions, assignments, scoped profiles, and authorization decisions.

Products and composing hosts own:

- which operators receive Administration permissions;
- retention periods, purge schedules, legal holds, and external archive policy;
- product UI, actor display-name enrichment, and product-specific audit interpretation.

No Framework change is planned. A module-owned cursor keeps the wire contract narrow without prematurely standardizing one cursor encoding across unrelated domains. Extraction into generic pagination belongs in Framework only after another production domain proves the same semantics.

## Acceptance Criteria

- no Administration source project references Auth, AccessControl, Organizations, product modules, NATS, Redis, or task-runtime internals;
- RBAC remains absent from the current Administration model and public surface;
- audit reads are authorized, exactly filtered, newest-first, cursor-bounded, deterministic, and count-free;
- malformed cursors and filters return stable validation failures without reaching persistence;
- retention requires explicit confirmation and cutoff, deletes at most the configured batch maximum, and reports remaining work;
- audit read and retention operations are themselves audited through the generic runner;
- denied callers cannot discover whether matching audit records exist;
- SQL Server and PostgreSQL migrations are drift-free;
- boundary, zero-warning build, unit, package vulnerability, and real PostgreSQL checks pass;
- Skeleton and BunkFy compose the published module without direct audit-database reads or architecture regressions.

## Explicitly Deferred

- automatic retention schedules or a built-in default retention period;
- legal-hold workflows, privacy erasure decisions, and regulator-specific retention rules;
- external SIEM, object-storage archive, WORM storage, or cryptographic tamper evidence;
- arbitrary text search, analytics, dashboards, and product-facing audit UI;
- actor profile joins or cross-module identity enrichment;
- automatic deletion or migration of legacy Administration RBAC tables;
- a framework-wide cursor pagination abstraction.

## Current Verification

The implementation has not started. Baseline proof is a clean dependency graph, a zero-warning build, 8 passing unit tests, and no vulnerable NuGet packages. Exact module, Skeleton, and BunkFy publication commits and CI runs will be recorded after the complete consumer chain is green.
