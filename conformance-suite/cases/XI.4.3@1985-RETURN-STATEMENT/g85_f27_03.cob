      *>--- conformance-case
      *> id: g85_f27_03
      *> rules: [1985-GR-XI.4.3.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XI.4.3.4 GR1 - the two record descriptions of the sort
      *> file share one storage area, an implicit redefinition: each
      *> record is RELEASEd through SR-A (alphanumeric, 6 positions)
      *> and, after each RETURN, read through SR-B (a 2-character key
      *> and a 4-digit unsigned number over the same 6 positions).
      *> Both descriptions are 6 positions long, so no item lies beyond
      *> the current record and nothing undefined is displayed. Fails if
      *> SR-B does not see the bytes RETURN placed in the area (the
      *> SB-K and SB-N columns) or if SB-N is not that numeric view
      *> (the last column, SB-N + 1).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F2703.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "g85f2703s.tmp".
       DATA DIVISION.
       FILE SECTION.
       SD  SF.
       01  SR-A          PIC X(6).
       01  SR-B.
           05  SB-K      PIC X(2).
           05  SB-N      PIC 9(4).
       WORKING-STORAGE SECTION.
       01  EOF-SW        PIC X VALUE "N".
       01  PLUS1         PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-P.
           SORT SF ON ASCENDING KEY SB-K
               INPUT PROCEDURE IS IN-P
               OUTPUT PROCEDURE IS OUT-P.
           STOP RUN.
       IN-P.
           MOVE "QQ0041" TO SR-A.
           RELEASE SR-A.
           MOVE "PP1234" TO SR-A.
           RELEASE SR-A.
       OUT-P.
           PERFORM UNTIL EOF-SW = "Y"
               RETURN SF
                   AT END MOVE "Y" TO EOF-SW
                   NOT AT END
                       COMPUTE PLUS1 = SB-N + 1
                       DISPLAY SR-A " " SB-K " " SB-N " " PLUS1
               END-RETURN
           END-PERFORM.
