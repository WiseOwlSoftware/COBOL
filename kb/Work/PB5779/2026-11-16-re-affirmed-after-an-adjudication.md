# Decision (the Mythos lane, Monday 2026-11-16 00:55 PST): at COBOL-85 the class of a file's storage medium is the implementor's, WiseOwl COBOL's files are on a non-reel/unit medium, and I-O status 07 conforms

This re-affirms the decision of kb/Work PB5779 (2026-11-15) on better grounds, after two refuters and one
adjudicator read the 1985 text the other way. A third refuter was then given the decision and the adjudication
together and asked to break the decision. Its verdict: TEXT ADMITS BOTH. No 1985 sentence forbids an implementor
from classing its mass storage as a non-reel/unit medium, so the classification is the implementor's and the
decision stands as a documented determination
(`E:/COBOL-coord/scratch/my-20261115/unwitnessable/refute2/refute2.json`, its `decision` object, 18 citations, each
`cite.py --check` OK). The strongest point against, in that refuter's words: the sentence that opens both CLOSE
statements and the hardware-dependent list's items that tie these phrases to "a reel or mass storage type of
device"; a reader can argue that answering 07 on every disk file does not implement them. It stops short of
decisive because nothing in the body requires an implementor to divide its mass storage into units.

## The objection
The general rules of CLOSE open, in the Sequential I-O and the Report Writer modules, with: "Treatment of sequential
mass storage files is logically equivalent to the treatment of a file on tape or analogous sequential media"
(`§1985:VII.4.2.4`, `§1985:XIII.4.2.4`). Two refuters and the adjudicator
(`E:/COBOL-coord/scratch/my-20261115/rewind-1985/adjudication.json`) read that as placing every sequential file on
disk in the category "sequential single-reel/unit", so that OPEN and CLOSE with NO REWIND and CLOSE with REEL or
UNIT must complete with 00, where the compiler gives 07 at every edition.

## Why the objection does not carry
1. **The 1985 text says what status 07 is for.** Its list of substantive changes introduces the status with this
   justification: "with mass storage files, these instances of OPEN and CLOSE can be considered successful in
   essence, if the anomaly of the NO REWIND, REEL/UNIT, or FOR REMOVAL phrase is overlooked. The new I-O status value
   of 07 makes successful completion possible, while preserving the information for the user in case he wishes to
   take specific action" (`§1985:B.2.2`, change 40c; cite.py OK on both sentences). If every mass storage file were
   a reel/unit file, no mass storage file could ever receive the status the text says was made for mass storage
   files.
2. **The category is defined by the medium, and the medium is the implementor's.** Status 07 is for a file "on a
   non-reel/unit medium" (`§1985:VII.1.3.5`, cite.py OK). That category is "A file whose input or output medium is
   such that the concepts of rewind and reels/units have no meaning" (`§1985:VII.4.2.4 GR2a`, cite.py OK). A unit is a
   module of mass storage whose dimensions "are determined by each implementor" (the glossary, `§1985:III`), and the
   hardware-dependent list makes the REEL/UNIT, FOR REMOVAL and NO REWIND phrases dependent on the device
   (`§1985:C.3`). An implementation whose mass storage has no unit and no rewind has a non-reel/unit medium.
3. **The preamble's sentence says how a unit-structured mass storage file is treated, not that every mass storage
   file is unit-structured.** It lets one table serve tape reels and mass storage units. It does not define a
   category.
4. **OPEN says the same.** "The NO REWIND phrase will be ignored if it does not apply to the storage medium on which
   the file resides" (`§1985:XIII.4.5.4 GR8`), and the rules for positioning apply only "If the storage medium for
   the file permits rewinding" (`§1985:XIII.4.5.4 GR9`).
5. **A regression net, not authority (CLAUDE.md rule 1), but it shows the reading the 1985 certification applied.**
   The validation suite's program SQ123A (`tests/nist/programs/SQ123A.cob`) is written for an "X-14 SEQUENTIAL
   NON-UNIT MASS STORAGE FILE": it executes CLOSE REEL and requires I-O status 07 with the file left open. Its
   recorded result is nine PASS lines (`tests/nist/valid/SQ123A.txt`).

## The decision
- Which media are reel/unit media is the implementor's classification at 1985. WiseOwl COBOL's determination is
  the one its 2023 register states (`docs/conformance/7/DOC-A.1-24.md`): every physical file is one host path with
  no reel, unit or volume behind it, so it is on a non-reel/unit medium. At `--std 85`, OPEN and CLOSE with NO
  REWIND and CLOSE with REEL, UNIT or FOR REMOVAL complete with 07, and the rules stated for a medium that permits
  rewinding do not apply.
- A 1985 entry in the register states it: `docs/conformance/3/D-MEDIUM-85.md`, with the citations above.
- `1985-GR-XIII.4.5.4-9` then has an antecedent no supported medium populates, and closes on a derivation
  (`unpopulatable-antecedent`) once a refuter has failed to break it under this decision.
- The rows whose cases expect 07 at 85 (`pb141_close_forms`, `pb140_close_status`, `pb317_open_no_rewind_85`,
  `pb1515_io_status_07_open_close_only`) stand.
- Rule 1's order, where the standard leaves the choice: GnuCOBOL. Its runtime source (`libcob/fileio.c` of the
  3.3-dev tree, read by the third refuter and by the deciding session; the compiler was NOT run, it is not installed
  on this host) returns 07 from the close of a sequential disk file for CLOSE WITH NO REWIND and for CLOSE with REEL
  or UNIT, the only two places it sets that status. What it does for OPEN WITH NO REWIND was not read. STILL OWED:
  the three programs of `E:/COBOL-coord/scratch/my-20261115/rewind-1985/src/` run under GnuCOBOL at `-std=cobol85`
  where it is installed. If a run shows a disk file classed as a reel/unit file, this decision is re-opened.

## What the adjudication found that stands whatever the classification
- `1985-GR-XIII.4.5.4-7`'s case accepts 00 or 07, so it pins nothing.
- 2002 dropped the 1985 sentence (`§2002:9.1.9`) and its Annex F does not record the change; the version change
  reference has no row for it.
- What a READ returns after CLOSE UNIT on an input file is not settled by the text.
