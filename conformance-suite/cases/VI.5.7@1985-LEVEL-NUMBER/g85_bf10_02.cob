      *>--- conformance-case
      *> id: g85_bf10_02
      *> rules: [1985-GR-VI.5.7.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.7.4 GR3 - "Multiple level 01 entries subordinate to any
      *> given level indicator other than the level indicator RD ... represent
      *> implicit redefinitions of the same area." Two 01 records under one SD.
      *>   SK=CC         : SR-A is moved, the key field SK of SR-B reads it.
      *>   K=AA V=11 ... : records RELEASEd through SR-A are sorted on SK (a
      *>                   field of SR-B) and RETURNed through SR-B's fields.
      *> Each leg fails if the two 01 entries of the SD get separate storage.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF1002.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "bf1002.srt".
       DATA DIVISION.
       FILE SECTION.
       SD  SF.
       01  SR-A                    PIC X(4).
       01  SR-B.
           05  SK                  PIC X(2).
           05  SV                  PIC X(2).
       PROCEDURE DIVISION.
       MAIN-S SECTION.
       M1.
           SORT SF ON ASCENDING KEY SK
               INPUT PROCEDURE IS IN-S
               OUTPUT PROCEDURE IS OUT-S.
           STOP RUN.
       IN-S SECTION.
       I1.
           MOVE "CC33" TO SR-A.
           DISPLAY "SK=" SK.
           RELEASE SR-A.
           MOVE "AA11" TO SR-A.
           RELEASE SR-A.
           MOVE "BB22" TO SR-A.
           RELEASE SR-A.
       OUT-S SECTION.
       O1.
           RETURN SF AT END GO TO O9.
           DISPLAY "K=" SK " V=" SV.
           GO TO O1.
       O9.
           EXIT.
