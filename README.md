# TM Mobile Shop — Dukan Hisab

Private development repository for the TM Mobile Shop accounting application.

## Current working baseline

- **Smart PC v1.20** — PowerShell/WPF implementation used as the verified accounting reference.
- The current production data remains outside this repository and is not overwritten by development builds.
- The next phase is to migrate the tested accounting logic into a proper compiled Windows application while keeping the working PowerShell version intact.

## Core accounting rules

- Final daily profit is based on **actual Closing balances**, not on whether every JazzCash/Easypaisa transaction was entered.
- `Tanzeem In/Out` entries are essential because they cross the shop/external boundary and directly affect profit.
- Internal transfers between shop balances are not profit.
- JazzCash/Easypaisa transaction entry is primarily for running balances, commission tracking, and diagnostics.
- Previous-day Closing corrections must sync into the next day's Opening while preserving later-day transactions.
- Corrections/edits should be auditable.

## Repository layout

- `legacy/powershell-v1.20/` — working reference implementation.
- `docs/` — accounting rules and migration notes.
- Future compiled Windows app source will live separately under `src/` so the reference implementation remains untouched.

> Private repository. Do not commit live customer/account balances, user fonts, or secrets unless intentionally required for a controlled migration.
