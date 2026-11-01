## Entry 2921 — 2026-11-01 00:45 PDT — PB4237: USE FOR DEBUGGING ON ALL identifier parses at 85, and the debug diagnostics name their line

**The mechanism.** The '85 debug operand `[ALL REFERENCES OF] identifier-1` has one keyword, ALL; REFERENCES and OF
are optional words. `useDebugTarget` (CobolControlFlow.g4) wrote `ALL REFERENCES OF? dataReference`, so
`USE FOR DEBUGGING ON ALL W-A` and `... ON ALL OF W-A` were COBOL5506 at --std 85. ISO/IEC 1989:2023 prints no debug
format (VCR Table 7 row 7.17), so CLAUDE.md rule 1's precedence falls to GnuCOBOL, whose `-std=cobol85` accepts all
four spellings; CCVS DB201A writes `ALL ID-2` and `ALL AB2 OF A2`.

**Re-probe (05fdfc2bd).** The review's `nodbg_*` probes: `ALL W-A` and `ALL OF W-A` drew COBOL5506, the long forms ran
inert. DB201A `--nist`: two COBOL5506 and a 37.4 s compile (the error put it on the slow LL path).

**Fix.** The arm is now `ALL REFERENCES? OF? dataReference`. Its one consumer, `ProcedureTableBuilder.FinalizeDebug`,
told the arm by `REFERENCES()`; with REFERENCES optional that would have sent `ALL W-A` to the bare-name arm, and
`ALL P1` would have silently made paragraph P1 a debug subject. It now reads `ALL()`. The sweep of the same method found
two more defects, fixed here: every COBOL1571 it reports was printed with no file and line (it runs after every walker;
the PB2034 rule), so each operand and the SORT/MERGE phrase now position their diagnostic with `ctx.Edition.At`
(`SortMergeProcedureRanges` carries its phrase for this); and the ALL message did not name its operand, so DB201A's two
ALL operands in one USE collapsed into one diagnostic. After: all five spellings run inert without WITH DEBUGGING MODE;
DB201A compiles in 1.8 s and reports ten positioned COBOL1571 (the staged data and file subjects).

**Determination and goldens.** docs/CONFORMANCE.md D-DEBUG-USE records the choice. Goldens:
`cases/_unassigned/pb4237_debug_all_identifier` (85: DB201A's two USE statements plus the other spellings, inert),
`cases/_unassigned/pb4237-debug-all-identifier-removed-2002` (COBOL0902 from 2002), and
`DebugFacilityTests.AllIdentifier_EverySpelling_At85_IsTheStagedDataItemOperand` (five spellings under WITH DEBUGGING
MODE reach the staged COBOL1571 naming the operand at its line, `ALL P1` included). No inventory row: the debug module
has no ISO 2023 rule.

