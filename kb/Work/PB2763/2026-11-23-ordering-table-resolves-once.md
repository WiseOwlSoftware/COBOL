# PB2763, 2026-11-23: STANDARD-COMPARE's ordering table and level resolve once (wave 20262147 group B)

**Re-probe (base a23ed881c2 plus the w20262139d replay).** The finding is a static allocation count, so the re-probe is the
code read: `CollationEngine.TryGetOrderingTable` still ran `IsDefaultOrderingTableName` (two `Canon` passes, one on the
constant), `IsKnownLocale` (two `CldrLocaleTag.Parse` passes) and `ResolveLocale` on every call; `StandardAtLevel` reached
`Cached`, which built a `CollationOptions` per call; `StandardCompare` trimmed each operand through the `string` overload of
`LocaleCollation.TrimForLocale` (a substring per space-padded operand); and `CobolIntrinsics` kept its own
`s_orderingCollators` cache for a tailored table beside the engine's.

**Fixed (the finding `runtime-text-1--performance--f1#2`).**
- `CollationEngine.TryGetOrderingTable` memoizes its answer per literal (`s_orderingTables`, the table or null for "not
  supported"); `ClearLocaleCache` forgets it with the locales. EC-ORDER-NOT-SUPPORTED is still raised at every call for an
  unsupported name or level: the memo stores the "no", not the raise.
- The default table's canonical name is a static field (`s_defaultOrderingTableCanon`).
- `CollationEngine.OrderingAtLevel(table, level)` replaces `StandardAtLevel(level)` and STANDARD-COMPARE's own
  `s_orderingCollators`: one per-(table, level) cache of shifted collators for every ordering table, the root table's level 4
  being `Standard` itself. One mechanism where there were two.
- `StandardCompare` passes `TrimForLocale(a.AsSpan())` to `Collator.Compare(ReadOnlySpan, ReadOnlySpan)`.

Witnesses: `CollationEngineTests.OrderingTableAndLevel_ResolveOncePerName` (same table instance per literal, same collator
per pair, `Standard` at root level 4, re-resolution after `ClearLocaleCache`); the golden
`12.3.7-SPECIAL-NAMES/pb101_standard_compare` output unchanged (run_probe `EXPECT: MATCH`). Inventory: RV-15.85.4-5's
code-location re-sited from `#StandardAtLevel` to `#OrderingAtLevel` (record_verdicts, GAP 2 -> 2). Runtime public API:
`StandardAtLevel` removed, `OrderingAtLevel` added, inside this branch's runtime-major 34 (`abi_pin.py repin`).

**Still open (why the note is `half`): the merged locale-identification findings.** The "Also found as" list
(`runtime-control-1--architecture--f2#2`, `runtime-text-1--architecture--f1#2`, `runtime-text-1--duplication--f1#1`,
`runtime-text-1--duplication--f1#2`, `runtime-text-1--performance--f1#1`, `runtime-text-2--duplication--f1#1`) is one
other mechanism: a locale tag's canonical form and fallback chain are written in several places
(`LocaleIdentification.Normalize`, `LocaleManager.Normalize`, `CollationEngine.ResolveLocale`'s own trim/replace,
`CldrLocaleTag`), and a LOCALE collating sequence re-resolves its locale per `Compare`/`KeyOf`. Untouched: it is a
re-design of the runtime's locale-identification layer (one tag type outside `Collation.Cldr`, one cache key), not a
per-call memo.
