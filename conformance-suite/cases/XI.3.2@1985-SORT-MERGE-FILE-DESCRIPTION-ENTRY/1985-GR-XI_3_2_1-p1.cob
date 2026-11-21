      *>--- conformance-case
      *> id: 1985-GR-XI_3_2_1-p1
      *> rules: [1985-GR-XI.3.2.1-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XI.3.2.1 p1 (THE SORT-MERGE FILE DESCRIPTION ENTRY). An
      *> output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-17.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * 1985-GR-XI.3.2.1-p1: the SD entry furnishes the physical
      * structure and record-names of a sort file. Two record names
      * (SR-A, SR-B) describe one sort record area; SORT orders by a
      * key in SR-A, RETURN delivers the records through SR-B.
      * Expected: COUNT 3 / CAB / KEYS 1 2 3.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SDS85.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "sds85.tmp".
       DATA DIVISION.
       FILE SECTION.
       SD  SF
           RECORD CONTAINS 4 CHARACTERS
           DATA RECORDS ARE SR-A SR-B.
       01  SR-A.
           02  SA-KEY  PIC 9.
           02  SA-TXT  PIC X(3).
       01  SR-B        PIC X(4).
       WORKING-STORAGE SECTION.
       01  N    PIC 9 VALUE 0.
       01  OUT  PIC X(3) VALUE SPACES.
       01  KS   PIC X(5) VALUE SPACES.
       01  EOF  PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN-P SECTION.
       M1.
           SORT SF ON ASCENDING KEY SA-KEY
               INPUT PROCEDURE IS IN-P
               OUTPUT PROCEDURE IS OUT-P.
           DISPLAY "COUNT " N.
           DISPLAY OUT.
           DISPLAY "KEYS " KS.
           STOP RUN.
       IN-P SECTION.
       I1.
           MOVE "3B  " TO SR-B. RELEASE SR-B.
           MOVE 1 TO SA-KEY. MOVE "C" TO SA-TXT. RELEASE SR-A.
           MOVE "2A  " TO SR-B. RELEASE SR-B.
       OUT-P SECTION.
       O1.
           PERFORM UNTIL EOF = "Y"
               RETURN SF AT END MOVE "Y" TO EOF
               NOT AT END
                   ADD 1 TO N
                   MOVE SR-B(2:1) TO OUT(N:1)
                   MOVE SA-KEY TO KS(N * 2 - 1:1)
               END-RETURN
           END-PERFORM.
