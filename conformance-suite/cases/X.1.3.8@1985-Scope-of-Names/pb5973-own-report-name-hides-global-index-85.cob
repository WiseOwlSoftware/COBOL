      *>--- conformance-case
      *> id: pb5973-own-report-name-hides-global-index-85
      *> rules: [1985-GR-X.1.3.8.2-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5973.
      *> §1985:X.1.3.8 (cite.py: OK): "a program's reference to such a
      *> name, even when it is a different type of user-defined word,
      *> is to the object which that program describes rather than to
      *> the object, possessing the same name, described in another
      *> program"; §1985:X.1.3.8.2 GR3a (cite.py: OK): "If the name is
      *> declared in program B, the object in program B is the
      *> referenced object."
      *> The container declares a GLOBAL table INDEXED BY IX; the
      *> contained program declares its own RD IX. There IX names that
      *> report, so SET IX TO 2 names no index and is refused
      *> (COBOL1639). A compiler that ranks an index-name only against
      *> data-names and condition-names sets the container's index.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P73XO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T GLOBAL.
          05 E PIC X OCCURS 3 INDEXED BY IX.
       PROCEDURE DIVISION.
       P-1.
           CALL "P73XI"
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P73XI.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPF ASSIGN TO "p73x.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPF REPORT IS IX.
       REPORT SECTION.
       RD IX.
       01 DET TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X VALUE "D".
       PROCEDURE DIVISION.
       I-1.
           SET IX TO 2
           EXIT PROGRAM.
       END PROGRAM P73XI.
       END PROGRAM P73XO.
