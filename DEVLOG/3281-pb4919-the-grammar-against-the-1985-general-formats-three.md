## Entry 3281 — 2026-11-07 16:05 PST — PB4919: the grammar against the 1985 general formats: three clauses take their optional words, two 2002 relaxations are gated at 85

kb/Work PB4919 asked for a decision on each of the 36 candidates that `audit_grammar_optional_words.py --edition 1985`
reports over the 213 decided 1985 formats. Each was read from its rendered 1985 page and probed with a witness compiled
at `--std 85` and `--std 2023`. Five mechanisms were real defects; they account for 17 of the 36 lines.

**Legal 1985 source the grammar refused.** The MEMORY SIZE clause: §1985:VI.4.4.2 underlines MEMORY and not SIZE, and
`MEMORY 8 WORDS` drew COBOL1970; `memorySizeClause` writes `SIZE?` and `memorySizeAhead` accepts MEMORY before SIZE or
an integer. The RERUN clause: §1985:VII.2.12.2 underlines neither EVERY nor either OF, and `RERUN ON X 100 RECORDS OF F`,
`... RECORDS F` and `END REEL F` drew COBOL5506. The same page shares one unbracketed `OF file-name-2` between the
REEL/UNIT and RECORDS phrases, so the old `integer-1 RECORDS [OF file-name-2]` also accepted a RECORDS phrase with no
file. That arm keeps the file optional in the grammar, as MULTIPLE FILE does, and `FileControlBinder.ScreenRerunClause`
refuses the omission by name with the new COBOL6496 at every edition. USE FOR DEBUGGING: §1985:XV.3.2.2 underlines USE
and DEBUGGING only, so the rule writes `FOR?`.

**1985 restrictions that 2002 lifted, which compiled clean at 85.** USE AFTER: §1985:VII.4.6.2 underlines AFTER. The
2002 post-ballot text (14.8.45.1 Format 1) does not, though Annex F.2 names only PROCEDURE. SET Format 4:
§1985:VI.6.23.2 has no outer ellipsis, and the 2002 text (14.8.35.1) adds it. Two construct rows,
`use-without-after-2002` and `set-condition-repeated-2002`, are gated in `VersionConformancePass.ParseArm`
(`VisitUseStatement` beside the PROCEDURE gate, and `VisitSetBooleanStatement` once per statement), with VCR row 7.52.
`pb450_set_outer_repetition` had claimed the repetition at 85, so it now accepts 2002, 2014 and 2023.

**The other 19 lines need no change.** Two are edition gates the audit cannot see (PERFORM BY, USE PROCEDURE). Three
come from a rule whose other alternative already allows the omission (SYMBOLIC CHARACTERS IS/ARE, VALUE OF IS, PAGE
LINE/LINES). Four put the underlined word inside a bracket the grammar also makes optional (EVALUATE ALSO, SIGN SEPARATE,
EXIT PROGRAM, and RERUN ON in the Relative and Indexed subsets). One anchors INVALID KEY's KEY to `readKey`. PB4919
records each decision.

Goldens: `pb4919_memory_without_size_85`, `pb4919_rerun_optional_words_85`, `pb4919-rerun-records-without-file`
(COBOL6496 at all four editions), `pb4919_use_debugging_without_for_85`, `pb4919-use-without-after-at-85` and
`pb4919-set-condition-repeated-at-85`. No inventory row is closed: no 1985 row exists yet, and the 1985 audit (PB4886)
creates them.

