      *>--- conformance-case
      *> id: pb5980-own-data-item-hides-global-file-read-85
      *> rules: [1985-GR-X.1.3.8.2-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5980. A refusal at 85. §1985:X.1.3.8.2 GR3a (cite.py:
      *> OK): "If the name is declared in program B, the object in
      *> program B is the referenced object." The container declares
      *> file F GLOBAL; the contained program declares its own data
      *> item F. READ F there names that data item, so the statement
      *> names no file and is refused (COBOL1639). The refusal names
      *> the hiding data item and the GLOBAL file-name it hides, as
      *> the OPEN of the same name does (the record verbs share the
      *> file-name resolution with OPEN). A compiler whose READ
      *> refusal says no SELECT gives the name fails.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5980R.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "p5980r.dat".
       DATA DIVISION.
       FILE SECTION.
       FD  F GLOBAL.
       01  FR  PIC X(3).
       PROCEDURE DIVISION.
       MAIN-1.
           CALL "P5980J".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5980J.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  F   PIC X VALUE "D".
       PROCEDURE DIVISION.
       B-1.
           READ F AT END CONTINUE
           END-READ.
           EXIT PROGRAM.
       END PROGRAM P5980J.
       END PROGRAM P5980R.
