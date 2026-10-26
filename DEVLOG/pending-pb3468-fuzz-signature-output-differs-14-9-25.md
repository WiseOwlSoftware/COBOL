## PB3468: the fuzz signature OUTPUT_DIFFERS:14.9.25 is not a compiler defect; every row is GnuCOBOL's, the standard's "undefined", or a documented WiseOwl decision, and the figurative-constant receivers get their witness

The note asked whether the differential fuzzer's smoke-run-3 signature `OUTPUT_DIFFERS:14.9.25` ("MOVE of figurative
constants into boolean, alphanumeric and national items") was the compiler's, GnuCOBOL's or neither's. Its sources were
said to be lost; four later fuzz runs under `conformance-suite/results/fuzz-*/` carry the same signature on 17 rows with
their programs. All 17 were re-run on this branch's build against GnuCOBOL 3.2 (the suite's adapters, `--std 2014`), and
each differing line was read against the standard.

**What the re-probe showed.** The note's description was a guess from the seed clause: not one row moves a figurative
constant into a boolean item. The differences fall in six classes, none a WiseOwl defect: HIGH-VALUE is U+FFFF here
(owner decision R52) and U+FFFD after GnuCOBOL's 0xFF (seven rows); items with no VALUE clause displayed before any
store, which the standard leaves undefined (§A.2 19, eight rows); GnuCOBOL's own warning that its USAGE NATIONAL is
unfinished (two rows); non-digit text moved to a numeric item or a bad numeric-edited VALUE, both incompatible data,
undefined by §14.6.13.2 (three rows); an out-of-range subscript, which continues unchecked, and an out-of-range
reference modification, which ends the run unit by the documented determination (two rows); and numeric display forms
recorded before `--numeric-display decimal` (one row that no longer differs at all, and a sign line of another).

**The family, probed directly.** One MOVE for each of 12 figurative constants against alphanumeric, alphabetic,
alphanumeric-edited, national, national-edited and boolean (display and bit) receivers, at 2014 and 2023: the accepted
and refused pairs and every stored value follow Table 16, SR6, SR7, Table 17 and §8.3.3.6.4 GR2 and GR4. The probe found
one more GnuCOBOL divergence: it leaves `MOVE ALL "AB"` unedited in an alphanumeric-edited item (`ABABA` for
`PIC X(2)BX(2)`) though it edits ZERO, SPACE and an ordinary literal; the standard puts the insertion character in its
own position (§13.18.40.5 rule 3, §14.9.25.4 GR6), so WiseOwl's `AB AB` stands.

**The witness.** `cases/14.9.25-MOVE/pb3468_figurative_constant_receivers` (accepted from 2002, free form) moves ZERO,
SPACE, QUOTE, ALL "AB" and ALL B"10" into an alphanumeric, two alphanumeric-edited, a national, a national-edited and
two boolean items, and tests HIGH-VALUE and LOW-VALUE by comparison so that the implementor-defined character is not
printed; its expected lines are derived from the rules, not from a run. Its negative below the introducing edition is
`pb3468-figurative-constant-boolean-below-2002` (COBOL0900 at 85). The compiler is unchanged.

Rows re-verdicted: none (the note claimed none and closes none). `kb/Work/PB3468.md` is `landed` with the per-row
classes. A lead for the fuzz triage: a program that displays storage no statement has written is undefined by §A.2 19
and accounts for eight of these 17 rows, so the triage could set such programs aside before a reader spends time on
them.
