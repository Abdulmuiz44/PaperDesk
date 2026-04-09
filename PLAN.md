# PaperDesk V1 Execution Plan

## Project Status (Repository Reality Check)
PaperDesk has moved past a pure scaffold into an **initial vertical slice foundation**:
- App shell, dependency wiring, and startup flow are in place.
- Domain model and safety defaults exist and are test-covered.
- SQLite bootstrap is implemented at initialization level (database open + baseline pragma).

The majority of end-user automation behavior described in this plan remains **planned work**:
- OCR extraction pipeline, real watcher-driven ingestion, repository persistence, indexing, duplicate workflows, and rename execution are still deferred or represented by placeholders.

Use this document as the target-state roadmap, not a claim that all sections below are already delivered.

## 1) Product Vision
PaperDesk is a **local-first, Windows-native desktop utility** that automates document organization for people who deal with high volumes of operational files (invoices, receipts, contracts, scans, statements, and client docs).

### Who it is for
- Small business owners managing documents across ad-hoc folders.
- Accountants and admin staff handling repetitive document intake and filing.
- Founders/operators who need quick retrieval of finance/legal docs.
- Freelancers juggling receipts, invoices, and contracts for multiple clients.

### Pain it solves
PaperDesk removes daily document chaos by:
- Watching messy input folders.
- Reading document content locally.
- Suggesting meaningful filenames.
- Filing documents into consistent structures.
- Making everything searchable offline.
- Flagging duplicates before clutter spreads.

### Why local-first, Windows-native matters
- **Privacy and compliance:** Sensitive business files never need to leave the machine.
- **Reliability:** Works without internet connectivity.
- **Performance:** Native desktop worker can process files continuously in background.
- **User trust:** A “boring utility” should be predictable, auditable, and safe by default.
- **Platform fit:** Windows is where this user segment already does scanner + accounting workflows.

---

## 2) Problem Statement
Target users currently run fragmented, manual workflows:

1. Files land in multiple places (Downloads, Desktop, scanner output, messaging app export folders).
2. Naming is inconsistent (`scan001.pdf`, `IMG_8944.jpg`, `invoice-final-final.pdf`).
3. Sorting is delayed because users lack time/context at intake moment.
4. Search later fails because filenames are poor and OCR text is not indexed.
5. Duplicate copies accumulate through re-downloads, forwarding, and edits.
6. Users fear cloud tools due to data privacy, client confidentiality, and internet dependence.

**Result:** Lost time, filing errors, duplicate clutter, delayed bookkeeping, and increased operational risk.

---

## 3) MVP Scope
PaperDesk V1 is a **single-user Windows desktop app** focused on safe, local automation.

### In scope
1. **Watched Folders**
   - User can add/remove local folders.
   - Recursive watch optional per folder.
   - File type filters (PDF, JPG, PNG initially).

2. **Local OCR for PDFs and images**
   - Extract text from text-based PDFs directly.
   - OCR scanned PDFs and images locally.
   - Store OCR confidence metadata.

3. **Content-aware file renaming**
   - Suggest standardized names from extracted fields.
   - Example pattern: `YYYY-MM-DD_DocType_Party_Amount.ext`.
   - Show confidence and rationale for each suggestion.

4. **Auto-sorting into folders**
   - Suggest destination folder based on rules + inferred type.
   - User-configurable target roots (e.g., `Invoices/2026/`).

5. **Searchable local document index**
   - Full-text search across OCR content and metadata.
   - Filters: doc type, date range, source folder, status.

6. **Duplicate detection**
   - Exact duplicate detection via content hash.
   - Near-duplicate signals via file name + size + extracted key fields.
   - No auto-delete in V1.

7. **Activity log / audit trail**
   - Every proposed and applied action is logged.
   - Includes timestamp, old path, new path, rule, and user confirmation status.

8. **Preview + approval before bulk actions**
   - Queue view of pending rename/move operations.
   - “Apply selected” and “Apply all” with explicit confirmation.
   - Undo for recent operations (bounded window).

### V1 guardrails
- No destructive action without explicit user confirmation.
- Default mode is “Suggest only” until user enables auto-apply for trusted rules.
- All processing remains local.

---

## 4) Non Goals (V1)
PaperDesk V1 intentionally excludes:
- Cloud sync/backup.
- Multi-user collaboration/team workspaces.
- Mobile applications.
- Browser/web app.
- Conversational AI assistant/chat UX.
- Integrations with accounting suites (QuickBooks/Xero/etc.).
- Email ingestion integrations.
- Cross-platform (macOS/Linux) support.

---

