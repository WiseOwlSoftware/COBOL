**A second symptom of this root (wave 20262135 group F, PB3201 report, New defects item 1).** In a fresh worktree
`census.measure` builds Debug only, `Tests.Unit` references the analyzer with `SetConfiguration=Release`, and
`bin/Release` has no analyzer dll at all (not a stale one, as above). The host then dies in
`tools/ArchCensus/Reachability.cs:313` (`StrongestUseAsync`) with "Unexpected value UnresolvedAnalyzerReference" instead of
a refusal. Repro: `python scripts/arch/census.py --commit HEAD --work <dir>` at 608ccf507c (log
`E:\COBOL-coord\scratch\w20262135\w20262135f\census-before.log`). The root fix here (the census builds the Release analyzer
it loads, or refuses naming the missing file and the rebuild command) covers it; the fix's test needs an absent-dll arm
beside the stale-dll arm, and `Reachability.cs:313` must refuse loudly on an unresolved analyzer reference. PB3201's
landing note names the trap and points here.
