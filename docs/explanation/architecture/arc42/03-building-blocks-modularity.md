# 03 — Building Blocks & Modularity

## 3.1 Modularity Goals

CommercialNews applies modularity to:

- **Keep business boundaries clear**: each module maps to a major business capability.
- **Reduce coupling**: changes inside module A should not ripple into module B.
- **Increase cohesion**: each module has one primary purpose (avoid “god modules”).
- **Protect the read path** (Public Reading): article list/detail endpoints must not be slowed down by views/likes/comments/email/audit.
- **Enable evolution**: ship V1 quickly, then upgrade toward event-driven projections in V2+ without breaking core.

---

## 3.2 Module Design Principles

### A. Module = Business Capability
- Each major capability becomes a module.
- A module contains its own **application layer (use cases)** and **domain rules**.
- A module **logically owns its data** (ownership is explicit).

### B. Inter-module communication: minimal sync, maximize events
- **Synchronous calls** are used only when an immediate response is required (e.g., routing by slug).
- **Asynchronous events** are used for audit, notifications, view aggregation, cache invalidation, indexing, etc.

### C. Database: “shared DB, not shared schema”
- A **single database** can be used for a modular monolith.
- But **tables/collections belong to a specific module** (the module is the owner).
- Other modules **must not query the owner’s tables directly** (except for explicitly allowed read-only access in V1).

---

## 3.3 Module Catalog (V1)

1) Content (Core Product)  
2) SEO  
3) Media  
4) Public Query (V1) → Read Model (V2+)  
5) Interaction  
6) Identity  
7) Authorization & Audit (Governance)  
8) Notification  

---

## 3.4 Responsibilities & Ownership by Module

### 3.4.1 Content Module (Core)
**Owner:** editorial lifecycle + taxonomy.

**Owns**
- Article/News: draft/published/unpublished/archived
- Category
- Tag + ArticleTag
- NewsHistory (edit history/versioning)

**Public APIs (use cases)**
- CreateDraft, UpdateDraft
- Publish(reason?), Unpublish(reason?)
- Archive/Restore (optional)
- Category CRUD, Tag CRUD
- AttachTags/DetachTags

**Domain Events (emit)**
- ArticleCreated
- ArticleUpdated
- ArticlePublished
- ArticleUnpublished
- ArticleArchived (optional)

> Content is the source of truth for article lifecycle and publication state.

---

### 3.4.2 SEO Module
**Owner:** slug/meta/canonical/social preview + policy.

**Owns**
- SeoMetadata: slug, metaTitle, metaDescription, canonical, social preview fields
- Slug uniqueness policy
- (V2) slug alias/redirect strategy, sitemap/robots

**Public APIs**
- UpsertSeo(articleId, ...)
- GenerateSlug(title), EnsureUnique(slug)
- GetSeo(articleId)
- GetArticleIdBySlug(slug) (routing)

**Consumes Events**
- ArticlePublished / ArticleUnpublished / ArticleArchived
- ArticleUpdated (when policy requires meta/slug evaluation)

**Emits Events**
- SeoUpdated (when cache invalidation / read model updates are needed)

> SEO does not control publishing. It reacts to Content publication state.

---

### 3.4.3 Media Module
**Owner:** media objects + attachment policy.

**Owns**
- Media: url, type, alt, metadata, status
- ArticleMedia: mapping + ordering + primary
- Soft delete/restore policy

**Public APIs**
- RegisterMedia(url, type, alt, metadata)
- AttachToArticle(articleId, mediaId, isPrimary?)
- Reorder(articleId, mediaIds[])
- SetPrimary(articleId, mediaId)
- SoftDelete/Restore(mediaId)

**Consumes Events**
- ArticleArchived (if cleanup policy exists)

**Emits Events**
- MediaAttached
- MediaPrimaryChanged
- MediaReordered (optional)

> Media only needs `articleId`; it must not join into Content workflows.

---

### 3.4.4 Public Query Module (V1) → Read Model (V2+)
**Owner (V1):** public read use cases (list/detail/related) + caching policy.

**Owns**
- Query logic for public endpoints
- (V2+) projection tables / denormalized read models / search indexing

**Public APIs**
- GetArticles(page, filters, sort)
- GetArticleBySlug(slug)
- GetRelated(articleId)

**Data sources**
- V1: read data by policy from Content/SEO/Media (read-only, controlled)
- V2+: subscribe to events and build dedicated projections

