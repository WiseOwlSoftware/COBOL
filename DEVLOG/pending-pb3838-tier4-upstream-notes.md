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

**The refuters' corrections are in the notes**, as a leading paragraph in Summary, a replaced Class section, a corrected draft
title in the front matter and a "Corrected before filing" block at the head of the Draft issue (the record's older text is
kept beneath, labelled as written before the correction). Six RECLASSIFY outcomes: `call-returning-invalid-returning-field`
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

**Two process defects from the tier 1 review** are filed with them: PB3938 (the GPL size guard counts only CLOSED backtick
fences, so an unclosed or tilde-fenced program passes) and PB3939 (`upstream_findings` documents a `check_base_agrees` call it
never makes, so the three `Work.base` arms have no falsifying self-test case). Until PB3938 is fixed, every filed note keeps its
fenced programs closed with backticks and within the 60-line cap.

**Where the evidence lives.** Records, repros, the refuters' reruns and the generator (`gen.py`, `corrections.py`) are in
`E:\COBOL-coord\scratch\op\pb3838\` (the generator under `file\`), outside git on one machine; the notes carry the programs and
the text, so the notes are the durable record. The first number allocated to this tier was PB3873; the allocation of 68 ids
(PB3873 to PB3940) left PB3940 unused.
