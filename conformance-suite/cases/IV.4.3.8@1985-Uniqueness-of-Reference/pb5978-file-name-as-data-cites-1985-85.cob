      *>--- conformance-case
      *> id: pb5978-file-name-as-data-cites-1985-85
      *> rules: [1985-SR-IV.4.3.8.1-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5978. A refusal at 85. §1985:IV.4.3.8 (cite.py:
      *> OK): "a statement in a COBOL program must contain a reference
      *> which uniquely identifies that resource". FX is a file-name
      *> (§1985:VII.2.3, the file control entry), so MOVE FX TO Y
      *> names no data item and is refused (COBOL1639). The refusal
      *> says what FX is declared as and cites the clause of the
      *> edition the program is compiled at: §1985:VII.2.3, never the
      *> COBOL-2023 file control clause. A compiler that accepts the
      *> MOVE, or a refusal citing another edition's clause, fails.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5978F.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FX ASSIGN TO "p5978f.dat".
       DATA DIVISION.
       FILE SECTION.
       FD FX.
       01 FX-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01 Y PIC X(10).
       PROCEDURE DIVISION.
       P-1.
           MOVE FX TO Y
           STOP RUN.
