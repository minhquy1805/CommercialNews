## 1) Business Capabilities — CommercialNews

### A) Content Management (Product Core)

1. **Create Articles**
   - Input: title, body/content, summary, cover/thumbnail
   - Select: category, tags
   - Set status: **Draft / Published / Archived**
   - Output: article with minimum **metadata** (author, timestamps, status)

2. **Edit Articles**
   - Save drafts and update content
   - Maintain an **edit history** (who edited, when, what changed)

3. **Publish / Unpublish**
   - Publish immediately
   - Unpublish when necessary (policy violations, content issues)
   - Record reasons (for auditability & administration)

4. **Category Management**
   - CRUD categories
   - (Optional) hierarchical categories aligned with the content strategy

5. **Tag Management**
   - CRUD tags
   - Attach/detach tags to/from articles

> **Key output:** a clear article **lifecycle**, reliable edit tracking, and sufficient metadata for operating the system.

---

### B) SEO & Discoverability

1. **Slug & Canonical**
   - Generate slugs from titles
   - Ensure slug **uniqueness**
   - Define clear canonical URLs per article

2. **Meta Title / Description**
   - Optimize for search engine snippets
   - Provide sensible defaults when not explicitly set by admins

3. **Social Sharing**
   - Control preview data when shared (title, description, thumbnail)

4. **Sitemap & Robots (V2)**
   - Mechanism to publish important URLs for indexing

---

### C) Media (Images / Videos / Files)

1. **Media Management**
   - Store: URL/path, alt text, media type

2. **Attach Media to Articles**
   - Select a primary image (cover)
   - Reorder media items

3. **Delete / Restore**
   - Support soft-delete and restore based on operational policy

---

### D) Reading Experience (Public)

1. **Article Listing**
   - Pagination
   - Filter by category/tag
   - Sort by time / popularity (scope-dependent)

2. **Article Details**
   - Render content + metadata + media
   - Show related articles (category/tag based)

3. **Search (V1/V2 depending on scope)**
   - Keyword-based article search

---

### E) Interaction (High Traffic, “Hot” Features)

1. **View Tracking**
   - Record views on read
   - (V2) unique view counting based on measurement policy

2. **Likes**
   - Like/unlike
   - Track total likes per article

3. **Comments**
   - Create / edit / delete comments
   - (V2) moderation and anti-spam controls

---

### F) Identity & Access

1. **Sign Up / Sign In**
   - Email + password
   - (Optional) third-party login integrations

2. **Email Verification**
   - Mark accounts as verified
   - Allow resend verification emails (rate-limited)

3. **Session Management**
   - Refresh tokens / logout
   - Rotate/revoke based on security policy

4. **Forgot / Reset Password**
   - Secure password reset flow

5. **Profile**
   - Update personal information
   - Change password

---

### G) Admin Governance & Authorization

1. **Roles / Permissions**
   - Manage roles and permissions; assign/revoke access

2. **Admin Panel (Content & User Administration)**
   - CRUD content
   - User management
   - Hide/approve comments (scope-dependent)

3. **Audit Trail**
   - Log sensitive actions: publish/unpublish, delete/restore, role assignment, etc.

---

### H) Notifications

1. **System Email**
   - Email verification, password reset
   - (Optional) new-article notifications

2. **Abuse Prevention**
   - Rate-limit email sending for sensitive flows (register/forgot/resend, etc.)