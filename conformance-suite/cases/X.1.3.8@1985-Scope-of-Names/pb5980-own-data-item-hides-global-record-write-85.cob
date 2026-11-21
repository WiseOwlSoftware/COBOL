      *>--- conformance-case
      *> id: pb5980-own-data-item-hides-global-record-write-85
      *> rules: [1985-GR-X.1.3.8.2-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5980 (review R1-1). A refusal at 85. §1985:X.1.3.8.2 GR3a (cite.py:
      *> OK): "If the name is declared in program B, the object in
      *> program B is the referenced object." The container's FD F is GLOBAL,
      *> so its record FR is a GLOBAL name; the contained program declares
      *> its own data item FR. WRITE FR there names that data item, so it
      *> is no logical record and is refused (COBOL1757); the refusal names
      *> the hiding data item and the GLOBAL record-name it hides, never
      *> that no file description entry has such a record.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5980W.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "p5980w.dat".
       DATA DIVISION.
       FILE SECTION.
       FD  F GLOBAL.
       01  FR  PIC X(3).
       PROCEDURE DIVISION.
       MAIN-1.
           CALL "P5980WJ".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5980WJ.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  FR  PIC X(3) VALUE "DDD".
       PROCEDURE DIVISION.
       B-1.
           WRITE FR.
           EXIT PROGRAM.
       END PROGRAM P5980WJ.
       END PROGRAM P5980W.
