# Dukan Hisab Next — compiled app status

This branch is the side-by-side compiled Windows replacement project. The current working Smart PC v1.20 remains the accounting reference and is not modified by this app.

## Safety

- Existing `%APPDATA%\DukanHisabSmartPC` files are read-only import sources.
- New development data is written under `%LOCALAPPDATA%\DukanHisabNext`.
- The installer uses a separate application name and install folder.
- v1.20 can remain installed and usable in parallel.

## First parity targets

- Closing is final truth.
- Tanzeem In/Out compensation must match v1.20.
- JazzCash/Easypaisa internal transfers are profit-neutral.
- Commission auto rules and manual override, including explicit zero.
- Previous-day corrected Closing updates next-day Opening safely.
- Transaction edit/delete and Closing corrections create audit entries.

The Windows build workflow runs deterministic regression checks before producing an installer.