## 5) User Personas

### Persona A: "Owner-Operator Olivia"
- Runs a 12-person services business.
- Receives invoices via WhatsApp and email downloads.
- Monthly pain: end-of-month cleanup takes 6+ hours.
- Needs: auto-file invoices/receipts, quick search during tax prep.

### Persona B: "Account Admin Arun"
- Back-office admin for multiple client entities.
- Handles scanner output and supplier invoices daily.
- Pain: inconsistent naming from different team members.
- Needs: deterministic naming convention and reliable audit trail.

### Persona C: "Freelancer Farah"
- Independent consultant with many small transactions.
- Receipts accumulate in Desktop and phone export folders.
- Pain: duplicate receipts and missing proof during reimbursement.
- Needs: duplicate flags + easy retrieval by vendor/date/amount.

### Persona D: "Finance Lead Leo"
- Operates in privacy-sensitive environment.
- Disallows cloud document processors for compliance reasons.
- Pain: manual local filing without searchable index.
- Needs: fully offline OCR + searchable archive.

---

## 6) Core User Flows

### 6.1 First-time onboarding
1. Install PaperDesk.
2. First launch wizard explains local-only processing and safety model.
3. User selects watched folders and output root folders.
4. User chooses naming template and default “suggest-only” mode.
5. Initial scan indexes existing files (read-only pass first).

### 6.2 Adding watched folders
1. User opens Settings → Watched Folders.
2. Adds folder, selects recursive mode and file extensions.
3. PaperDesk validates access permissions and estimated file count.
4. Folder enters “active watch” state; backlog processing queued.

### 6.3 Processing new files
1. File watcher detects new/changed file.
2. Processing pipeline: stabilization wait → type detection → text extraction/OCR.
3. Metadata extracted (date/vendor/amount/doc type where possible).
4. Rename + destination suggestions generated with confidence score.
5. Item appears in Review Queue.

### 6.4 Reviewing rename suggestions
1. User opens Review Queue.
2. For each file: see old/new path diff, confidence, duplicate warnings.
3. User can edit filename/destination, skip, or approve.
4. On bulk approve, confirmation dialog summarizes actions.
5. Operations execute transactionally; outcomes logged.

### 6.5 Searching documents
1. User enters keyword (e.g., vendor or invoice number).
2. Results ranked by textual relevance + metadata match.
3. Filters narrow by doc type/date/folder.
4. Open containing folder or preview file from result.

### 6.6 Resolving duplicates
1. Duplicate center lists exact and potential duplicate groups.
2. User compares metadata/hash/preview.
3. User marks canonical copy; others can be moved to “Review Duplicates” folder.
4. No hard delete by default in V1.

### 6.7 OCR failures / low confidence
1. File flagged with low confidence or extraction error.
2. User sees reason (poor scan, unsupported format, locked file).
3. User can retry OCR, manually tag metadata, or keep original.
4. Failure event logged for diagnostics.

---

## 7) Feature Breakdown

### A) Folder Watcher
- Register/unregister watched paths.
- Debounce and file stabilization checks.
- Handle rename/move events from external apps.
- Pause/resume per folder.

### B) Ingestion + OCR Engine
- MIME/type detection.
- Direct PDF text extraction.
- OCR fallback for scanned content.
- Language profile (start with English; extensible).

### C) Metadata Extraction
- Parse dates, amounts, invoice numbers.
- Heuristic vendor detection from OCR text.
- Document type classification (invoice/receipt/contract/statement/other).
- Confidence scoring per extracted field.

### D) Renaming Engine
- Template-driven filename generation.
- Illegal character sanitization for Windows paths.
- Collision handling (`(1)`, `(2)` suffixes).
- Confidence + explanation output.

### E) Rules Engine
- User-defined mapping rules (e.g., doc type → folder path).
- Priority ordering and fallback destination.
- Dry-run preview support.

### F) Search Index
- Full-text index for OCR + extracted metadata.
- Incremental updates on file change/delete.
- Query parser with field filters.

### G) Duplicate Detection
- Exact hash index (SHA-256).
- Similarity candidates using size/filename/key metadata.
- Review UI workflows for canonical selection.

### H) Desktop UI
- Dashboard: recent activity + queue stats.
- Review Queue: approve/edit/skip actions.
- Search page with filters.
- Duplicates center.
- Settings (folders, naming rules, processing behavior).

### I) Settings, Logs, and Safety
- Activity/audit timeline.
- Operation history and undo window.
- Error diagnostics panel.
- Export logs for support.

---

## 8) Recommended Tech Stack (Opinionated V1)

