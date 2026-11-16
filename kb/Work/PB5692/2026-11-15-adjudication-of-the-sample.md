**2026-11-15 15:35 PST, the Mythos lane: the three cause (a) rows of the PB5692 sample are UPHELD, and the question the note asks is decided per edition.**

The sample is the operator's (`E:/COBOL-coord/scratch/sample-shared/RESULT.md`: 60 KEPT rows of 1985 and 60 of 2002,
each code-adjudicated blind at its own edition and refuted). The decision rule was fixed before it ran: no upheld
contradiction of kind (a), where the code takes another arm at the row's edition and that arm is wrong, means a shared
verdict keeps closing a row; any (a) means it does not. Every candidate (a) row came to this session before it counted.
Each was derived from the 1985 text first, then run on a build of main be075d028b
(`E:/COBOL-wt/gl1985-tree-a/src/WiseOwl.Cobol.Cli/bin/Debug/net10.0/cobol.exe`); every citation below was run through
`python scripts/spec/cite.py --check` with verdict OK. Probes and runs: `E:/COBOL-coord/scratch/my-20261115/pb5692/`.

## Mechanism 1: at `--std 85` a format 3 file's record that holds an OCCURS DEPENDING ON table is sized at the table's minimum (rows `1985-SR-XI.4.4.3-10`, `1985-SR-XI.4.4.3-4`): UPHELD, cause (a)

The text. `§1985:VII.3.8.4 GR15`, under the heading FORMAT 3: "The size of a record is determined by the sum of the
number of characters in all fixed length elementary items plus the sum of the maximum number of characters in any
variable length item subordinate to the record". `§1985:VII.3.8.4 GR14`: "the size of each data record is completely
defined in the record description entry". The minimum-occurrence summation is Format 2's (`§1985:VII.3.8.4 GR5a`: "The
minimum number of table elements described in the record is used in the summation"). Whether format 3 gives fixed or
variable records is the implementor's (`§1985:VII.3.8.4 GR13`); the compiler treats it as variable at 85, as its own
messages below show. `§1985:XI.4.4.3 SR10`: the SD's records "must not be less than the smallest record nor larger than
the largest record described for file-name-3". `§1985:XI.4.4.3 SR4f`: the keys lie within the first x positions, "where
x equals the minimum record size specified for the file referenced by file-name-1".

Derived, then observed (`cobol.exe <file> --std <n> --run`):

| Probe | By the 1985 text | `--std 85` | `--std 2023` |
|---|---|---|---|
| `r4-f3odo-clean.cob`: GIVING file `RECORD CONTAINS 8 TO 9` over a record of 1 + (1 to 7) and one of 9; SD fixed 5 | G1's records are 8 and 9; 5 is less than the smallest: refuse (SR10) | `RAN` (under-rejects) | COBOL1757 |
| `r4-sdf3odo-clean.cob`: SD `RECORD CONTAINS 6 TO 7` over a record of 1 + (1 to 5) and one of 7; GIVING `VARYING 3 TO 8` | the SD's records are 6 and 7, inside 3 to 8: accept | COBOL1757 "'SW' describes variable-length records of 2 to 7 bytes" (refuses legal source) | `RAN` |
| `r5-f3odo-key.cob`: the same SD, key at positions 3 to 4 | x = 6: accept (SR4f) | COBOL0874 "minimum size 2" (refuses legal source) | `RAN` |

The arm: below 2002 `VaryingRecordInfo.BoundsEnforced` is false for a format 3 clause, and `FileModel.VaryMin` falls to
the record descriptions' minimum with the table at its minimum occurrences; from 2002 integer-4 and integer-5 bound
the record. The later arm is right for its text; the 1985 arm is wrong for 1985's. One mechanism, two rows, harm both
ways. Sibling to sweep when it is fixed: every consumer of `VaryMin` / `MinRecordSize` for a format 3 file at 85
(SORT USING `§1985:XI.4.4.3 SR3`, MERGE, the record-size checks of WRITE and RELEASE).

## Mechanism 2: at `--std 85` the nested-COPY gate judges the library text before REPLACING (row `1985-GR-XII.2.4-8`): UPHELD, cause (a), on a probe that does not depend on the refuter's disputed reading

The text. `§1985:XII.2.4 GR12`: "The text produced as a result of the complete processing of a COPY statement must
not contain a COPY statement". `§1985:XII.2.4 GR8`: "The syntactic correctness of the library text cannot be
independently determined". 2002 moved the rule onto the library text (`§2002:7.1.2.3 GR11`: "If the REPLACING phrase is
specified, the library text shall not contain a COPY statement"), so the two editions differ exactly here.

The refuter's probe (`c4/r4a.cob`: library text `COPY ZZZ.`, `REPLACING ==COPY ZZZ== BY ==MOVE 3 TO X==`) needs the
word COPY inside pseudo-text-1, and the compiler refuses that at every edition (COBOL2451, on the sentence 1985 also
has, `§1985:XII.2.3 SR7`: "a COPY statement must not occur within a COPY statement"). Whether text words `COPY ZZZ` with
no period are "a COPY statement" is a reading question (`§1985:XII.2.3 SR2`, `SR6`), and I do not decide it here,
because a second probe needs no such reading:

