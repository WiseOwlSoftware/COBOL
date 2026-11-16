**2026-11-15, the sequential module's row joins this question (the Mythos lane's batch-7 code reads, mechanism 1; source `kb/Work/PB5683/2026-11-15-rulings-code-reads-batch-7.md`).**

**Ruling: NOT DECIDED, harm wrong_answer under one reading.** The row `1985-GR-VII.4.6.4-4` is the question of this note in the sequential module: a containing program's USE GLOBAL on the file against the contained program's own USE ON INPUT (`INNER-INPUT 35`). The sequential text matches the indexed text word for word, so it settles nothing more. The row stays undecided with this note. The checker's confidence: high.

**The checker's evidence.** The probe `p1rg` (sequential FA IS GLOBAL; the container's USE GLOBAL ... ON FA; the contained INNER1's USE ... ON INPUT; OPEN INPUT of an absent file in INNER1) prints `INNER-INPUT 35` and `INNER RESUMED 35` at `--std 85` and at `--std 2023`: exactly this note's arrangement and observation. The sequential text is word for word the indexed text this note weighed (GR4, GR5b, SR3 as corrected by Amendment 2 "within the same Procedure Division", X.5.5.4 GR1), so the sequential module's own text does not decide it where the indexed text did not.

**Where** (the checker's reading): `src/WiseOwl.Cobol.Compiler/CodeGen/UseTierEmitter.cs#EmitScopeTiers` (relevant under this note's reading 1 only).

**Row added.** `1985-GR-VII.4.6.4-4`. `work.py check` refuses a 1985 rule id in `inventory_rows` (kb/Work PB5094), so the row is named here. The 2023 rows the checker names for the same question: `GR-14.9.49.4-5`, `GR-14.9.49.4-6`, `GR-14.9.49.4-3`.

**Rules (each citation checked again with `python scripts/spec/cite.py --check`, verdict OK).**
- `§1985:VII.4.6.4 GR4`: "When file-name-1 is specified explicitly, no other USE statement applies to file-name-1"
  `OK  §1985:VII.4.6.4 GR4  (4.6.4 General Rules)  the text is in GR4`
- `§1985:VII.4.6.4 GR5b`: "except those files referenced by file-name-1 in another USE statement specifying the same condition"
  `OK  §1985:VII.4.6.4 GR5b  (4.6.4 General Rules)  the text is in GR5b`
- `§1985:VII.4.6.3 SR3`: "The same file-name must not appear in more than one USE AFTER EXCEPTION statement within the same Procedure Division"
  `OK  §1985:VII.4.6.3 SR3  (4.6.3 Syntax Rules)  the text is in SR3`

**Repro** (`E:/COBOL-coord/scratch/my-20261115/contradictions-b7/src/p1rg.cob`, `--std 85`: exit 0, `INNER-INPUT 35` / `INNER RESUMED 35`; `p1rg23.cob` at `--std 2023` prints the same):

```cobol
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P1RG.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FA ASSIGN TO "p1rg-absent.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD FA IS GLOBAL LABEL RECORDS ARE STANDARD.
       01 FA-R PIC X(4).
       WORKING-STORAGE SECTION.
       01 FS PIC XX IS GLOBAL.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-FA SECTION.
           USE GLOBAL AFTER STANDARD ERROR PROCEDURE ON FA.
       D1.
           DISPLAY "OUTER-NAMED-GLOBAL " FS.
       END DECLARATIVES.
       M SECTION.
       P.
           CALL "INNER1".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. INNER1.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-IN SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON INPUT.
       D2.
           DISPLAY "INNER-INPUT " FS.
       END DECLARATIVES.
       M2 SECTION.
       P2.
           OPEN INPUT FA.
           DISPLAY "INNER RESUMED " FS.
           EXIT PROGRAM.
       END PROGRAM INNER1.
       END PROGRAM P1RG.
```
