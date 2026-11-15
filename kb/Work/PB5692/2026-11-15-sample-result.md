**2026-11-15, the PB5692 sample's result (the operator's measurement, 15:20 PST, filed by the registrar landing records-sample-pb5692). Source: `E:/COBOL-coord/scratch/sample-shared/RESULT.md`, with `non-agreeing.json` (the 11 rows, each with its reader record and refuter case), `key.json` (the stored verdicts) and `out/` (every slice's reader and refuter files and repro sources); those paths stay as pointers. The adjudication of its contradictions is the dated file beside this one, `2026-11-15-adjudication-of-the-sample.md`. The mechanisms the sample found are filed as kb/Work PB5731 to PB5738.**

Run wf_f47dc191-edf: 40 agents, no errors, 7.0 M tokens (about 6 points). 120 of 120 rows decided; 116 CONFORMS verdicts
refuted. Frame: 1985 = the 570 KEPT rows whose every 2023 target is witnessed at `--std 85`; 2002 = the 614 stored KEPT rows
whose targets pass the evidence check. Seed 20261115. Files: `in/`, `out/<slice>.json`, `out/<slice>-refute.json`,
`key.json` (stored verdicts), `non-agreeing.json` (the 11 rows below, with the reader's record and the refuter's case).

### Tally
| | 1985 (60) | 2002 (60) |
|---|---|---|
| CONFORMS, refuter UPHELD (agrees with the stored shared verdict) | 54 | 55 |
| reader non-conforming | 2 PARTIAL | 1 PARTIAL, 1 DOCUMENTED-NON-SUPPORT |
| reader CONFORMS, refuter OVERTURNED | 4 | 2 |
| reader CONFORMS, refuter DOUBTFUL | 0 | 1 |
| path consults the edition somewhere | 48 | 44 |
| ... and this edition takes ANOTHER arm than the latest | 38 | 4 |
| reader says the rule's behaviour differs by edition | 15 | 3 |

Seeded against unseeded (anchoring control): 1985 seeded 48 = 42 agree, 6 not; unseeded 12 = 12 agree. 2002 seeded 48 = 44
agree, 4 not; unseeded 12 = 11 agree, 1 not. No sign that seed sites pull readers toward agreement (the unseeded rows
agree at least as often); 24 unseeded rows is a small control.

### The 11 non-agreeing rows, by cause
**(a) the code takes another arm at this edition, and that arm is wrong: 3 rows, all 1985, all found by the REFUTER**
- `1985-GR-XII.2.4-8`: at `--std 85` the nested-COPY gate judges the library text before REPLACING; §1985:XII.2.4 GR12 judges
  the produced text; a COPY that REPLACING removes is refused (COBOL0900). 2023 refuses it on a rule 2023 does state.
  The refuter notes the verdict depends on reading pseudo-text with no period as not a COPY statement.
- `1985-SR-XI.4.4.3-10` and `1985-SR-XI.4.4.3-4` (one mechanism): at `--std 85` a format 3 record holding an OCCURS DEPENDING
  ON table is sized at the table's MINIMUM; §1985 GR15 sizes it at the maximum. Under-rejects one program, refuses two
  legal ones; right at 2023.
**(b) the code fails the rule the same way at every edition: 4 rows (the stored 2023 verdict is wrong too)**
- `1985-GR-IX.2.5.4-5` (reader, PARTIAL): two EXTERNAL describers agreeing on an alternate key: the second OPEN gets 39
  (`FileRegistry.AddAlternateKey` lacks the `::EXT::` guard). Same at 2023. No note found.
- `1985-SR-VI.6.27.3-4` (reader, PARTIAL): UNSTRING COUNT IN / TALLYING IN / WITH POINTER over a reference-modified integer
  item passes the binder and fails in Roslyn (CS1503). Same at 2023. Sibling of PB5384.
- `2002-SR-14.8.21.2-2` (reader, PARTIAL): INSPECT refuses a GROUP-USAGE NATIONAL group operand that GR2b treats as
  elementary. Same at 2023.
- `2002-SR-7.2.13.2-2` (refuter): `>>IF 1 = 1;` (trailing semicolon) compiles; `>>ELSE;` is refused. Same at 2023. Unseeded row.
**(c) the rule covers more in this edition than the stored record assumed: 1 row**
- `1985-SR-VI.5.8.3-1` (refuter): by §1985:III.2 every item subordinate to an ODO entry is a variable occurrence data item,
  so a nested OCCURS under ODO must be refused; accepted at 85 (and at 2023). The refuter records a narrower counter-reading.
**Verdict class, not a contradiction of behaviour: 2 rows (2002 screen facility)**
- `2002-SR-13.16.23.2-2` (reader DNS) and `2002-SR-13.16.31.2-6` (refuter): the rule belongs to the screen section, which
  the compiler declines; the stored shared verdict is CONFORMS where the honest one at 2002 is DOCUMENTED-NON-SUPPORT
  (§2002:B.3 item 1). The compiler's behaviour is not in dispute.
**Open reading: 1 row**
- `2002-GR-11.6.3-4` (refuter DOUBTFUL): METHOD-ID m AS literal, reachable or not through a typed reference; if GR1b/GR4
  govern it is cause (b).

### Against the decision rule you fixed
"No upheld (a) in 120 rows means shared verdicts keep closing rows; any (a) means the rows whose trace shows the edition
consulted need their own adjudication." There are 3 candidate (a) rows (two mechanisms), all at 1985, none at 2002. They
are yours to check before they count. If they stand: at 1985, 48 of 60 sampled rows have a path that consults the edition
and 38 take an arm of their own (63 %); at 2002, 44 consult it and only 4 take another arm (7 %). So the rule as worded
would send about 80 % of 1985's shared rows to their own adjudication, and "takes another arm" would send about 63 % of
1985 and 7 % of 2002. The data supports treating the two editions differently, and filtering on "takes another arm"
rather than "consults".

### A second finding the sample was not built for
Cause (b) rows are wrong at 2023 too, behind a stored CONFORMS: 4 of 120 (3.3 %), 5 if the doubtful one falls, 6 with
cause (c). Applied to 2023's 3,877 CONFORMS rows that is on the order of 130 to 190 rows whose record a code adjudication
with a refuter would overturn. The 2023 inventory's zero is a count of records, not a measured defect rate. I am telling
the owner this figure as an estimate from a sample of 120, with that caveat.

### What I do next unless you say otherwise
1. Nothing is recorded from the sample until you have checked the contradictions (your rule).
2. Then one registrar landing: the dated result file beside PB5692; notes for the mechanisms (ODO sizing of a format 3
   record at 85; nested-COPY gate at 85; AddAlternateKey; UNSTRING reference-modified counters; INSPECT group-usage
   national; `>>IF ...;`; ODO-subordinate OCCURS); the rows re-recorded (the 2023 rows of the cause (b) mechanisms too).
