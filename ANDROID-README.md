# TM Mobile Shop — Android

Preview foundation.

A separate, offline Android accounting app for TM Mobile Shop. It is intentionally independent of the Windows Dukan Hisab software.

## Core rules
- No transaction-based live profit during the day; the Home closing amounts show the result instantly once all required values are entered.
- Final profit is saved at Closing.
- Manual Reference can reset a day's opening baseline without changing old history.
- Drawer note counting is only for "دوسرے گلے کی کیش".
- Smart Closing Audit runs only at Closing, not on every entry.
- Smart audit compares calculated vs actual balances and tries x10 / ÷10 alternatives to flag likely missing/extra zero mistakes.
- No floating Messenger-style bubble.
- Computer is treated as the current day's computer income for profit presentation, not as a wallet whose profit is closing minus opening.
- Previous day's closing Computer amount moves into the next day's Drawer opening, and the next day's Computer starts at 0.
- Total profit = shop profit + current day's Computer amount.

## Current Android accounting regression
- 18-08-2026 test case: shop profit 3,894; Computer income 2,300; total profit 6,194.
- Next-day rule remains: previous Computer amount joins Drawer; next-day Computer starts at 0.

## Accounts
- دوسرے گلے کی کیش
- کمپیوٹر
- JazzCash TM Mobile
- Easypaisa Shafaqat
- Easypaisa Tanzeem
- OneLoad
- Shop ↔ Tanzeem movements
- Mobile accessories

## Android compatibility
- minSdk 27 (Android 8.1)
- Offline state stored in native SharedPreferences through a WebView bridge.
- Release APK uses a stable private signing key in CI so future previews can upgrade over the previous build.
