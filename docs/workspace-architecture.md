# Faheem — Workspace Model Architecture

> **Status:** Design locked — implementation not started
> **Author:** Mohamed Mustafa
> **Last updated:** 2026-06-22
> **Scope:** Refactor the multi-tenancy layer from "one teacher = one tenant" into a
> **Workspace model** that supports tutoring **centers** (multiple teachers under one
> billed workspace) and teachers who operate across multiple workspaces.

This document is the single source of truth for the workspace refactor. Any
disagreement during implementation is resolved by re-reading this file and, if
needed, updating it **before** writing code.

---

## 1. Goals

1. A teacher works from **one login** but can belong to **multiple workspaces**:
   - their own **private** workspace (the teacher pays), and/or
   - one or more **center** workspaces (the center pays).
2. **Billing is per workspace.** A center workspace's subscription is paid and
   managed by the center owner and covers all its member teachers (package sized
   by number of member teachers — exact pricing TBD).
3. A **center owner** can invite teachers, and sees everything inside the center
   workspace. A member teacher sees only their own groups within that workspace.
4. **No data leaks between workspaces.** Selecting workspace A must never expose
   workspace B's data.
5. **Students and parents are unaffected** — they are not tenant-scoped (see §3).

### Non-goals (explicitly out of scope for this refactor)
- Payment gateway / Paymob integration (separate track).
- The web admin panel (built **after** this refactor lands).
- Moving a teacher's existing private groups into a center (a center workspace
  **starts empty** — decision locked).

---

## 2. Current State (as built today)

Grounded in the actual code, not assumptions.

### Tenancy mechanics
- **Finbuckle.MultiTenant**, configured in `Infrastructure/StartUp.cs`:
  ```csharp
  .AddMultiTenant<AppTenantInfo>()
      .WithClaimStrategy(ClaimConstants.Tenant)   // header strategy REMOVED (security fix)
      .WithEFCoreStore<TenantDbContext, AppTenantInfo>()
  ```
- The tenant store is a real table: `TenantTbContext` → `Tenants` (schema `Multitenant`).
- `AppTenantInfo`: `Id`, `Identifier`, `Name`, `Email`, `FirstName`, `LastName`,
  `ConnectionString?`, `ValidUpTo`, `IsActive`.

### How a user is bound to a tenant — **it is a claim, not a column**
- `ApplicationUser` has **no** `TenantId` property.
- The binding lives in `AspNetUserClaims` as a claim of type `ClaimConstants.Tenant`
  (`"tenant"`). Because `AspNetUserClaims` is many-rows-per-user, the storage layer
  **already supports** a user belonging to more than one tenant — we just never
  issue or read more than one today.
- Tenant `Id == Identifier == "tenant_{phoneNumber}"` (created in
  `AuthService.RegisterTeacherAsync`). *(Predictable — flagged for future GUID
  migration, not urgent now that the header strategy is gone.)*

