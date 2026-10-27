## PB3838 tier 4: 65 GnuCOBOL defects and ISO departures filed as `upstream` notes

The owner's order (2026-10-27 ~11:25 PDT, "File the surviving GnoCobol records") turned the tier 3 output into the register's
first `upstream` notes. A clerk generated them from the harvest's record files with a script that parses each record's ten
sections and writes one `kb/Work/PB####.md` per family, PB3873 to PB3937, in the order of the survivor list. Every note is
`kind: upstream`, `process_only: true`, `upstream_status: unreported` (the GnuCOBOL tracker has not been searched; tier 6 is
owner-gated), related to PB3838, with the minimal program inline (ours), the exact `cobc` command, the observed result, the
ISO clause numbers as `spec_refs` (each resolved against the transcription's clause list; clauses that exist only in the 2009
draft are named in the prose, not in `spec_refs`), our compiler's result, the fix site in the 3.2 source named by file and
function, the refuter's verdict text and the draft issue. No GnuCOBOL source, testsuite program or expected output is
committed; `work.py check` passes (3184 notes) and the kind keeps all of them out of `work.py next`.

**What the 65 are.** By `upstream_class`: 19 wrong-output, 19 rejects-legal-source, 15 feature-missing, 9
not-implemented-at-run-time, 3 crash. All are edition 2014 (the edition `-std=cobol2014` claims); the adjudicators' longer
edition strings (introduced 2002, and so on) are kept verbatim in each Expected result section.

**The refuters' corrections are in the notes, applied in place** in every section they touch (Summary, Observed, Expected
result, Class, Fix site and the Draft issue's own text). The first filing put them only in a leading Summary paragraph and a
"Corrected before filing" block ahead of the draft, which left the publishable sections asserting what the refuters had
overturned; the round-1 branch review caught it (below). Six RECLASSIFY outcomes: `call-returning-invalid-returning-field`
and its program-RETURNING crash became feature requests to be filed as one issue; `standard-compare` is only the missing
4.2.6 warning, with the 2014 text needing a warning and a refusal not being enough; `occurs-dynamic` zero-size table is unsafe
code for an unimplemented optional element, not a nonconformance; `usage-bit` is a feature request; `rounded-mode` is a
word-list transcription error in `cobol2014.words`, not an ISO violation (its `upstream_class` is the nearest member of the
closed set, `rejects-legal-source`, and says so). The night 6 corrections: both code sites for the table-format VALUES
record, the derived edition for BYTE-LENGTH with a comma, the 23-digit form and TODO 3.2.2 for the over-18-digit BINARY
record, and the overturned latitude reading for the national item size.

**Folds, not separate notes (3).** `picture-implied-from-value-required--object-reference-ice` (S5) and
`universal-object-reference-not-supported--compare-ice` (S9) are further triggers of `cobc-ice-byte-length-tree-c` (PB3874),
which now names all three triggers with their minimal programs, with the sweep corrected (only the comparison with NULL gets an
ordinary error) and the "refuse the SET" fix alternative dropped. The survivor list also carried a third duplicate that the
order did not name, `floating-insertion-edits-untruncated-value--day-to-yyyyddd-argument-zero` (S5, a duplicate of the S3
record), which is folded into PB3889 the same way, so 66 survivors became 65 notes.

**Witnesses** are listed by program id and night, never their text: night 6's S11 adjudication lines (SAME, OTHER-FAMILY,
NEW-MECHANISM, resolved to the record each names, including the `parts` of the split lines) and the night 6 harvest grouping
for the families touched without `needs_judgment`. The ALPHA-TO-BIT (fz02530) and BIT-SMALL (fz04645) witnesses of S11 line 11 are
attached to no note (the refuter ruled them NOT-GNUCOBOL-SIDE).

**Not filed (18 adjudicated records, all with a verdict that is not a GnuCOBOL defect or a duplicate):** 13 NOT-A-DEFECT (the
national display and de-editing mechanisms that are latitude or undefined results, the stale exception status, the
checking-off compile-time refusals, MOVE SPACE to numeric (two records), the floating-insertion TAN case, FLOAT-BINARY-32 and
the NUMVAL-C locale test, the EBCDIC alphabet name); `current-date-empty-argument-list-refused` OVERTURNED by the refuter (`()` is per function in ISO, and CURRENT-DATE's format
has none); `dynamic-length-clause-not-supported` and `occurs-dynamic-not-supported--constant-subscript` reclassified to
implementor latitude (optional elements, 4.2.7); and the two ICE duplicates above.

