# Administration Domain Completion Task

Status: complete
Date: 2026-07-20

## Goal

Complete the reusable Administration domain for production use without turning it into a security-policy, identity, tenancy, or product-operations module.

The Framework owns generic administrative execution contracts. Administration owns durable administrative audit data and its audit operations. AccessControl owns persisted authorization. Skeleton owns the canonical composition and generation experience. Products own operator grants, retention policy, legal holds, archives, and product-specific interpretation.

This task builds on the completed [Administration production hardening](administration-production-hardening-task.md) slice. That slice established bounded audit discovery and retention. This completion slice closes the remaining cross-repository contract, failure-semantics, provider-proof, upgrade, and composition gaps.

## Verified Baseline

- Administration is dependency-neutral and does not reference another reusable module or product code.
- Persisted RBAC is absent from the current Administration model and belongs to AccessControl.
- Audit reads use deterministic `(CreatedAtUtc, Id)` keyset pagination without counts.
- Purge is confirmed, cutoff-based, and bounded.
- PostgreSQL persistence tests, migration-drift checks, boundary checks, zero-warning builds, and package audits pass.
- Skeleton and BunkFy currently compose Administration, AccessControl, and their separate admin hosts successfully.

## Findings

1. Framework registers `NullAdminAuditSink` by default. An admin host that forgets Administration silently discards audit records and reports no audit failure.
2. The runner passes request cancellation into the terminal audit write. A request can be cancelled after its mutation commits and prevent the outcome from being recorded.
3. CLI execution writes an audit warning to stderr but still exits with success when the operation succeeded and its audit failed. Automation cannot distinguish complete success from an unaudited mutation.
4. `AdminPermission` still accepts the legacy owner wildcard even though wildcards are persisted AccessControl grants, never permissions requested by an admin operation. The AccessControl bridge has to reject this invalid state explicitly.
5. `new-gma-app.ps1` can generate `AdminApi` and `AdminCli` hosts while leaving selected Administration and AccessControl modules unreferenced and unregistered. Administration-only persistence settings, migrations, readiness, and packaged CLI content-root behavior are also missing.
6. The module proves persistence directly only on PostgreSQL. SQL Server behavior currently relies on larger Skeleton and product suites instead of a focused module-owned relational contract.
7. Version `0.1` Administration RBAC tables are intentionally retained but the upgrade/recovery procedure and preservation proof are incomplete. Automatic cross-module migration must not be guessed inside either module.
8. Skeleton's root summary still describes Administration as owning persisted RBAC, which contradicts the current domain boundary.

## Delivery Slices

### 1. Framework Audit Semantics

- replace the silent default audit registration with an unavailable sink that produces the existing bounded audit-failure signal;
- retain an explicit no-op sink only for hosts that consciously opt out;
- attempt terminal audit writes independently of request cancellation, with a bounded framework-owned timeout;
- normalize audit timestamps to UTC at the generic contract boundary;
- add a distinct CLI exit code for an operation that completed but was not audited, and document that callers must not blindly retry it;
- preserve API operation results while continuing to expose audit failure through the stable response header.

### 2. Permission Boundary Cleanup

- remove owner-wildcard acceptance and state from `AdminPermission`;
- keep wildcard grants solely in AccessControl contracts and persistence;
- simplify the Administration-to-AccessControl bridge to translate concrete requested permission codes only;
- prove no source consumer depends on the removed compatibility state.

### 3. Administration Persistence Proof

- add focused SQL Server integration coverage for append, filters, cursor traversal, and bounded retention;
- prove UTC normalization through persistence;
- prove current migrations preserve legacy RBAC tables and their data while the current EF model remains audit-only;
- keep PostgreSQL and SQL Server migration snapshots drift-free;
- run both providers in the module's relational CI job.

### 4. Canonical Skeleton Composition

- make generated admin hosts compose selected admin-capable modules rather than creating empty shells;
- require Administration and AccessControl for the production admin-host scaffold so generated hosts have durable audit and persisted authorization;
- add Administration persistence settings, migration references, readiness, and module registrations to generated hosts;
- generate the Admin CLI with the canonical output-directory content root, startup validation, and bounded exception handling;
- extend the generated-selection matrix with Administration/Admin API/Admin CLI coverage;
- correct Skeleton documentation and guards so future generator changes cannot reintroduce silent degradation.

