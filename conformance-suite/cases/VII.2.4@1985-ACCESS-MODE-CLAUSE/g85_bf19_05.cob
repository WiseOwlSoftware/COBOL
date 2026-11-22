      *>--- conformance-case
      *> id: g85_bf19_05
      *> rules: [1985-GR-VII.2.4.3-1, 1985-GR-VII.2.4.3-2,
      *>    1985-GR-VII.2.4.1-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.2.4.3 GR1 - no ACCESS MODE clause: sequential access is
      *>   assumed. cite.py: OK §1985:VII.2.4.3 GR1 "If the ACCESS MODE
      *>   clause is not specified, sequential access is assumed".
      *> §1985:VII.2.4.3 GR2 - records are accessed in the order the WRITE
      *>   statements established when the file was created or extended.
      *>   cite.py: OK §1985:VII.2.4.3 GR2 "established by the execution
      *>   of WRITE statements when the file is created or extended".
      *> Derivation: OUTPUT writes S2 then S1, EXTEND writes S0 (positioned
      *>   after the last record, §1985:VII.4.3.4 GR15). Read through SQ,
      *>   which has no ACCESS MODE clause, the records come in WRITE order
      *>   (GR2): SQ:S2, SQ:S1, SQ:S0, then at end SQ:END. Lines 1-4 pin
      *>   GR2 and, since SQ has no ACCESS clause, GR1.
      *>   Lines 5-8 read the same file through SQX, which states ACCESS
      *>   MODE IS SEQUENTIAL: GR1 says the omitted clause means exactly
      *>   this, so the two sequences are equal.
      *> Why it can fail: an EXTEND that rewrites or prepends, a READ order
      *>   other than WRITE order, or a default access other than
      *>   sequential changes lines 1-4 or makes them differ from 5-8.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF1905.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SQ ASSIGN TO "BF1905.DAT"
               FILE STATUS IS SQ-ST.
           SELECT SQX ASSIGN TO "BF1905.DAT"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS SX-ST.
       DATA DIVISION.
       FILE SECTION.
       FD  SQ.
       01  SQ-REC PIC X(2).
       FD  SQX.
       01  SX-REC PIC X(2).
       WORKING-STORAGE SECTION.
       01  SQ-ST PIC XX.
       01  SX-ST PIC XX.
       01  WS-EOF PIC X.
       PROCEDURE DIVISION.
       MAIN-PARA.
      * SQ has no ACCESS MODE clause: sequential access (GR1).
           OPEN OUTPUT SQ.
           MOVE "S2" TO SQ-REC.
           WRITE SQ-REC.
           MOVE "S1" TO SQ-REC.
           WRITE SQ-REC.
           CLOSE SQ.
      * The file is extended (GR2 "or extended").
           OPEN EXTEND SQ.
           MOVE "S0" TO SQ-REC.
           WRITE SQ-REC.
           CLOSE SQ.
           OPEN INPUT SQ.
           MOVE "N" TO WS-EOF.
           PERFORM READ-SQ UNTIL WS-EOF = "Y".
           CLOSE SQ.
      * The same file through an explicit ACCESS MODE SEQUENTIAL.
           OPEN INPUT SQX.
           MOVE "N" TO WS-EOF.
           PERFORM READ-SX UNTIL WS-EOF = "Y".
           CLOSE SQX.
           STOP RUN.
       READ-SQ.
           READ SQ AT END
               MOVE "Y" TO WS-EOF
               DISPLAY "SQ:END".
           IF WS-EOF = "N"
               DISPLAY "SQ:" SQ-REC.
       READ-SX.
           READ SQX AT END
               MOVE "Y" TO WS-EOF
               DISPLAY "SQX:END".
           IF WS-EOF = "N"
               DISPLAY "SQX:" SX-REC.
