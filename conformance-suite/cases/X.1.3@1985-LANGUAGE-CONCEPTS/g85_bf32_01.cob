      *>--- conformance-case
      *> id: g85_bf32_01
      *> rules: [1985-GR-X.1.3.6-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.1.3.6 GR3 - a parameter passed by reference
      *> establishes a common data item: the called program refers to
      *> the caller's items under different identifiers (LX, LY, LG).
      *> Leg 1 (line S) fails if the called program does not see the
      *> caller's values; leg 2 (line M) fails if a store made by the
      *> called program through its own identifier is not seen by the
      *> caller, i.e. the item is not common (a copy was passed).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF3201.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WA  PIC X(4) VALUE "AAAA".
       01  WN  PIC 9(3) VALUE 12.
       01  WR.
           05  WR1 PIC X(2) VALUE "RR".
           05  WR2 PIC 9(2) VALUE 5.
       PROCEDURE DIVISION.
       P1.
           CALL "BF32S01" USING WA WN WR.
           DISPLAY "M WA=" WA " WN=" WN " WR=" WR.
           STOP RUN.
       END PROGRAM G85BF3201.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. BF32S01.
       DATA DIVISION.
       LINKAGE SECTION.
       01  LX  PIC X(4).
       01  LY  PIC 9(3).
       01  LG.
           05  LG1 PIC X(2).
           05  LG2 PIC 9(2).
       PROCEDURE DIVISION USING LX LY LG.
       S1.
           DISPLAY "S LX=" LX " LY=" LY " LG1=" LG1 " LG2=" LG2.
           MOVE "XNEW" TO LX.
           ADD 1 TO LY.
           MOVE "GG" TO LG1.
           ADD 10 TO LG2.
           EXIT PROGRAM.
       END PROGRAM BF32S01.
