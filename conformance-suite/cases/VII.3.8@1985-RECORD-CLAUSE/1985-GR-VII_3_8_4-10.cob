      *>--- conformance-case
      *> id: 1985-GR-VII_3_8_4-10
      *> rules: [1985-GR-VII.3.8.4-10]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.3.8.4 GR10 (THE RECORD CLAUSE). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the witnesses-85m drafting pass (a
      *> first draft for a 1985 rule no pass had drafted, its
      *> expectation derived from the rule's text and its citations
      *> checked with cite.py) by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RECVAR.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT VF ASSIGN TO "VAR85M.DAT" FILE STATUS IS FS1.
           SELECT CW ASSIGN TO "ODO85M.DAT" FILE STATUS IS FS2.
           SELECT CR ASSIGN TO "ODO85M.DAT" FILE STATUS IS FS3.
       DATA DIVISION.
       FILE SECTION.
       FD  VF
           RECORD IS VARYING IN SIZE FROM 5 TO 20 CHARACTERS
               DEPENDING ON L1.
       01  VR PIC X(20).
       FD  CW
           RECORD IS VARYING IN SIZE FROM 3 TO 12 CHARACTERS.
       01  CWR.
           05  CN PIC 99.
           05  CE PIC X OCCURS 1 TO 10 TIMES DEPENDING ON CN.
       FD  CR
           RECORD IS VARYING IN SIZE FROM 3 TO 12 CHARACTERS
               DEPENDING ON L3.
       01  CRR PIC X(12).
       WORKING-STORAGE SECTION.
       01  L1 PIC 99.
       01  L3 PIC 99.
       01  FS1 PIC XX.
       01  FS2 PIC XX.
       01  FS3 PIC XX.
       01  EOF PIC X VALUE "N".
       PROCEDURE DIVISION.
       DECLARATIVES.
       VF-ERR SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON VF.
       VF-ERR-PARA.
           DISPLAY "USE " FS1.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-PARA.
           OPEN OUTPUT VF.
           MOVE ALL "A" TO VR.
           MOVE 10 TO L1.
           WRITE VR.
           DISPLAY "W1 " FS1.
           MOVE ALL "B" TO VR.
           MOVE 20 TO L1.
           WRITE VR.
           DISPLAY "W2 " FS1.
           CLOSE VF.
           OPEN INPUT VF.
           PERFORM READ-PARA UNTIL EOF = "Y".
           CLOSE VF.
           OPEN OUTPUT CW.
           MOVE 4 TO CN.
           MOVE "W" TO CE (1).
           MOVE "X" TO CE (2).
           MOVE "Y" TO CE (3).
           MOVE "Z" TO CE (4).
           WRITE CWR.
           DISPLAY "C1 " FS2.
           CLOSE CW.
           OPEN INPUT CR.
           READ CR AT END DISPLAY "C2 END".
           DISPLAY "C2 " FS3 " " L3 " " CRR (1:L3).
           CLOSE CR.
           OPEN OUTPUT VF.
           MOVE 3 TO L1.
           WRITE VR.
           STOP RUN.
       READ-PARA.
           READ VF AT END MOVE "Y" TO EOF.
           IF EOF = "N" DISPLAY "R " L1 " " VR (1:L1).
