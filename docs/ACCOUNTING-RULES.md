# Accounting Rules — TM Mobile Shop

This document records the tested business logic that must remain stable during migration.

## 1. Final daily profit

The authoritative daily result is based on actual end-of-day balances:

`Actual Closing Total - Opening/Reference Total + Tanzeem In - Tanzeem Out`

When pending customer liabilities and receivables are added later, they must adjust the accountable opening/closing position rather than being treated as profit.

## 2. Closing is authoritative

Detailed entries are useful, but final profit must still work when some internal JazzCash/Easypaisa/OneLoad movements were not entered.

At Closing, actual balances are entered for:

- Physical drawer cash
- Computer cash
- JazzCash TM Mobile
- Easypaisa Shafqat
- OneLoad

The sum of those actual balances is the Closing Total.

## 3. Tanzeem boundary rule

Tanzeem is outside counted shop assets, so these entries are mandatory:

- **Tanzeem In**: money/value leaves counted shop assets and goes to Tanzeem/external side. Add it back in the profit formula.
- **Tanzeem Out**: Tanzeem/external money/value enters counted shop assets. Subtract it in the profit formula.

Internal transfers among counted shop accounts are not profit.

## 4. Digital account flow

For customer transactions, JazzCash and Easypaisa usually affect the physical drawer as the counter-account.

Examples:

- Easypaisa receives customer money and cash is paid out: Easypaisa increases, drawer decreases.
- Easypaisa sends money and customer gives cash: Easypaisa decreases, drawer increases.
- JazzCash follows the same source/destination logic.
- JazzCash -> Easypaisa internal transfer: no commission and no profit.

## 5. Commission

Principal movement and commission are separate concepts.

Default send commission:

- 100–1,000 => 10
- 1,001–2,000 => 20
- 2,001–3,000 => 30
- Continue +10 for each started 1,000 band.

Default withdrawal commission:

- 100–1,000 => 20
- 1,001–2,000 => 40
- 2,001–3,000 => 60
- Continue +20 for each started 1,000 band.

Manual commission overrides automatic commission; it must never be added twice.

For a Tanzeem customer withdrawal example where 2,040 reaches Tanzeem and commission is 40, only 2,000 physical cash leaves the drawer. Accounting effect: drawer -2,000, Tanzeem In +2,040, profit +40.

## 6. Accessories

Accessory sales increase drawer cash. Returns should reverse the sale rather than silently deleting the original business event. Edit/delete must be available for genuine data-entry mistakes.

## 7. Profit sources

Track known sources separately where possible:

- JazzCash commission
- Easypaisa commission
- Tanzeem commission
- Accessories
- Computer income
- OneLoad known income/commission when a reliable rule is defined
- Unclassified / unexplained remainder

Final Closing Profit remains authoritative even if the source classification is incomplete.

## 8. Rollover

New-day Opening should come from the previous day's actual Closing.

If a previous Closing is corrected later, the next day's Opening must sync to the corrected Closing while preserving the next day's transactions. The correction must be logged in the audit history.

## 9. Audit history

Record material corrections with:

- date/time
- action type
- old value
- new value
- affected day/account/transaction
- automatic rollover syncs

The count of corrections for a day should be visible.