> This module isolates the read path and enables V2 upgrades without polluting core modules.

---

### 3.4.5 Interaction Module
**Owner:** views/likes/comments (+ moderation hooks).

**Owns**
- Views: raw logs + aggregation policy
- Likes: per user/article toggle + totals
- Comments: CRUD + moderation flags (V2)

**Public APIs**
- TrackView(articleId, userId?, visitorKey?)
- Like/Unlike(articleId, userId)
- CreateComment/EditComment/DeleteComment

**Consumes Events**
- ArticleUnpublished / ArticleArchived (disable interaction policy)

**Emits Events**
- ArticleViewed
- ArticleLiked
- CommentCreated / CommentUpdated / CommentDeleted

> Interaction must not slow down read endpoints; view tracking should be non-blocking.

---

### 3.4.6 Identity Module
**Owner:** user accounts + credentials + sessions.

**Owns**
- Register/Login/Refresh/Logout
- Email verification
- Forgot/Reset password
- Profile/ChangePassword

**Public APIs**
- Register, Login, RefreshToken, Logout
- VerifyEmail, ResendVerification
- ForgotPassword, ResetPassword
- UpdateProfile, ChangePassword

**Emits Events**
- UserRegistered
- UserEmailVerified
- PasswordResetRequested
- UserPasswordChanged

> Identity is independent; other modules reference only `UserId`.

---

### 3.4.7 Authorization & Audit (Governance)

#### Authorization Module
**Owner:** roles/permissions + policy evaluation.

**Owns**
- Role, Permission, UserRole, RolePermission
- Policy evaluation boundary

**Public APIs**
- AssignRole/RevokeRole
- GrantPermission/RevokePermission
- EvaluatePolicy(permission, subject, resource, environment)

#### Audit Module
**Owner:** append-only audit trail + audit policy.

**Owns**
- Audit log store
- Audit policy (which actions must be logged, redaction rules)

**Consumes Events**
- Domain events from modules
- Filters by audit policy for persistence

> Audit is cross-cutting but remains a dedicated module to avoid scattering logging across the codebase.

---

> Related:
> - `04-runtime-view-v1.md`
> - `09-architecture-style.md`
> - `10-system-data.md`
> - `13-transactions-and-consistency-v1.md`

---

### 3.4.8 Notification Module
**Owner:** system notifications (email) + templates + retry policy.

**Owns**
- Verification email
- Reset password email
- (Optional) new-article notification

**Consumes Events**
- UserRegistered → send verification
- PasswordResetRequested → send reset
- ArticlePublished → notify subscribers (optional)

> Notification is a consumer; domain modules must not directly depend on Notification.

---

## 3.5 Dependency Rules

### Allowed
- Content: does not depend on other modules (core)
- SEO/Media: depend on Content via events and `articleId` (no workflow joining)
- Public Query (V1): calls read-only APIs from Content/SEO/Media by policy
- Interaction: uses `articleId`, `userId` (no join into User/Content for rendering)
- Identity: independent
- Authorization: independent, uses `userId`
- Audit/Notification: consume events only

### Forbidden
- Interaction querying Content tables directly to render details
- Public Query writing/updating Content/SEO/Media tables directly
- SEO changing publication status
- Any module bypassing ownership by directly querying another module’s database schema (except explicitly allowed V1 read-only access)

---

## 3.6 Standardized Inter-module Contracts

### A. ID Contract
- `ArticleId` and `UserId` must be consistent system-wide.
- Modules store only foreign key IDs, not object graphs from other modules.

### B. Event Contract (envelope)
All domain events follow a minimal envelope:
- EventId
- OccurredAt
- CorrelationId
- ActorUserId? (optional)
- EventType
- Version
- Payload

Example minimal payloads:
- ArticlePublished: { ArticleId, PublishedAt }
- ArticleUnpublished: { ArticleId, Reason?, UnpublishedAt }
- UserRegistered: { UserId, Email }
- PasswordResetRequested: { UserId, Email, ResetTokenId }

---

## 3.7 Evolution Path (V1 → V2)

**V1**
- Clear module boundaries + minimal event catalog
- Public Query acts as a query facade/caching policy (no mandatory full projection yet)

**V2+**
- Upgrade Public Query into a true Read Model: subscribe to events and build denormalized projections
- Upgrade search capabilities (full-text)
- SEO: slug alias/redirect + sitemap/robots
- Interaction: advanced moderation and anti-abuse mechanisms