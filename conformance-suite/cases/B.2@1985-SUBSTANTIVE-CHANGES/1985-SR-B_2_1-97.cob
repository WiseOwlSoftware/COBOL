      *>--- conformance-case
      *> id: 1985-SR-B_2_1-97
      *> rules: [1985-SR-B.2.1-97]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:B.2.1 SR97 (SUBSTANTIVE CHANGES). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/g-07.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * 1985 B.2.1 item 97: an input/output procedure may GO TO a
      * point outside it, the rest of the Procedure Division may
      * GO TO a point inside it, and INPUT/OUTPUT PROCEDURE may
      * name a paragraph. Self-checking: expects "SRTXFR OK".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SRTXFR.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "SRTXFR.SRT".
       DATA DIVISION.
       FILE SECTION.
       SD  SF.
       01  SR                PIC 9.
       WORKING-STORAGE SECTION.
       01  N                 PIC 9 VALUE 0.
       01  OUT-STR           PIC X(3) VALUE SPACES.
       01  OUT-PTR           PIC 9 VALUE 1.
       01  EOF-SW            PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-PARA.
           SORT SF ON ASCENDING KEY SR
               INPUT PROCEDURE IS IN-PARA
               OUTPUT PROCEDURE IS OUT-PARA.
           IF OUT-STR = "123" AND N = 3
               DISPLAY "SRTXFR OK"
           ELSE
               DISPLAY "SRTXFR BAD " OUT-STR " " N.
           STOP RUN.
       IN-PARA.
           IF N = 0
               MOVE 3 TO SR RELEASE SR
               MOVE 1 TO SR RELEASE SR
               MOVE 1 TO N
               GO TO OUTSIDE-PARA.
           MOVE 2 TO SR. RELEASE SR.
           MOVE 3 TO N.
       OUT-PARA.
           RETURN SF AT END MOVE 1 TO EOF-SW.
           IF EOF-SW = 0
               STRING SR DELIMITED BY SIZE INTO OUT-STR
                   WITH POINTER OUT-PTR
               GO TO OUT-PARA.
       OUTSIDE-PARA.
           GO TO IN-PARA.
