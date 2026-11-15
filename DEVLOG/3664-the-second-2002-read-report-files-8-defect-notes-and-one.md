## Entry 3664 — 2026-11-15 01:16 PST — The second 2002 read report files 8 defect notes and one adjudication

**What was read.** The second pass of the 2002 read (readers and refuters q-21..q-44, 48 files under `E:/COBOL-coord/scratch/op-20261114/read-2002/out`) found 140 defect cases: 8 new groups, 6 extensions of open notes, 3 owner-decision facilities extended, 2 conditional items and 4 dropped groups. Only `kb/Work/` and this entry change.

**New notes (every citation re-checked with `scripts/spec/cite.py --check`).**

- PB5617: an implicitly described CD field carries no PICTURE, so passing it BY REFERENCE or RETURNING into a PIC 9(4) item is refused COBOL0828
- PB5618: EXIT METHOD and EXIT FUNCTION in a GLOBAL declarative accepted
- PB5619: SET pointer UP/DOWN BY lands its amount at scale 6 in an Int128 carrier
- PB5620: a WRITE with its END-OF-PAGE phrases before the retry and lock phrases is a parse error
- PB5621: an ANY LENGTH formal bound to an OCCURS DEPENDING group takes the maximum length
- PB5622: a zero-length argument to REVERSE or UPPER-CASE raises no EC-ARGUMENT-FUNCTION
- PB5623: an omitted parameter as the RETURNING item of a CALL raises no EC-PROGRAM-ARG-OMITTED
- PB5624: a report VARYING counter in a PRESENT WHEN condition is refused as not defined

**Adjudication.** PB5625 holds the two conditional readings: USE AFTER EXCEPTION OBJECT naming a parameterized interface without EXPANDS (§2002:9.3.13), and the class of a USAGE NATIONAL numeric item (§2002:8.5.2.7 against §2002:8.5.2.9). The first pass held the first reading back; it is filed here.

**Extended (rows and a dated file beside each).** PB5604 (17 rows, and the site `CommunicationSectionBinder#BindEntry`), PB5602 (4), PB5575 (25), PB5612 (1), PB5615 (1), PB4950 (10; the file lists the diagnostic codes and the edition each cites). Owner-decision notes PB5577 (4 rows), PB5578 (7), PB5579 (18), status and kind unchanged.

**Dropped.** The VALIDATE, FORMAT/SELECT WHEN and WRITE/REWRITE FILE rows (under the owner-decision notes), four cases that repeat the mechanisms of PB5578, PB5579, PB5602 and PB5604, eight refuter cases upheld with the compiler outcome right, and a CD TYPEDEF lead whose refusal is correct because a type declaration has no storage.

**Correction to the first report's notes.** The `Sites` blocks of PB5598-PB5615 as landed in #1692 cut each path at its first dot after `src/WiseOwl` and printed the rest outside the code span. They are regenerated here with the whole path.

