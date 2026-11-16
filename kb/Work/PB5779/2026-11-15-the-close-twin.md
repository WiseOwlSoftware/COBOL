**2026-11-15, the CLOSE twin of this decision (the Mythos lane's batch-2 code reads, mechanism 11; source `kb/Work/PB5683/2026-11-15-rulings-code-reads-batch-2.md`). The row is `1985-GR-XIII.4.2.4-2`.**

**Mechanism 11, decided.** The 1985 CLOSE rules sort files into three categories and define the first as a file "whose input or output medium is such that the concepts of rewind and reels/units have no meaning" (`§1985:VII.4.2.4 GR2a`, cite.py OK; the report writer's CLOSE has the same sentence); `§1985:VII.1.3.5` gives status 07 to a CLOSE with NO REWIND, REEL/UNIT or FOR REMOVAL, and to an OPEN with NO REWIND, when "the referenced file is on a non-reel/unit medium". The checker reads the CLOSE preamble ("Treatment of sequential mass storage files is logically equivalent to the treatment of a file on tape") as putting a disk file in the second category, with status 00. I do not: which media have reels, units and a rewind is a fact about the implementation's devices, the standard lists the phrase as hardware dependent, and for the host files this compiler uses those concepts have no meaning. So the files are of the first category, 07 is the conforming status, and the rules for the reel and unit categories have nothing to apply to. That is the same classification kb/Work PB5779 rests on for OPEN; this row joins it, and the one conformance entry for 1985 that PB5779 owes must state the classification for OPEN and CLOSE both, with what GnuCOBOL does measured, not remembered (CLAUDE.md rule 1).

**Rules (each citation checked again with `python scripts/spec/cite.py --check`, verdict OK).**
- `§1985:VII.4.2.4 GR2a`: "whose input or output medium is such that the concepts of rewind and reels/units have no meaning"
  `OK  §1985:VII.4.2.4 GR2a  the text is in GR2a`
- `§1985:XIII.4.2.4`: "Treatment of sequential mass storage files is logically equivalent to the treatment of a file on tape or analogous sequential media"
  `OK  §1985:XIII.4.2.4  the text is in the clause`
- `§1985:XIII.4.2.4 GR2a`: "A file whose output medium is such that the concepts of rewind and reels/units have no meaning"
  `OK  §1985:XIII.4.2.4 GR2a  the text is in GR2a`
- `§1985:VII.1.3.5`: "the referenced file is on a non-reel/unit medium"
  `OK  §1985:VII.1.3.5  the text is in the clause; it lies in GRL2.1d`

**The checker's evidence** (`rulings.json`, mechanism for `1985-GR-XIII.4.2.4-2`; ruling OPEN-READING, harm wrong_answer if reading 1, confidence medium). The compiler places every report file in category (a): CLOSE REEL, CLOSE UNIT FOR REMOVAL and CLOSE WITH NO REWIND each give 07 (same at 2023, where it is right). Reading 1: the 1985 preamble (dropped from 2002 on) puts a disk report file in category (b), so NO REWIND = B,C, REEL = F,G, REEL FOR REMOVAL = D,F,G, all with 00. Reading 2: GR2a defines category (a) by the medium, which a host file meets, and VII.1.3.5's status 07 is defined for "the referenced file is on a non-reel/unit medium", a fact about the medium, which "treatment" does not change. NIST SQ123A expects 07 for a mass storage CLOSE REEL (a net, not authority). Site: `src/WiseOwl.Cobol.Runtime/IO/CloseTable14.cs#PhysicalFileCategory`; `docs/conformance/7/DOC-A.1-24.md`. Existing notes: PB5779 (the OPEN twin; owes the 1985 DOC-A.1-24 entry); PB5256.

**Repro** (`E:/COBOL-coord/scratch/my-20261115/contradictions-b2/src/rcls.cob`):

```cobol
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RCLS.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PF1 ASSIGN TO "RCLS.OUT" FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD PF1 REPORT IS R1.
       WORKING-STORAGE SECTION.
       01 FS PIC XX.
       REPORT SECTION.
       RD R1.
       01 D1 TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X(2) VALUE "DE".
       PROCEDURE DIVISION.
       P0.
           OPEN OUTPUT PF1.
           INITIATE R1.
           GENERATE D1.
           CLOSE PF1 REEL.
           DISPLAY "REEL=" FS.
           CLOSE PF1 UNIT FOR REMOVAL.
           DISPLAY "UNITREM=" FS.
           GENERATE D1.
           TERMINATE R1.
           CLOSE PF1 WITH NO REWIND.
           DISPLAY "NOREW=" FS.
           STOP RUN.
```

**Observed at `--std 85` and at `--std 2023`** (exit 0): `REEL=07 UNITREM=07 NOREW=07`. Under the decision above this conforms: the row `1985-GR-XIII.4.2.4-2` is confirmed as conforming by this note's classification once its conformance entry exists.
