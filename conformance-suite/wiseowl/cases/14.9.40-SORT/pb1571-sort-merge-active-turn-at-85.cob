*>--- conformance-case
*> id: pb1571-sort-merge-active-turn-at-85
*> rules: [GR-14.9.40.4-10]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1571 -- the checked half of ISO 14.9.40.4 GR10 / GR13 (the
      *> EC-SORT-MERGE-ACTIVE order against EC-FLOW-RETURN / EC-FLOW-RELEASE) below
      *> the edition that introduced the exception-checking model: the TURN directive
      *> (7.3.25) and the EC-SORT-MERGE and EC-FLOW exception-names are COBOL-2002, so
      *> a COBOL-85 program cannot enable them; the refusal is the directive's own
      *> edition gate.
      >>TURN EC-SORT-MERGE-ACTIVE EC-FLOW-RETURN EC-FLOW-RELEASE CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1571N85.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "pb1571n85.srt".
       DATA DIVISION.
       FILE SECTION.
       SD SF.
       01 SF-REC PIC X(3).
       PROCEDURE DIVISION.
       MAIN-P.
           SORT SF ON ASCENDING KEY SF-REC
               INPUT PROCEDURE IS FEED OUTPUT PROCEDURE IS TAKE.
           STOP RUN.
       FEED.
           MOVE "QQQ" TO SF-REC.
           RELEASE SF-REC.
       TAKE.
           RETURN SF AT END DISPLAY "AT-END" END-RETURN.
