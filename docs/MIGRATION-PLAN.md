# Migration Plan — Compiled Windows Application

## Goal

Build a proper installable Windows application without changing or endangering the current working Smart PC PowerShell version.

## Safety rules

1. Keep the PowerShell/WPF v1.20 implementation frozen as the reference baseline.
2. Do not write new development data into the existing `%APPDATA%\DukanHisabSmartPC` folder until migration is explicitly tested.
3. New compiled builds should use a separate app/data folder during development.
4. Import/copy existing data; do not mutate the original files during testing.
5. Compare daily totals, Tanzeem totals, commission totals, Closing profit, rollover, and audit history against the reference app.

## Proposed application structure

- `src/DukanHisab.App/` — Windows UI application
- `src/DukanHisab.Core/` — accounting engine and domain models
- `src/DukanHisab.Storage/` — local JSON/data persistence and migration
- `tests/DukanHisab.Tests/` — accounting regression tests
- `installer/` — installer project/scripts

## Migration stages

### Stage 1 — Freeze and test the accounting engine

Convert the existing tested rules into deterministic unit tests before rebuilding the UI.

Priority regression cases:

- Internal JazzCash/Easypaisa transfer => profit 0
- Tanzeem -> shop funding => Tanzeem Out compensation => profit 0
- Shop -> Tanzeem transfer => Tanzeem In compensation => profit 0
- Manual commission overrides auto commission
- 2,040 customer/Tanzeem example => drawer -2,000 and profit +40
- Actual Closing overrides incomplete detailed transaction tracking
- Previous Closing correction syncs next Opening without deleting transactions
- Edit/delete/correction audit history

### Stage 2 — Data migration

Read existing Smart PC daily JSON/TXT files in read-only mode and import a copy into the new application's development storage.

### Stage 3 — Windows UI

Rebuild the current workflow with fast section switching and Urdu-first layout. Avoid rebuilding pages on each navigation click.

### Stage 4 — Installer

Produce a normal Windows installer and Start Menu/Desktop shortcut. Development installer must remain side-by-side with Smart PC until parity is verified.

### Stage 5 — Parallel testing

Use both applications on copied data and compare results for several real days before considering replacement of the working version.