- `c4h/AUTHZ.cpy` is `PLACEHOLDER. COPY ZZZ.`; `c4h/r4h.cob` has, in the Identification Division,
  `COPY AUTHZ REPLACING ==PLACEHOLDER== BY ==AUTHOR==.`
- By the text: the produced text is `AUTHOR. COPY ZZZ.`, an AUTHOR paragraph whose comment-entry holds the word COPY,
  and `§1985:XII.2.3 SR10` says "If the word COPY appears in a comment-entry or in the place where a comment-entry may
  appear, it is considered part of the comment-entry". The produced text holds no COPY statement; GR12 is met; accept.
- Control `c4h/r4i.cob` writes the produced text directly (`AUTHOR. COPY ZZZ.`): `--std 85` prints `X=3`. So the
  compiler agrees that this text holds no COPY statement.
- `r4h.cob` at `--std 85`: `error COBOL0900: library text that contains a COPY statement requires COBOL-2002
  (targeting COBOL-85) — COPY AUTHZ — §1985:XII.2.4 GR12`, then `CBL3620: COPY copybook 'ZZZ' not found` (it also
  expands the inner COPY). Refuses legal source. At `--std 2002` and `--std 2023` the refusal is COBOL1640 on
  2002's library-text rule, which those editions do state.

The arm: `CopyProcessor.ResolveOneCopy` tests the library text for a COPY statement before the replacements are
applied; below 2002 that raises `Constructs.CopyNestedLibraryText2002` under GR12, whose subject is the produced text
(kb/Work PB5126 introduced the 85 gate). The defect's home row is GR12's own, `1985-GR-XII.2.4-12`, which today
carries an OWN verdict CONFORMS (`tests/version-matrix/inventory-1985/1985-GR-XII.2.4-12.json`, relation CHANGED): an
adjudicated verdict that this probe overturns, so it is re-recorded non-conforming naming the mechanism's note. GR8's
row (`1985-GR-XII.2.4-8`, KEPT, no verdict of its own) is PARTIAL for the same mechanism. The COBOL2451 question (is the word COPY inside pseudo-text barred at 1985) is a
separate, open reading, the same at every edition, and belongs in its own note if it is filed at all.

## What the sample decides (kb/Work PB5692)

1. **1985: a shared verdict does not close a row.** Three of 60 sampled rows (two mechanisms) are wrong at `--std 85`
   behind a CONFORMS inherited from their 2023 targets, all in rows whose path takes an arm of its own at 85, and 38
   of 60 rows have such an arm. Every KEPT row of 1985 therefore needs a verdict adjudicated at 1985: a code read at
   `--std 85` with a refuter, as the sample did. The filter "only rows whose path takes another arm" is not usable in
   advance: the arm is found by the read (in mechanism 1 it sits in `BindRecordClause`, which the stored
   code-location does not name), so the read is the filter. The sample's own 60 rows are adjudicated by it.
2. **2002: a shared verdict keeps closing a row, shown as shared.** No cause (a) in 60 rows, and 4 of 60 take another
   arm. Zero of 60 leaves an upper bound of about 5 % at 95 % confidence; that is the stated risk, and decision
   PB5679's D3 (never present a shared verdict as adjudicated) is what keeps it visible. The two 2002 screen-section
   rows are a verdict-class error (DOCUMENTED-NON-SUPPORT recorded as CONFORMS), found mechanically from the declined
   facility, not by a code read: re-record them, and sweep the stored 2002 KEPT rows for rules of a facility the
   compiler declines at 2002.
3. **The editions are judged separately** because cause (a) needs an edition arm on the rule's path, and the sample
   measured that frequency: 63 % at 1985, 7 % at 2002.
4. **Not decided here, and the owner is told:** the cause (b) rows (4 of 120 wrong at every edition behind a stored
   2023 CONFORMS) say the 2023 inventory's zero is a count of records. The operator's estimate from them is 130 to 190
   of 3,877 rows. A sample of 120 drawn for another purpose is thin for that figure; a stratified sample of 2023's
   own rows comes before any decision to re-read 2023.

## Who does what next
- The operator's registrar landing carries its RESULT.md and this file beside kb/Work PB5692, the mechanism notes
  (the two above, each claiming its rows; the four cause (b) mechanisms; the cause (c) row), and the re-recorded rows.
- 1985 is the Mythos lane's on account 1: the witnesses (kb/Work PB5730 and the gl1985 lane) and then the code
  adjudication of the 510 KEPT rows that are witnessed today and not in the sample, with the sample's own pipeline and
  briefs; rows that gain a witness join that queue. At the sample's rate (120 rows, about 6 points) the 510 rows cost
  about 25 points.
- 2002 and the fix loop stay the operator's.
