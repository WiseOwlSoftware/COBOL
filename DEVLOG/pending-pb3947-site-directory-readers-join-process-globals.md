## Every unit-test class that resolves a locale joins the process-globals collection (PB3947)

`LocaleCollationFailureTests` sets `COBOL_CLDR_DIR` / `COBOL_COLLATION_DIR` (process-wide) to a temporary site directory,
and `CldrLocaleLoaderTests` resolves real locales through the same variables without being in
`[Collection("process-globals")]`, so xUnit could run the two in parallel and one read the other's directory. The re-probe
found the note's pair right and the file paths moved (both live under `Collation/` now). The sweep found nine more
classes in the same position: `CanonicalClosureDriftTests`, `CldrBuildCostTests`, `CldrCollationSettingsTests`,
`CldrIcuCrossCheckTests`, `CollationKeyTests`, `StandardCompareTests`, `LocalePartialStartTests`, `GraphemeBreakerTests`
and `UnicodeNormalizerTests`.

Fix shape: the new `ProcessGlobalCollectionDriftTests` finds every Unit test source that names the two variables (their
constants, read from the runtime) or calls a member of `CldrLocaleLoader`, `CldrTailoringBuilder`, `TailoringRules`,
`LocaleManager` or `CollationEngine`, and requires the collection on it; it was RED on those ten classes first. Each
now carries the attribute. A new reader joins the rule by being written, not by someone remembering a list.

No inventory row (test infrastructure); `closes_rows: []` stands. No diagnostic code used (COBOL6664-COBOL6666 returned).
