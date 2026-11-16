      *>--- conformance-case
      *> id: g2-same-as-excluded-clauses
      *> rules: [2002-GR-13.16.47.3-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.47.3 GR1 - "The effect of the SAME AS clause is as
      *> though the data description identified by data-name-1 had been
      *> coded in place of the SAME AS clause, excluding the level-number,
      *> name, and the EXTERNAL, GLOBAL, REDEFINES, and SELECT WHEN clauses
      *> specified for data-name-1". Expected lines, from that text:
      *>   BASEA=BASE RD1=BASE S1=ZZZZ - S1 SAME AS RD1 (RD1 REDEFINES
      *>     BASEA) takes RD1's PIC X(4) but not its REDEFINES, so S1 has
      *>     storage of its own: MOVE "ZZZZ" TO S1 leaves BASEA and RD1;
      *>   IN-EXT1=EEEE IN-GLB1=GLOB - the nested program sees the external
      *>     EXT1 and the global GLB1 (the controls for the next two legs);
      *>   EXT1=IIII S2=QQQQ - S2 SAME AS EXT1 is not EXTERNAL: the nested
      *>     program's 01 S2 EXTERNAL is another item, so its MOVE "PPPP"
      *>     leaves the outer S2 at QQQQ, while its MOVE "IIII" to the
      *>     external EXT1 reaches the outer EXT1;
      *>   S3=GLOB GRP=GLOB! - S3 SAME AS GLB1 and the level-5 S5 SAME AS
      *>     GLB1 take GLB1's PIC X(4) VALUE "GLOB" (the VALUE clause is not
      *>     excluded) but keep their own level-numbers, and S5 is not
      *>     GLOBAL (GLOBAL is admitted only at level 1, so the entry is
      *>     legal only if it is excluded); that S3 is not GLOBAL is
      *>     refused in g2-same-as-global-excluded;
      *>   LEN-S=05 - REC-S SAME AS REC-A (REC-A SELECT WHEN OTHER) is REC-A's
      *>     five-character group without the SELECT WHEN clause; F2's other
      *>     entry REC-T has none, which §2002:13.16.49.2 SR2 admits only if
      *>     REC-S has none either (the compiler refuses the mixed form,
      *>     COBOL7343), so F2 compiles only if SELECT WHEN is excluded.
      *> A leg fails if an excluded clause is carried to the subject, or if
      *> the description (picture, size, VALUE) is not.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SAX01.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "G2SAX01A.DAT".
           SELECT F2 ASSIGN TO "G2SAX01B.DAT".
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       01  REC-A SELECT WHEN OTHER.
           05 A-KEY PIC X.
           05 A-TXT PIC X(4).
       FD  F2.
       01  REC-S SAME AS REC-A.
       01  REC-T PIC X(5).
       WORKING-STORAGE SECTION.
       01  N PIC 99.
       01  BASEA PIC X(4) VALUE "BASE".
       01  RD1 REDEFINES BASEA PIC X(4).
       01  EXT1 PIC X(4) EXTERNAL.
       01  GLB1 PIC X(4) GLOBAL VALUE "GLOB".
       01  S1 SAME AS RD1.
       01  S2 SAME AS EXT1.
       01  S3 SAME AS GLB1.
       01  GRP.
           05 S5 SAME AS GLB1.
           05 S6 PIC X VALUE "!".
       PROCEDURE DIVISION.
           MOVE "ZZZZ" TO S1
           DISPLAY "BASEA=" BASEA " RD1=" RD1 " S1=" S1
           MOVE "EEEE" TO EXT1
           MOVE "QQQQ" TO S2
           CALL "G2SAX1N"
           DISPLAY "EXT1=" EXT1 " S2=" S2
           DISPLAY "S3=" S3 " GRP=" GRP
           MOVE FUNCTION LENGTH(REC-S) TO N
           DISPLAY "LEN-S=" N
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SAX1N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  EXT1 PIC X(4) EXTERNAL.
       01  S2 PIC X(4) EXTERNAL.
       PROCEDURE DIVISION.
           DISPLAY "IN-EXT1=" EXT1 " IN-GLB1=" GLB1
           MOVE "IIII" TO EXT1
           MOVE "PPPP" TO S2
           GOBACK.
       END PROGRAM G2SAX1N.
       END PROGRAM G2SAX01.
