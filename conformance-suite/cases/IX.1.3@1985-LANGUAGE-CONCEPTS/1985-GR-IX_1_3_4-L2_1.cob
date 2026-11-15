      *>--- conformance-case
      *> id: 1985-GR-IX_1_3_4-L2_1
      *> rules: [1985-GR-IX.1.3.4-L2.1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.1.3.4 GRL2.1 (LANGUAGE CONCEPTS). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the witnesses-85m drafting pass (a
      *> first draft for a 1985 rule no pass had drafted, its
      *> expectation derived from the rule's text and its citations
      *> checked with cite.py) by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. IDXDUP.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IXF ASSIGN TO "IDX85M.DAT"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS IK
               ALTERNATE RECORD KEY IS IA WITH DUPLICATES
               FILE STATUS IS FS1.
           SELECT OPTIONAL IXO ASSIGN TO "IDXN85M.DAT"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS OKY
               FILE STATUS IS FS2.
       DATA DIVISION.
       FILE SECTION.
       FD  IXF.
       01  IXR.
           05  IK PIC X(3).
           05  IA PIC X(2).
       FD  IXO.
       01  IXOR.
           05  OKY PIC X(3).
       WORKING-STORAGE SECTION.
       01  FS1 PIC XX.
       01  FS2 PIC XX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT IXF.
           MOVE "001" TO IK.
           MOVE "AA" TO IA.
           WRITE IXR INVALID KEY DISPLAY "W1 INVALID".
           DISPLAY "W1 " FS1.
           MOVE "002" TO IK.
           WRITE IXR INVALID KEY DISPLAY "W2 INVALID".
           DISPLAY "W2 " FS1.
           MOVE "003" TO IK.
           MOVE "BB" TO IA.
           WRITE IXR INVALID KEY DISPLAY "W3 INVALID".
           DISPLAY "W3 " FS1.
           CLOSE IXF.
           OPEN INPUT IXF.
           MOVE "AA" TO IA.
           READ IXF KEY IS IA INVALID KEY DISPLAY "R1 INVALID".
           DISPLAY "R1 " FS1 " " IK.
           READ IXF NEXT RECORD AT END DISPLAY "R2 END".
           DISPLAY "R2 " FS1 " " IK.
           READ IXF NEXT RECORD AT END DISPLAY "R3 END".
           DISPLAY "R3 " FS1 " " IK.
           CLOSE IXF.
           OPEN I-O IXO.
           DISPLAY "O1 " FS2.
           CLOSE IXO.
           OPEN INPUT IXO.
           DISPLAY "O2 " FS2.
           CLOSE IXO.
           STOP RUN.