### 5. Upgrade And Operator Guidance

- document the supported `v0.1` Administration-RBAC to AccessControl recovery sequence;
- require inventory, backup, AccessControl bootstrap/import or deliberate role recreation, permission comparison, and rollback evidence before legacy tables are removed;
- do not put cross-module SQL or automatic semantic guesses in Framework, Administration, or AccessControl;
- leave a reusable automated importer for a later explicit `GMA-Extensions` slice if real upgrade demand justifies it.

### 6. Product Alignment

- move Framework and Administration pins only after their exact commits are green;
- verify Skeleton admin API/CLI against SQL Server and PostgreSQL after composition changes;
- align BunkFy backend pins and generated workspace files without exposing audit through the public product API;
- run BunkFy fast, architecture, host, integration, and Docker validation before closing the domain.

## Ownership Boundaries

Framework owns:

- admin actor, operation, requested permission, execution, authorization, and audit-write contracts;
- deny-by-default authorization;
- truthful missing-audit behavior, terminal audit attempt semantics, API audit signaling, and CLI exit-code mapping.

Administration owns:

- the audit aggregate persistence representation and migrations;
- audit discovery, cursor, filters, and explicit bounded retention;
- Administration audit permissions and Admin API/CLI front doors;
- preservation and documentation of its historical schema.

AccessControl owns:

- roles, wildcard and concrete grants, subjects, assignments, scopes, and authorization decisions;
- bootstrap and role-management front doors.

Skeleton owns:

- production-oriented host composition, generator defaults, selection-matrix proof, and architecture guards.

Products own:

- operator assignments and deployment authentication;
- retention periods, schedules, legal holds, external archive, and regulator-specific policy;
- audit UI, actor display names, target enrichment, and product-specific reports.

## Acceptance Criteria

- a host missing a real audit sink cannot silently claim a fully audited operation;
- a committed operation still makes a bounded terminal audit attempt after request cancellation;
- CLI automation receives a distinct partial-success exit code when audit persistence fails;
- Framework Administration contains no wildcard grant concept and still has no AccessControl dependency;
- Administration remains audit-only and references no reusable module implementation or product source;
- focused PostgreSQL and SQL Server relational tests pass, including legacy-table preservation;
- generated admin hosts are usable, durable, authorized, and non-empty when requested;
- generated CLI configuration and exception behavior match the canonical Skeleton host;
- current Framework, Administration, Skeleton, and BunkFy commits are green before each parent pin moves;
- no public product endpoint exposes the Administration audit store.

## Explicitly Deferred

- automatic retention schedules or universal retention periods;
- legal-hold policy, privacy erasure decisions, and regulator-specific behavior;
- SIEM, WORM/object-storage archive, cryptographic chains, and tamper attestation;
- product audit dashboards, arbitrary text search, and cross-module actor or target enrichment;
- automatic migration of legacy RBAC data without a separately designed, explicit Extensions contract;
- distributed transaction guarantees between a module mutation and the separate audit store.

## Completion Evidence

- Framework `aa08aeabc8b597747a9761b3e36bb105e06531f2`: exact-commit validation passed in run `29760542302`; the local Framework suite passed 1,005 tests.
- Administration `4d3fceaceab432c7c0bd216ebad6dffe99cd174a`: exact-commit validation passed in run `29761458108`; local validation passed 29 fast tests and 10 required relational tests across PostgreSQL and SQL Server with no skips.
- Skeleton `6d8d1bf2a77ad6c17c530fd4f685e0b7dd0eaecb`: Windows and Ubuntu validation passed in run `29763650070`, and exact-commit Docker validation passed in run `29764151001`.
- BunkFy backend `dcfffb92628aee0d5de882c9d94fd6f55c4bd6b8`: Windows and Ubuntu validation passed in run `29765676240`, and required Docker validation passed in run `29765677045` with 27 tests and no skips.
- BunkFy root `129540830e00f246049783a55545b6bac4542b15`: clean recursive workspace validation, including backend and committed frontend gates, passed in run `29766437697`.
- Public product hosts still do not compose or expose the Administration audit front door; Administration remains available only through the dedicated admin API and CLI composition roots.