### Per-role behaviour today
| Role | Tenant claim? | `IsGlobalUser`? | Notes |
|------|---------------|-----------------|-------|
| **Teacher** | yes (owns the tenant) | no | tenant created inactive, activated on email verify |
| **Assistant** | yes (the **teacher's** tenant) | no | separate `ApplicationUser`, role `Assistant`, `UserType.Teacher` |
| **Student** | **no** | **yes** | sees data via `GroupStudent` joins, not tenant filter |
| **Parent** | **no** | **yes** | same |

### Data isolation
`ApplicationDbContext` applies a global query filter to every `IMustHaveTenant`
entity:
```csharp
x => _currentUserService.IsGlobalUser || x.TenantId == _currentUserService.TenantId;
```
- `CurrentUserService.TenantId` = the `"tenant"` claim from the JWT.
- `IsGlobalUser` = true for Student/Parent → filter is bypassed for them.

### JWT
`TokenService.GetUserClaimsAsync` puts into the token: `sub`, `email`, `jti`,
name, phone, `SecurityStamp`, the **tenant claim**, subscription flags
(`Tenant_IsActive`, `Tenant_ValidUpTo`, `Tenant_IsExpired`), roles, and
permissions. An expired subscription downgrades permissions to read-only.

### Key insight
The **Assistant** already *is* a "workspace member with a non-owner role" —
expressed clumsily via a shared tenant claim. The refactor formalises this
concept instead of inventing it.

---

## 3. Target Architecture

**Tenant = Workspace.** A workspace is `Individual` or `Center`. Membership becomes
a first-class entity. The tenant claim in the JWT stays (Finbuckle reads it) but is
now **derived from the membership table at token-issue time**, after the user picks
a workspace.

```
User (one login)
 ├── WorkspaceMember(role=Owner)     → Workspace "Mohamed (private)"   Type=Individual
 ├── WorkspaceMember(role=Teacher)   → Workspace "El-Nokhba Center"    Type=Center
 └── WorkspaceMember(role=Teacher)   → Workspace "Future Center"       Type=Center
```

Students/parents stay **global** (no membership, no tenant claim) — the workspace
model does not touch them. This keeps the refactor surface small.

---

## 4. Data Model Changes

### 4.1 `AppTenantInfo` (+ `Tenants` table)
Add:
```csharp
public TenantType Type { get; set; }   // Individual | Center
// optional, for center packages (Phase 3):
public int? MaxTeachers { get; set; }  // null = unlimited / individual
```
```csharp
public enum TenantType { Individual = 0, Center = 1 }
```

### 4.2 New entity: `WorkspaceMember`
The new source of truth for "who belongs to which workspace, and as what".
```csharp
public class WorkspaceMember
{
    public Guid Id { get; set; }
    public string UserId { get; set; }          // FK → AspNetUsers
    public string TenantId { get; set; }         // FK → Tenants.Id
    public WorkspaceRole Role { get; set; }      // Owner | Teacher | Assistant
    public WorkspaceMemberStatus Status { get; set; } // Active | Invited
    public DateTime CreatedAt { get; set; }
}

public enum WorkspaceRole { Owner = 0, Teacher = 1, Assistant = 2 }
public enum WorkspaceMemberStatus { Active = 0, Invited = 1 }
```
- Unique index on `(UserId, TenantId)`.
- This **replaces** the dual-purpose tenant claim as the membership record. The JWT
  tenant claim is still emitted, but its value now comes from the selected
  `WorkspaceMember`, not from a free-standing claim.

### 4.3 `Group` (and the center multi-teacher problem)
`Group` has `TenantId` but **no owner field**. Today that's fine (tenant = teacher).
In a center, many teachers share **one** tenant, so we must record which teacher
owns each group:
```csharp
public string? OwnerUserId { get; set; }   // teacher who created the group
```
- Backfill: for existing single-teacher tenants, set `OwnerUserId` = that tenant's
  Owner.
- Query rule inside a center:
  - **Center Owner** → sees all groups in the tenant (no `OwnerUserId` filter).
  - **Member Teacher** → sees only `OwnerUserId == currentUserId`.

> Same ownership consideration may later apply to other teacher-created entities
> (exams, materials). They inherit isolation from `Group` via FK, so no extra
> column is needed as long as access always goes through the owning group.

---

## 5. Authentication & Workspace Selection Flow

### 5.1 Login — backward compatible by design
`POST /api/token` (login) counts the user's **active** workspace memberships:

| Active memberships | Behaviour | Who hits this |
|--------------------|-----------|---------------|
| **0** | issue full token, **no** tenant claim (today's behaviour) | students, parents |
| **1** | issue full token **with** that tenant claim (today's behaviour) | every existing teacher/assistant |
| **≥2** | issue a short-lived **Account Token** (identifies the user, **no** tenant claim) + return the workspace list | future multi-workspace teachers |

Because **every existing user has ≤1 membership**, the new branch (`≥2`) is dead
code until centers exist → the refactor is **100% backward compatible** on day one.

### 5.2 Select / switch workspace — no re-login
```
POST /api/token/select-workspace
  Auth:  Account Token (or any valid token for this user)
  Body:  { tenantId }
  → validates the user has an Active membership in that tenant
  → issues Access Token (with tenant claim + subscription flags) + Refresh Token
```
- **Switching** workspace later = call the same endpoint again. No password.
- The refresh token is bound to the issued (tenant-scoped) access token, so a
  refresh keeps you in the same workspace.

### 5.3 Response shape (new)
```jsonc
// login with ≥2 memberships
{
  "accountToken": "...",          // short-lived, no tenant
  "workspaces": [
    { "tenantId": "...", "name": "Mohamed (private)", "type": "Individual", "role": "Owner" },
    { "tenantId": "...", "name": "El-Nokhba Center",  "type": "Center",     "role": "Teacher" }
  ]
}
```
The mobile app shows a workspace picker when `workspaces` is present; otherwise it
proceeds exactly as today.

---

## 6. Authorization & Data Isolation

- The global query filter is **unchanged** in shape:
  `IsGlobalUser || x.TenantId == CurrentTenant`. Once a workspace is selected, the
  JWT carries that tenant and everything scopes correctly — no change to Groups,
  Students, Exams, Attendance, Payments, Materials.
- **Within a center**, add the `OwnerUserId` rule from §4.3 for member teachers.
- A new permission/policy distinguishes **Center Owner** from **Member Teacher**
  (Owner can manage members, see all groups, manage the subscription).
- Header-based tenant override stays **removed** (the security fix). Tenant is only
  ever trusted from the signed JWT.

---

## 7. Subscription / Billing

- Subscription state stays on the **workspace (tenant)**: `ValidUpTo`, `IsActive`
  (already there). No per-user billing.
- **Individual** workspace → the owner (teacher) pays.
- **Center** workspace → the center owner pays; covers all member teachers.
  Package sized by member-teacher count (`MaxTeachers`). Enforce on invite:
  reject when active members would exceed `MaxTeachers`.
- Expired subscription → existing read-only downgrade applies to that workspace
  only. Other workspaces a teacher belongs to are independent.

---

## 8. Migration Strategy (existing data)

A single EF migration + a one-off data backfill (idempotent):

1. Create `WorkspaceMembers` table; add `TenantType` (+ `MaxTeachers?`) to
   `Tenants`; add `OwnerUserId?` to `Groups`.
2. **Backfill `Tenants.Type`** = `Individual` for all existing rows.
3. **Backfill `WorkspaceMembers`** from `AspNetUserClaims`:
   - for each `(UserId, ClaimValue=tenantId)` where `ClaimType == "tenant"`:
     - if the user is in role `Assistant` → `Role=Assistant`
     - else → `Role=Owner`
     - `Status=Active`, `CreatedAt=now`.
4. **Backfill `Groups.OwnerUserId`** = the `Owner` member of each group's tenant.
5. Leave the existing tenant **claims in place** for now (the token pipeline still
   reads them in Phase 1). They are removed from the issue path in Phase 2 once the
   token derives the tenant from `WorkspaceMember`.

> The backfill is written so re-running it is safe (check-before-insert), and it
> runs inside the seeder/transaction so a failure rolls back cleanly.

---

## 9. What Does NOT Change

- Students & parents: registration, login, all data access.
- The domain tables and their `TenantId` columns: Groups, GroupStudents, Sessions,
  Attendance, Exams/Grades, PaymentCycles/Records, Materials, Announcements.
- The global query filter's shape.
- Refresh-token rotation + reuse detection.
- The Finbuckle claim strategy (we keep emitting a `tenant` claim).

This is why the effort is **"refactor the Identity/Auth layer"**, not "rebuild the
app".

---

## 10. Implementation Phases (with checkpoints)

Work happens on branch **`feature/workspace-model`**, never directly on `main`.
Each phase ends with a verification gate before the next begins.

### Phase 1 — Foundation (no behaviour change)
- `TenantType` enum + column; `WorkspaceMember` entity + table; `Group.OwnerUserId`.
- Migration + backfill (§8 steps 1–4).
- **Checkpoint:** existing app behaves **identically** — register/login/groups/
  attendance/payments all work as before. Backfill verified: every existing
  teacher has one `Owner` membership; assistants have `Assistant` memberships.

### Phase 2 — Auth flow
- Login returns workspace list when memberships ≥2 (Account Token).
- `POST /select-workspace` issues the tenant-scoped token; switching works.
- Token pipeline derives the tenant claim from the selected `WorkspaceMember`
  (stop reading the standalone tenant claim on the issue path; §8 step 5).
- Mobile app: workspace picker screen (only shows when needed).
- **Checkpoint:** single-workspace users see **zero** change. Create a test user
  with 2 memberships → verify login → pick → data isolation → switch → isolation.

### Phase 3 — Center features
- Create `Center` workspaces; `POST /invite-teacher` + accept flow
  (`WorkspaceMember Status: Invited → Active`).
- `Group.OwnerUserId` enforcement: owner sees all, member teacher sees own.
- Center-owner role/policy + member management UI.
- `MaxTeachers` seat enforcement on invite.
- **Checkpoint:** full center scenario E2E — owner invites two teachers, each
  creates groups, owner sees both sets, each teacher sees only their own, students
  unaffected, billing/seat limit honoured.

---

## 11. Open Items / Future

- **Tenant Id → GUID** instead of `tenant_{phone}` (privacy/predictability;
  low priority now that header override is gone).
- Payment gateway integration (Paymob) — separate track, after Phase 3.
- Web admin panel — after the workspace model is stable.
- Per-entity ownership for exams/materials if we ever allow access outside the
  owning group.
