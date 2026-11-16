      *>--- conformance-case
      *> id: pb5628-release-in-contained-program-outside-range-85
      *> rules: [1985-SR-XI.4.2.3-2, 1985-GR-XI.4.4.4-7]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5628 - the control of pb5628_release_in_called_contained_program_85. §1985:XI.4.2.3 SR2:
      *> "A RELEASE statement may be used only within the range of an input procedure associated with a
      *> SORT statement for the file-name whose sort-merge file description entry contains record-name-1."
      *> The contained program P3N releases the GLOBAL record of SORT-A, but the only CALL of P3N is in the
      *> main body, after the SORT, so no CALL in an input procedure's range reaches it (§1985:XI.4.4.4 GR7).
      *> Expected: COBOL7081 at 85 at that RELEASE. Fails if every contained program is taken into the range.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5628R.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SORT-A ASSIGN TO "PB5628RA".
           SELECT SRT-OUT ASSIGN TO "PB5628RO".
       DATA DIVISION.
       FILE SECTION.
       SD  SORT-A.
       01  REC-A IS GLOBAL.
           05  KEY-A PIC X(3).
       FD  SRT-OUT.
       01  SRT-R PIC X(3).
       PROCEDURE DIVISION.
       M SECTION.
       M1. SORT SORT-A ON ASCENDING KEY KEY-A INPUT PROCEDURE IS IN-P
               GIVING SRT-OUT.
           CALL "P3N".
           STOP RUN.
       IN-P SECTION.
       I1. MOVE "AAA" TO REC-A. RELEASE REC-A.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P3N.
       PROCEDURE DIVISION.
       N1. MOVE "GGG" TO REC-A. RELEASE REC-A.
           EXIT PROGRAM.
       END PROGRAM P3N.
       END PROGRAM PB5628R.
