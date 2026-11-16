      *>--- conformance-case
      *> id: g2_read_at_end_transfer
      *> rules: [2002-GR-14.8.29.3-21]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.29.3 GR21 - the at end condition, each arm:
      *> a) "is set to '10' to indicate the at end condition".
      *> b) AT END written, F has a USE procedure: imperative-stmt-1
      *>    runs ("AE 10"), "Any USE AFTER EXCEPTION procedure ... is
      *>    not executed" (no USE line), then control goes to the end
      *>    of the READ and NOT AT END is ignored (no NAE line).
      *> c) no AT END, F has a USE procedure: "that procedure is
      *>    executed. Control is then transferred to the end of the
      *>    READ statement" ("USE 10"); NOT AT END ignored.
      *> d) no AT END, G has no USE procedure: "control is transferred
      *>    to the end of the READ statement. The NOT AT END phrase is
      *>    ignored if it is specified" ("G=10", no NAE line).
      *> R1 is the control: a successful read runs NOT AT END and no
      *> USE procedure (§2002:9.1.12: none for successful completion).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RD21.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "g2rd21f.dat"
               ORGANIZATION SEQUENTIAL FILE STATUS S.
           SELECT G ASSIGN TO "g2rd21g.dat"
               ORGANIZATION SEQUENTIAL FILE STATUS T.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 R PIC X.
       FD G.
       01 Q PIC X.
       WORKING-STORAGE SECTION.
       01 S PIC XX.
       01 T PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON F.
       D1-P.
           DISPLAY "USE " S.
       END DECLARATIVES.
       M SECTION.
       MAIN.
           OPEN OUTPUT F. MOVE "A" TO R. WRITE R. CLOSE F.
           OPEN OUTPUT G. CLOSE G.
           OPEN INPUT F.
           READ F AT END DISPLAY "AE-WRONG"
                  NOT AT END DISPLAY "R1 " S END-READ.
           READ F AT END DISPLAY "AE " S
                  NOT AT END DISPLAY "NAE-WRONG" END-READ.
           DISPLAY "AFTER-B".
           CLOSE F.
           OPEN INPUT F.
           READ F.
           READ F NOT AT END DISPLAY "NAE-WRONG-C" END-READ.
           DISPLAY "AFTER-C".
           CLOSE F.
           OPEN INPUT G.
           READ G NOT AT END DISPLAY "NAE-WRONG-D" END-READ.
           DISPLAY "G=" T.
           DISPLAY "AFTER-D".
           CLOSE G.
           STOP RUN.
