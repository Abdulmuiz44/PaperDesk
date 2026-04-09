# PaperDesk

PaperDesk is a privacy-first, local-first Windows desktop app for document workflow automation.

It is designed to help users keep operational documents organized by watching local folders, extracting content, suggesting safe rename/sort actions, indexing documents for offline search, and surfacing duplicates for review.

## Current Status
This repository is now in an **early first vertical slice** state for V1 (beyond pure scaffold).

Implemented today:
- .NET 8 solution and project boundaries (App / Application / Domain / Infrastructure / Tests)
- WPF shell that boots with DI and navigation-ready primary layout
- Core domain model (entities/enums) and safety policy defaults
- Application contracts and starter use-case wiring
- SQLite startup foundation (path resolution + DB open + WAL pragma initialization)
- Infrastructure service registrations with placeholder adapters where behavior is intentionally deferred
- Unit tests for foundational domain/application rules

Deferred / intentionally placeholder:
- Production OCR pipeline and extraction quality tuning
- Real `FileSystemWatcher` event ingestion pipeline
- Persistent CRUD for document repository and unit-of-work transactions
- Full-text indexing, duplicate detection heuristics, and rename execution engine
- Full database schema evolution/migrations and seeded demo workflows
- Installer packaging, diagnostics hardening, and operational polish

## Solution Structure
```text
PaperDesk.sln
src/
  PaperDesk.App              # WPF app shell, DI bootstrap, config loading
  PaperDesk.Application      # Abstractions, DTOs, use-case wiring
  PaperDesk.Domain           # Core entities, enums, safety policies
  PaperDesk.Infrastructure   # SQLite bootstrap + integration adapters (mixed concrete/placeholder)
tests/
  PaperDesk.Tests            # Foundational unit tests
```

## Run Locally
### Prerequisites
- Windows
- .NET 8 SDK

### Build
```bash
dotnet build PaperDesk.sln
```

### Run WPF app
```bash
dotnet run --project src/PaperDesk.App/PaperDesk.App.csproj
```

## Design Guardrails (V1)
- Local-only processing
- No cloud dependencies
- Safe-by-default operations (explicit approval expected)
- Modular architecture with clear boundaries between domain/application/infrastructure/UI