### Primary stack choice
- **UI framework:** .NET WPF (Windows-native desktop)
- **Language:** C# (.NET 8 LTS)
- **Architecture style:** Clean architecture with MVVM in UI

### Why this stack
- Best fit for a **Windows-native feel** and mature desktop capabilities.
- Strong filesystem and OS integration.
- Excellent tooling (Visual Studio, profiling, installer ecosystem).
- Lower delivery risk for a "boring and useful" utility than a web-wrapped desktop shell.

### Specific component choices
1. **Local database:** SQLite
   - Reliable, embedded, zero-admin.
   - Good for metadata, queue state, and activity logs.

2. **OCR approach:** Windows OCR API + Tesseract fallback
   - Windows.Media.Ocr gives native integration.
   - Tesseract fallback improves compatibility for edge scans.

3. **File watching:** `FileSystemWatcher` + resilient queue wrapper
   - Native .NET watcher with retry/stabilization logic to handle noisy events.

4. **Search indexing:** SQLite FTS5
   - Keeps indexing local and simple.
   - Sufficient for V1 full-text and metadata filtering.

5. **Installer/packaging:** MSIX (primary) + optional MSI fallback
   - MSIX for clean install/update/uninstall behavior.

6. **Logging:** Serilog with rolling local files
   - Structured logs for supportability and diagnostics.

7. **Testing strategy:**
   - Unit tests for domain/services.
   - Integration tests for pipeline and SQLite interactions.
   - End-to-end smoke tests for critical user flows on Windows CI runners.

---

## 9) Suggested Architecture

### Layer 1: UI (WPF + MVVM)
- Presents queue, search, duplicates, settings.
- Binds to ViewModels; no direct filesystem logic.
- Handles user approvals and confirmations.

### Layer 2: Application/Services
- Orchestrates use cases: ingest, classify, suggest rename, apply operations.
- Coordinates transactions, retries, and audit logging.
- Publishes progress/events to UI.

### Layer 3: Domain/Core Logic
- Pure business rules:
  - naming conventions,
  - rule evaluation,
  - confidence scoring,
  - duplicate policies,
  - safety constraints.
- Test-first and framework-agnostic.

### Layer 4: Infrastructure
- Concrete adapters for:
  - filesystem watcher,
  - OCR providers,
  - hashing,
  - system time/clock,
  - path operations.

### Layer 5: Persistence
- SQLite repositories for:
  - document metadata,
  - OCR text,
  - action queue,
  - audit trail,
  - rules/settings.

### Layer 6: Background Workers
- Long-running workers for:
  - watch event intake,
  - OCR/extraction,
  - index updates,
  - duplicate scanning.
- Bounded concurrency and backpressure controls.

---

## 10) Folder Structure Proposal

```text
PaperDesk/
  PLAN.md
  src/
    PaperDesk.App/                 # WPF shell, App bootstrap
    PaperDesk.UI/                  # Views, ViewModels, UI resources
    PaperDesk.Application/         # Use cases, service orchestration
    PaperDesk.Domain/              # Entities, value objects, policies
    PaperDesk.Infrastructure/      # OCR, file watcher, hashing, adapters
    PaperDesk.Persistence/         # SQLite schema, repositories, migrations
    PaperDesk.Workers/             # Background processing workers
    PaperDesk.Contracts/           # DTOs and service contracts
  tests/
    PaperDesk.Domain.Tests/
    PaperDesk.Application.Tests/
    PaperDesk.Infrastructure.Tests/
    PaperDesk.Persistence.Tests/
    PaperDesk.E2E.Tests/
  build/
    installer/
    scripts/
  docs/
    adr/                           # optional architecture decisions post-MVP
```

> Note: For this prompt, only `PLAN.md` is being created; the structure above is implementation guidance for subsequent prompts.

---

## 11) Risk Analysis

1. **OCR accuracy variability**
   - Risk: poor scans reduce rename/sort quality.
   - Mitigation: confidence scoring, low-confidence queue, manual edit path.

2. **File lock and partial-write issues**
   - Risk: files from scanner/download apps may be in-use during processing.
   - Mitigation: stabilization delay + retry policy + safe skip logging.

3. **Accidental destructive operations**
   - Risk: incorrect bulk move/rename harms trust.
   - Mitigation: preview-first, explicit confirmations, undo window, no auto-delete.

4. **Large folder performance**
   - Risk: first index of large folders feels slow.
   - Mitigation: incremental onboarding scan, progress UI, bounded worker concurrency.

