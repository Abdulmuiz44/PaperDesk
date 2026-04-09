# PaperDesk

PaperDesk is a privacy-first, local-first Windows desktop app for document workflow automation.

It is designed to help users keep operational documents organized by watching local folders, extracting content, suggesting safe rename/sort actions, indexing documents for offline search, and surfacing duplicates for review.

## Current Status
This repository currently contains the **initial architecture scaffold** for V1.

Implemented in scaffold form:
- .NET 8 solution and project structure
- WPF shell application with a clean navigation-ready layout
- Domain entities and enums for core workflow concepts
- Application interfaces for OCR, watcher, renaming, indexing, duplicates, logging, and persistence
- Infrastructure placeholders for SQLite bootstrap, configuration, logging, and service adapters
- Unit test project with foundational tests

Not implemented yet:
- Real OCR engine integration
- Real file system watcher behavior
- Real indexing, duplicate detection, and rename execution logic
- Full database schema/migrations
- Installer packaging and production hardening

## Solution Structure
```text
PaperDesk.sln
src/
  PaperDesk.App              # WPF app shell, DI bootstrap, config loading
  PaperDesk.Application      # Abstractions, DTOs, use-case scaffolding
  PaperDesk.Domain           # Core entities, enums, safety policies
  PaperDesk.Infrastructure   # SQLite foundation + integration placeholders
tests/
  PaperDesk.Tests            # Unit test scaffold
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
