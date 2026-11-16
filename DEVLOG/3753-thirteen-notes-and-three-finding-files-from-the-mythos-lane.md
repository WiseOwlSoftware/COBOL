## Entry 3753 — 2026-11-16 05:00 PST — Thirteen notes and three finding files from the Mythos lane's batch-10 rulings of 2026-11-16

Filed in kb/Work (documents only, no compiler source, no case, no inventory record). Each defect note carries the rule text with its `cite.py --check` line, the repro program in full, expected and observed, and a Sites block.

- PB5844 (defect, MINOR): the RECORD DELIMITER clause on a fixed-length file compiles and any word is accepted as its implementor-name though none is defined (every edition); decided: one implementor-name for the native framing, every other word refused, and a 1985 register entry owed with the fix.
- PB5845 (defect, MAJOR): at `--std 85` an OPEN refused with a 9x status (LINAGE of zero) leaves the file open; a second OPEN, a WRITE and the CLOSE answer 41, 90 and 00 where 1985 gives 48 and 42.
- PB5846 (defect, MAJOR): a Linkage Section item that is not in the USING phrase is accepted as a subscript or a reference-modification operand (every edition).
- PB5847 (defect, MAJOR): the implicit CLOSE of a CANCEL, and of an INITIAL program's EXIT PROGRAM, never stores an I-O status into the FILE STATUS item (every edition).
- PB5848 (defect, MINOR): a COPY text-name is never checked against the reserved words; `COPY LENGTH.` and `COPY ADD.` compile (every edition).
- PB5849 (adjudication, open): is "the same usage" of the USAGE rule the alternative as written (BINARY over COMP refused) or the manner of representation (the data-model design's choice)?
- PB5850 (defect, MINOR): at `--std 85` JUSTIFIED, and BLANK WHEN ZERO, on a report group entry that has subordinates compiles; 2023 refuses it.
- PB5851 (defect, MAJOR): a group VALUE is silently dropped when a subordinate item carries REDEFINES (every edition); the code path that drops it is a lead nobody has checked.
- PB5852 (defect, MAJOR): at `--std 85` a MOVE whose truncated result is zero stores a positive sign where the 1985 rule places the sending operand's sign.
- PB5853 (adjudication, open): does a report with no PAGE clause have a line 0, so that a first group beginning LINE PLUS 0 ends with LINE-COUNTER 2 and not 3?
- PB5854 (defect, process only): the code-read store does not learn that a contradicted row's defect was fixed; decided: `plan` treats a contradicted read whose note is landed as owed a re-read.
- PB5855 (defect, process only): `scripts/semgrep/verify.py` fails locally on "unreadable files" whose number varies by run.
- PB5839 (defect, MINOR): an unparseable WISEOWL_COBOL_CLOCK is silently ignored and the run uses the real clock; the Mythos lane set `silent` and `misleads_diagnosis`.

Three dated finding files beside existing notes: `kb/Work/PB5781/2026-11-16-the-rem-format-row.md` (the row `1989-FMT-A.2.37.2`), `kb/Work/PB5826/2026-11-16-two-more-shapes.md` (`1 = A OR 2` and `IF 1 = 2` only warn), and the ruling copied unchanged as `kb/Work/PB5683/2026-11-16-rulings-code-reads-batch-10.md`.

**The population is read.** The ruling counts 97 of 1,282 rows of the 1985 code reads contradicted (7.6 %), from the rulings files by script.

