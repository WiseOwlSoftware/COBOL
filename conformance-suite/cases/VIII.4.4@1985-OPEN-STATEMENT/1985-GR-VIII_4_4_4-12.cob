      *>--- conformance-case
      *> id: 1985-GR-VIII_4_4_4-12
      *> rules: [1985-GR-VIII.4.4.4-12]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.4.4 GR12 (THE OPEN STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WRONG original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. EXTREL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RFILE ASSIGN TO DISK1
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  RFILE
           LABEL RECORDS ARE STANDARD.
       01  RREC PIC X(4).
       WORKING-STORAGE SECTION.
       01  FS PIC XX.
       01  EOF-FLAG PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RFILE.
           MOVE "REC1" TO RREC.
           WRITE RREC
               INVALID KEY DISPLAY "WRITE FAILED".
           MOVE "REC2" TO RREC.
           WRITE RREC
               INVALID KEY DISPLAY "WRITE FAILED".
           CLOSE RFILE.
           OPEN EXTEND RFILE.
           MOVE "REC3" TO RREC.
           WRITE RREC
               INVALID KEY DISPLAY "WRITE FAILED".
           CLOSE RFILE.
           OPEN INPUT RFILE.
           PERFORM READ-PARA UNTIL EOF-FLAG = "Y".
           CLOSE RFILE.
           STOP RUN.
       READ-PARA.
           READ RFILE NEXT RECORD AT END MOVE "Y" TO EOF-FLAG.
           IF EOF-FLAG = "N" DISPLAY RREC.
