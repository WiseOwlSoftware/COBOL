## Entry 3624 — 2026-11-14 14:11 PST — PB5308: the ENTER statement is not provided, so --std 85 refuses it by name (COBOL7240) instead of compiling it to nothing

**Re-probe.** `ENTER FORTRAN SUBRTN.` between two DISPLAYs, `cobol --std 85 --source-format fixed --run`, on this branch's base: exit 0, `BEFORE`, `AFTER`, no diagnostic. The program ran as if the other-language routine had run.

**Decision.** §1985:I.1.5.2.5.5 lets an implementation "include the ENTER statement or not", §1985:VI.6.12.3 SR1 makes language-name-1 "specified by the implementor", ISO 2023 has no ENTER, and GnuCOBOL refuses it, so by CLAUDE.md rule 1 the statement is not provided (docs/conformance/3.7.1, already recorded). It still parses, so the refusal can name it.

**Fix.** `StatementBinder.BindEnter` reports the new COBOL7240 (`DiagnosticCatalog.EnterStatementNotProvided`) and returns a `BoundRejected`. The construct row `enter-removed-2002` becomes a declined row (`declined: COBOL7240`, the PB5007 shape): refused with COBOL7240 at 85, COBOL0902 from 2002. A declined row has no pre-removal meaning for `--permissive` to keep, so `VersionMatrixCatalogue.RemovedMatrix` leaves declined rows out. At 2002 and later the statement draws both COBOL0902 (removed) and COBOL7240 (not provided); under `--permissive` COBOL7240 alone refuses it.

**Removed with it (rule 4, no dead mechanism).** `PlacementRules.EnterEndsSentence` and COBOL6973 (PB5178's period rule for a statement that is now refused whole), `UserWordKind.RoutineName` and the routine-name's census entry, the two `Ansi85AcceptanceTests` ENTER facts, and the goldens `pb5178-enter-not-end-of-sentence`, `pb5178-routine-name-is-paragraph-name` and `pb5178_routine_name_later_verb_word`. The grammar keeps `routineName` so a sentence parses whole and is refused once.

**Goldens.** `1985-FMT-VI_6_12_2` (ENTER COBOL), `1985-SR-VI_6_12_3-1` (ENTER FORTRAN SUBRTN) and `1985-SR-VI_6_12_3-3` (both sentences) are now refusals at 85 (COBOL7240; their `.out` files are gone), and `_unassigned/pb693_enter_after_periodless_move` is a refusal for the periodless-MOVE sentence. VCR Table 7 row 7.16 and docs/conformance/3.7.1 say so.

**Rows.** `1985-SR-VI.6.12.3-1` and `1985-SR-VI.6.12.3-3`: DIVERGES to CONFORMS through `record_verdicts --edition 1985` (GAP 172 to 170). `1985-FMT-VI.6.12.2` and `1985-SR-VI.6.12.3-2` stay PARTIAL and keep prose written against the accepting compiler; they want a re-read.

