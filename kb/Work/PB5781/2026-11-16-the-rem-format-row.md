**2026-11-16, the format row of FUNCTION REM joins this mechanism (the Mythos lane's batch-10 code reads, mechanism 9; source `kb/Work/PB5683/2026-11-16-rulings-code-reads-batch-10.md`).**

**Ruling: UPHELD (b), harm rejects_legal_source.** The row `1989-FMT-A.2.37.2` (the format of REM) is the mechanism of this note: `FUNCTION REM (A (- B))` is refused, because an identifier followed by a parenthesised arithmetic expression in a function argument list is parsed as a subscript (COBOL2096). The checker's confidence: high. Same at `--std 2023` (cause b).

**The checker's evidence (rulings.json, copied).** A second argument that begins with a unary minus must be written `(- B)` (`§1985:VI.6.2.3 SR4` says "it must be immediately preceded by a left parenthesis", cite.py OK, and the check matched SR4), and an argument may be an arithmetic expression (`§1985:IV.4.3.8.3.3 SR1`, "Argument-1 must be an identifier, a literal, or an arithmetic expression", cite.py OK). A is not a table, so no subscript reading exists. One program of the checker's puts `FUNCTION REM (A (- B))` beside this note's own `FUNCTION MIN (A (- B))`: both draw the identical COBOL2096 "a subscript is written on A" at 85 and 2023, REM additionally COBOL1504 (1 argument) and at 85 COBOL0900. Same site as this note (`src/WiseOwl.Cobol.Frontend/Grammar/Core/CobolExpressions.g4#functionArgList`). This note's `inventory_rows` name only `1985-SR-VI.6.2.3-4`; this format row should be added to it.

**Rows added.** `1989-FMT-A.2.37.2`. **2023 row** the checker names: `FMT-15.77.2`.

**Where:** `src/WiseOwl.Cobol.Frontend/Grammar/Core/CobolExpressions.g4#functionArgList`, as this note.

**Rules (each citation checked again with `python scripts/spec/cite.py --check`, verdict OK).**
- `§1985:VI.6.2.3 SR4`: "it must be immediately preceded by a left parenthesis"
  `OK  §1985:VI.6.2.3 SR4  (║6.2.3 Formation and Evaluation Rules║)  the text is in SR4`
- `§1985:IV.4.3.8.3.3 SR1`: "Argument-1 must be an identifier, a literal, or an arithmetic expression"
  `OK  §1985:IV.4.3.8.3.3 SR1  (4.3.8.3.3 Syntax Rules)  the text is in SR1`

**Repro** (`E:/COBOL-coord/scratch/my-20261115/contradictions-b10/src/rem.cob`; A and B are elementary):

```cobol
       IDENTIFICATION DIVISION.
       PROGRAM-ID. REMP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC S9 VALUE 7.
       01 B PIC S9 VALUE 2.
       01 R PIC S9V99.
       PROCEDURE DIVISION.
       P0.
           COMPUTE R = FUNCTION REM (A (- B)).
           DISPLAY R.
           COMPUTE R = FUNCTION MIN (A (- B)).
           DISPLAY R.
           STOP RUN.
```

**Output at `--std 85`** (exit 65): COBOL2096 on both lines (a subscript is written on `A`, whose entry has no OCCURS; the message's citations are elided here), COBOL1504 for REM (`FUNCTION REM takes 2 argument(s); 1 given`), and COBOL0900 on both (a subscript that is not a COBOL-85 positive integer, integer data-name or index-name, each optionally followed by + or - an unsigned nonzero integer requires COBOL-2002). **Output at `--std 2023`** (exit 65): COBOL2096 on both lines and COBOL1504 for REM.

**Expected:** compiles (the argument list is `A` and `(- B)`), as this note's `FUNCTION MIN` case should. **Observed:** refused, as above.
