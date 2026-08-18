      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB70 - the receiving chokepoint. A data reference that RESOLVES to a declared item but whose
      *> shape COBOL.NET does not implement as a receiver used to be dropped from the receiver list by
      *> .OfType<Place>() - `MOVE "Z" TO OK1 TB(2:1) OK2` moved into OK1 and OK2 and silently skipped TB.
      *> The chokepoint now reports an undiagnosed null (COBOLNET0899, recognized-not-implemented) so the
      *> compilation fails instead of the statement running one receiver short. The shape here: a level-66
      *> RENAMES span over a BINARY leaf as a receiver (13.18.45 - legal; the composed image codec over a
      *> typed-numeric leaf is a later slice, so it stages).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB70NRECV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 A PIC X(2) VALUE "AB".
          05 B PIC 9(4) COMP VALUE 7.
       66 RN RENAMES A THRU B.
       01 OK1 PIC X.
       PROCEDURE DIVISION.
           MOVE "X" TO OK1 RN.
           STOP RUN.
