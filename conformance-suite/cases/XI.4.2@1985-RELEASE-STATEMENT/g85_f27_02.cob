      *>--- conformance-case
      *> id: g85_f27_02
      *> rules: [1985-GR-XI.4.2.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XI.4.2.4 GR1 - each RELEASE releases the record named by
      *> record-name-1 to the initial phase of the sort: the input
      *> procedure builds four records in the sort record area but
      *> RELEASEs only three (the "X" record is built and overwritten
      *> before any RELEASE). The output procedure RETURNs what was
      *> sorted. Fails if a released record is missing, if the
      *> unreleased record appears, or if the content released is not
      *> the record area's content at that RELEASE (COUNT 3 and the
      *> three record lines).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F2702.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "g85f2702s.tmp".
       DATA DIVISION.
       FILE SECTION.
       SD  SF.
       01  SF-REC.
           05  SF-KEY    PIC X.
           05  SF-DATA   PIC X(3).
       WORKING-STORAGE SECTION.
       01  EOF-SW        PIC X VALUE "N".
       01  REC-COUNT     PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-P.
           SORT SF ON ASCENDING KEY SF-KEY
               INPUT PROCEDURE IS IN-P
               OUTPUT PROCEDURE IS OUT-P.
           DISPLAY "COUNT " REC-COUNT.
           STOP RUN.
       IN-P.
           DISPLAY "INPUT".
           MOVE "C" TO SF-KEY.
           MOVE "CCC" TO SF-DATA.
           RELEASE SF-REC.
           MOVE "X" TO SF-KEY.
           MOVE "XXX" TO SF-DATA.
           MOVE "A" TO SF-KEY.
           MOVE "AAA" TO SF-DATA.
           RELEASE SF-REC.
           MOVE "B" TO SF-KEY.
           MOVE "BBB" TO SF-DATA.
           RELEASE SF-REC.
       OUT-P.
           PERFORM UNTIL EOF-SW = "Y"
               RETURN SF
                   AT END MOVE "Y" TO EOF-SW
                   NOT AT END
                       ADD 1 TO REC-COUNT
                       DISPLAY SF-REC
               END-RETURN
           END-PERFORM.