**Two process defects from the tier 1 review** were filed with them and are fixed in this branch: PB3938 (the GPL size guard
counted only CLOSED backtick fences, so an unclosed or tilde-fenced program passed) and PB3939 (`upstream_findings` documented
a `check_base_agrees` call it never made, so the three `Work.base` arms had no falsifying self-test case). PB3967, which main's
lead clerk filed for the same two leads, lands with them. `work.py` now reads every note body through ONE fence tracker,
`fenced_blocks` (backtick and tilde fences, CommonMark's closing rule, an unclosed block reported and refused), which
`body_sections` and the size guard share in place of the two toggles that drifted; `check_base_agrees` takes the file it
reads, and `upstream_findings` runs it over the shipped `kb/Work.base` and over a copy with each of the kind's three arms
removed. Every new self-test case is killed by a mutant of the rule it guards.

**Bringing main in** conflicted only in `register_problems`, where this branch's merge of pb3941 and pb3941's own final merge
called `frontier_model_shape` and `upstream_problems` in opposite orders; main's order is kept, with main's non-string refusal
in `frontier_model_shape` and this branch's tier 4 draft-title and unapplied-correction guards.

**Where the evidence lives.** Records, repros, the refuters' reruns and the generator (`gen.py`, `corrections.py`) are in
`E:\COBOL-coord\scratch\op\pb3838\` (the generator under `file\`), outside git on one machine; the notes carry the programs and
the text, so the notes are the durable record. The first number allocated to this tier was PB3873; the allocation of 68 ids
(PB3873 to PB3940) left PB3940 unused.

**Round-1 branch review: four findings, all answered in this branch.** R1-1 (blocking): the reclassified notes' Expected
result and Draft issue sections still said what the refuters overturned. Each is rewritten in place: PB3884's draft no longer
calls the default dialect's refusal enough (the 2002 text and the draft toward 2014 require the syntax to be accepted, so
2014 needs a warning), PB3877's draft is the one feature request for both sides of program RETURNING (PB3878 points to it),
PB3917 no longer requires `AAABBBWWW` of an optional element GnuCOBOL does not claim, PB3931's draft drops the conformance
claim for A.3 14) and 9), PB3874's draft carries the `IF R1 = R2` program itself, and PB3924 and PB3933 follow the same
shape. R1-2 (blocking): the corrections inside UPHELD refutations had not been applied at all; they now are (PB3873's count
variant reproduces only with `-fsyntax-only` or `-m`, PB3875/PB3885/PB3886/PB3887 give the draft toward 2014 its own clause
numbers, PB3876 names the 2014 DISPLAY number and the `accept-display-extensions` option, PB3882 states the condition of
15.31.3 1), PB3888 rests its 2014 case on the rules the draft carries and answers 14.7.5 with D.16.4, PB3892/PB3893 report
`-std=cobol2014 --list-intrinsics` as Yes and the DIALECT-ALL flip as the cause, PB3910 lets the CHAR program carry the
claim, PB3913 and PB3922 become one issue, PB3919's fix site drops the PICTURE-precedence table, PB3928 names
`expr_chk_cond` instead of NULL handling). The two defects the refuters found and said to file are folded into the notes
that share their code site, each with a re-run repro, the observed result, a checked citation and our result: `0 ** -1`
raising no size error into PB3886 (`cob_decimal_pow`'s zero-base branch), and TEST-NUMVAL / TEST-NUMVAL-C of `"12.50cr"`
returning 6 into PB3926 (`cob_check_numval`'s `anycase` flag). R1-3: PB3931's draft title no longer wraps. R1-4: PB3935's
summary no longer describes text the draft lacks. Every probe a corrected sentence rests on was re-run on cobc 3.2.0 in WSL
for this round. The root cause, a correction stated beside the text it contradicts, is now refused by `work.py`
(`upstream_problems`): a Draft issue opens with its one-line `**Title:**` and a blank line, and no note says a paragraph
"governs" its other text; four self-test cases prove both arms can fail.
