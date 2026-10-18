## PB870: START FIRST / LAST on a RELATIVE file is a 2002 rule like its INDEXED and SEQUENTIAL twins, and the generator now holds a rule's band to the construct its test is written in

**What the note said.** `GR-14.9.41.4-11` and `-12` (START FIRST / LAST on a relative file) carried editions
2014,2023 while GR18-GR21, the same phrase on indexed and sequential files, carried 2002,2014,2023, and
`constructs.json` dates the one construct (`start-first-last-2002`, COBOLNET0900) at 2002. PB356 had widened the
indexed pair and left these two, deliberately, because it had not tested them.

**Re-probe (this build).** `l1_start_first_last_relative` is refused at `--std 85` with COBOLNET0900 and compiles and
runs, with the same output, at 2002, 2014 and 2023: the compiler and the registry agree and only the inventory bands
were stale. The golden was accepted at 2023 alone because the generator refuses an accepted edition outside a rule's
band, so the wrong band also hid the 2002 and 2014 runs.

**Edge, derived.** The repo holds no 2002 or 2014 text. 2023's Annex E (the 2014 to 2023 delta) names no START change,
so the phrase predates 2023; the 2002-versus-2014 split is not derivable in-repo, so the row takes the provisional 2002
edge (the one that never refuses a legal 2002 program). That derivation had never been written down: it is now
`docs/VERSION_CHANGE_REFERENCE.md` Table 7 row 7.38, in the discipline of rows 7.21 and 7.36, and the construct's `vcr`
field points at it. GnuCOBOL 3.2 accepts the phrase at every `-std`, which is silence and not evidence.

**Fix.** Rows 11 and 12 are 2002,2014,2023 (one `record_verdicts` batch, 15 records: the two band changes plus the new
witnesses on thirteen rows; GAP 66 to 66, nothing was open). The relative golden is accepted at 2002 and 2023 and names
the construct, as do the five other START FIRST / LAST goldens (indexed, suppressed, sequential, line sequential,
`l1_table20_seq_relative`).

**The abnormal and exception arms (owner 2026-10-17: SORT and file I/O tests cover failure as fully as success).** New
`pb870_start_first_last_relative_exceptions`, expected values derived from 14.9.41.4 GR1/GR2/GR5/GR7/GR11/GR12, 9.1.14,
9.1.13.7 6) and 7), 14.9.49.4 GR6/GR7 before any run, then compared: a READ after OPEN of an empty file is 10 (the
control); START FIRST with INVALID KEY takes the phrase and not the USE procedure (23); START LAST with neither
phrase runs the USE procedure and continues (23); the READ after that failed START is 46, not 10 (GR7); START on a file
open OUTPUT and on a closed file is 47, takes neither phrase and runs the USE procedure; START on an absent OPTIONAL
file is 23 (GR5); a file no USE procedure covers simply continues with the status set; the record area survives
(GR2). The run matched the derivation at 2002 and 2023. New negative `pb870-start-first-last-relative-85` (the relative
arm had no refusal below 2002; COBOLNET0900 on both statements).

**The sweep as a structure.** `conformance-suite/harness/generate.py` gained `_check_band_against_constructs`: a test
that names constructs may not witness a rule whose band begins after EVERY one of them unless the row's `notes` name
the construct (the stated difference the note asked for). Self-test arms: a late band is refused, a stated late band
passes. The check would have fired on the original golden (band 2014, construct 2002, accept 2023). Measured over the
repository, `constructs` is declared by only a few tests, so the reach is small today; over the 210 introduction-gate
negatives the only band later than its construct is `AR-15.79.3-4` (the leap-second directive is 2002, the function it
affects 2014), a different mechanism whose notes already state it, left as it is.

**Self-review.** Architecture: the rule lives in the generator, the one reader of test headers, not in a drift test
beside it. Full code: the check keys on `max` of the named constructs' introductions, so a test naming an old and a
new construct is not refused for the new one's later rules; `windows` holds only known constructs so the lookup cannot
miss. Silent failure: the stated-difference escape is a substring of the row's notes, so it cannot be satisfied by an
empty note. Tests: the exception golden's every leg can fail independently (the control against S3 separates 10 from
46). Comments: the golden headers cite only clauses run through `cite.py --check`.

**Review round 1.** The branch reviewer found the campaign requirement (EC-I-O conditions with and without USE and FILE
STATUS) unmet for the checked arm and for the no-FILE-STATUS-no-USE arm. Two goldens now pin them:
`pb870_start_first_last_relative_ecio_checked` (`>>TURN EC-I-O CHECKING ON`: the INVALID KEY phrase runs with
EC-I-O-INVALID-KEY as the last exception status and the USE procedure does not; without the phrase the USE procedure
runs and the statement ends, with and without FILE STATUS; START on a closed file is 47, a fatal EC-I-O-LOGIC-ERROR no
declarative here covers, and 14.6.13.1.3 7) ends the run unit even though the file has a FILE STATUS clause) and
`pb870_start_first_last_relative_nofs_nouse` (checking off, no FILE STATUS, no USE: 23 continues, a NOT INVALID KEY
phrase alone does not run, a 47 ends the run unit per DOC-A.1-103). Both are accepted at 2002 and 2023 and end with the
run unit's abnormal termination, so the expected output stops before the AFTER line of the 47 leg.

**Gate.** See the report; the verdict line is quoted there.
