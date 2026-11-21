      *>--- conformance-case
      *> id: pb5980-own-data-item-hides-global-file-open-85
      *> rules: [1985-GR-X.1.3.8.2-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5980. A refusal at 85. §1985:X.1.3.8.2 GR3a (cite.py:
      *> OK): "If the name is declared in program B, the object in
      *> program B is the referenced object." The container declares
      *> file F GLOBAL; the contained program declares its own data
      *> item F. There F names that data item, so OPEN OUTPUT F names
      *> no file and is refused (COBOL1639). The refusal says what F
      *> names here, with its line, and that it hides the container's
      *> GLOBAL file-name; it never says that no SELECT gives the
      *> name, which one does. A compiler that opens the container's
      *> file, or whose refusal sends the programmer to a file
      *> description that exists, fails.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5980O.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "p5980o.dat".
       DATA DIVISION.
       FILE SECTION.
       FD  F GLOBAL.
       01  FR  PIC X(3).
       PROCEDURE DIVISION.
       MAIN-1.
           CALL "P5980I".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5980I.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  F   PIC X VALUE "D".
       PROCEDURE DIVISION.
       B-1.
           OPEN OUTPUT F.
           EXIT PROGRAM.
       END PROGRAM P5980I.
       END PROGRAM P5980O.