5. **Low-end Windows machine constraints**
   - Risk: OCR and indexing spike CPU/RAM.
   - Mitigation: throttling modes, configurable processing schedule, lightweight defaults.

6. **False duplicate matches**
   - Risk: user confusion or wrong archival decisions.
   - Mitigation: separate exact vs potential duplicates, require user confirmation.

7. **Packaging complexity**
   - Risk: installer friction and OCR dependency issues.
   - Mitigation: early packaging spike (Phase 0), dependency health checks in installer.

8. **Rule misconfiguration by users**
   - Risk: wrong folders/naming patterns.
   - Mitigation: dry-run preview and rule simulation against sample files.

---

## 12) MVP Build Phases

### Phase 0: Foundations & Technical Spike
- **Goal:** De-risk core technical assumptions.
- **Deliverables:**
  - OCR spike with sample PDFs/images.
  - File watcher spike with stability/retry logic.
  - SQLite schema draft for documents/actions.
  - Installer feasibility (MSIX).
- **Exit criteria:**
  - Can ingest sample file, extract text, and persist metadata locally.

### Phase 1: Core Ingestion Pipeline
- **Goal:** Reliable intake and extraction.
- **Deliverables:**
  - Watched folder management.
  - Background queue and processing worker.
  - OCR + metadata extraction service.
  - Basic activity logging.
- **Exit criteria:**
  - New files flow into queue and produce extraction records consistently.

### Phase 2: Rename + Sort Suggestions
- **Goal:** Generate safe, useful automation suggestions.
- **Deliverables:**
  - Naming template engine.
  - Rule-based folder suggestion engine.
  - Preview queue UI with per-item edit/approve/skip.
- **Exit criteria:**
  - User can review and apply rename/move suggestions with confirmations.

### Phase 3: Search + Duplicates
- **Goal:** Make archive retrievable and cleaner.
- **Deliverables:**
  - FTS indexing and search UI.
  - Exact/potential duplicate detection and review flow.
- **Exit criteria:**
  - User can find files by content and resolve duplicate candidates safely.

### Phase 4: Safety, Observability, and Polish
- **Goal:** Production-grade trust and operational quality.
- **Deliverables:**
  - Audit trail completeness.
  - Undo for recent operations.
  - Error handling and diagnostics view.
  - Performance tuning for large folder sets.
- **Exit criteria:**
  - End-to-end flows stable on pilot machines for 2+ weeks.

### Phase 5: Launch Readiness
- **Goal:** Ship a narrow, dependable V1.
- **Deliverables:**
  - Installer and upgrade path.
  - QA sign-off checklist.
  - In-app onboarding copy and docs.
- **Exit criteria:**
  - Candidate build meets success criteria and no P0/P1 defects remain.

---

## 13) Success Criteria (MVP)
V1 is successful if within pilot usage:
- 80%+ of incoming docs are assigned a usable rename suggestion.
- 70%+ of suggestions are accepted without manual edits.
- Search returns desired document in under 5 seconds for typical queries.
- Duplicate detection catches 90%+ of exact duplicates in test corpus.
- Zero incidents of destructive actions without explicit user confirmation.
- Users report at least 50% reduction in weekly manual filing time.

---

## 14) Pricing Hypothesis
Initial pricing model:
- **Free trial:** 14 days full-featured.
- **Paid:** $9–$15/month per Windows device (or ~$99/year).
- **Optional one-time lifetime tier:** higher price point for utility-app buyers.

Rationale:
- Clear ROI through time saved in repetitive filing/search.
- Simple per-device pricing aligns with single-user local desktop utility behavior.

---

## 15) Open Questions
1. Should auto-apply mode be allowed per rule, per folder, or both?
2. Which OCR languages are mandatory at launch beyond English?
3. What is the exact default naming template and conflict behavior policy?
4. Should we support DOCX/XLSX text extraction in V1 or defer?
5. How long should undo history persist (time-based vs operation-count)?
6. What threshold defines “low confidence” for mandatory manual review?
7. Do we quarantine risky operations to a staging folder before final move?
8. What telemetry (if any) can be collected while remaining privacy-first/local-first?
9. What minimum hardware spec should be officially supported?
10. Should near-duplicate detection run continuously or only on-demand?

---

## Technical Direction Summary (V1)
PaperDesk V1 should be built as a **C#/.NET 8 WPF Windows desktop application** with a **local SQLite database**, **Windows-native OCR plus fallback**, **FileSystemWatcher-based ingestion**, and **SQLite FTS5 search indexing**. The product should prioritize **safe preview-and-approve workflows**, **strong auditability**, and **predictable local performance** over broad feature breadth.
