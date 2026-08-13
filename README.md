# TM Mobile Shop — Dukan Hisab

Private development repository for the TM Mobile Shop accounting software.

## Current working reference

The proven working desktop version is **Smart PC v1.20**. Its accounting rules remain the reference baseline while the compiled Windows replacement is developed separately.

## Development branches

- `baseline-v1.20` — frozen reference point for the working PowerShell/WPF line.
- `compiled-app-dev` — new compiled .NET/WPF application, installed side-by-side.
- `performance-experiments` — optional isolated experiments; never used for live accounting.

## Accounting principle

Final profit comes from actual Closing balances, adjusted by Tanzeem In/Out. Detailed JazzCash/Easypaisa transactions help with commission/source tracking but are not required for final Closing profit. Tanzeem boundary entries are required.

See `docs/ACCOUNTING-RULES.md`, `docs/MIGRATION-PLAN.md`, and `docs/NEXT-APP-STATUS.md` for the detailed rules and migration plan.
