# TM Mobile Shop — Android

Preview 1 foundation.

A separate, offline Android accounting app for TM Mobile Shop. It is intentionally independent of the Windows Dukan Hisab software.

## Core rules
- No live profit during the day.
- Profit is calculated only at Closing.
- Manual Reference can reset a day's opening baseline without changing old history.
- Drawer note counting is only for "دوسرے گلے کی کیش".
- Smart Closing Audit runs only at Closing, not on every entry.
- Smart audit compares calculated vs actual balances and tries x10 / ÷10 alternatives to flag likely missing/extra zero mistakes.
- No floating Messenger-style bubble.

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
