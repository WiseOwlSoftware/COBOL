# Rulings on the top-up slice of read pass 17 (slice 1985-243, the four rows of pull request 1833)

Ruled 2026-11-16 about 18:40 PST by the Mythos lane session (Claude Mythos 5.2). The slice read again, on a compiler
built at main 3aea938236, the four KEPT rows whose fixes landed with pull request 1833 (kb/Work PB5805, PB5857,
PB5876, PB5878). Three agree (reader CONFORMS, refuter UPHELD): `1985-GR-X.5.1-4`, `1985-GR-XIII.4.8.4-5`,
`1985-SR-VII.4.7.3-5`. One does not (`E:/COBOL-coord/scratch/coderead-1985/non-agreeing-243-243.json`). I ran the
programs named below myself, with the tracked probe runner on the same compiler.

| row | reader | refuter | my ruling | note |
|---|---|---|---|---|
| `1985-GR-VIII.2.4.4-8` | CONFORMS | OVERTURNED | UPHELD (b), rejects_legal_source | PB5888 |

## Mechanism 8 (PB5888): RELATIVE KEY naming the same external data item is refused when two programs reach it through differently named groups (every edition)

**Ruling: UPHELD (b), harm rejects_legal_source, MINOR.** Two programs share one EXTERNAL relative file and both
write `RELATIVE KEY IS REL-KEY`. Both describe `01 EXT-KEYS IS EXTERNAL` with `REL-KEY PIC 9(4)` at characters 1 to
4, under a group named `GA` in one program and `GB` in the other. The pair is refused COBOL1575 at `--std 85` and at
`--std 2023`, although the message's own two conditions hold: "the same name at the same character positions of the
external record". The row `1985-GR-VIII.2.4.4-8` does not stand as confirmed. kb/Work PB5857 (landed today) decided
that the same external data item is the same storage and built the comparison; this is a form it refuses that its
own decision admits.

**The text.** The rule asks for the same external data item, and the descriptions of an external record need agree
only in the record-name (and the size); the entries subordinate to it may differ between programs:
- `§1985:VIII.2.4.4 GR8`: "must reference that same external data item in each case"
  `OK  §1985:VIII.2.4.4 GR8  (2.4.4 General Rules)  the text is in GR8`
- `§1985:X.4.5.4 GR2`: "each record-name of the associated record description entries must be the same"
  `OK  §1985:X.4.5.4 GR2  (║4.5.4 General Rules║)  the text is in GR2`

**MEASURED.** `E:/COBOL-coord/scratch/coderead-1985/out/refute-1985-243/r1-group-differ.cob`: exit 65,
`error COBOL1575` at (37,54), at `--std 85` (`E:/COBOL-coord/scratch/my-20261115/b17-check/run/r1-group-differ-001`)
and at `--std 2023` (`r1-group-differ-002`). The control `r1-same-ctrl.cob` (no group between the record and the
key) compiles and prints `B[SECOND-REC]` at `--std 85` (`r1-same-ctrl-001`).
**Where (the refuter's reading of the code, a lead):** `DataItem.ExternalItemIdentity` keys on the whole dotted name
path from the EXTERNAL record (`EXT-KEYS.GA.REL-KEY` against `EXT-KEYS.GB.REL-KEY`) with the offset and width.
**What the fix owes:** the identity is what the message says: the record, the item's character positions and
width, and the item's own name; never the names of the groups between. Every reader of that identity is swept (the
RELATIVE KEY comparison, and whatever else asks whether two programs name the same external item: RECORD KEY and
ALTERNATE RECORD KEY of an external indexed file, the FILE STATUS, PADDING and LINAGE operands if they ask it).
**Not ruled here:** whether an item of ANOTHER name at the same positions is the same external data item (the
refuter's `r1-name-differ.cob` is refused too). kb/Work PB5857 decided the name is part of the identity; the fix
reads that note's reason and leaves the name in unless that reason does not hold.

## Leads that are not yet anything (the slice-243 reader's; not run by me)
- `PROCEDURE DIVISION USING BY REFERENCE ...` compiles at `--std 85`, where the 1985 Procedure Division header has
  no BY phrase (BY VALUE is refused there).
- `WRITE ... ADVANCING ZERO` is admitted at `--std 85` under a 2023 citation; the reader holds it open against
  `§1985:IV.4.2.2.2.3`.
