      *>--- conformance-case
      *> id: 1985-GR-VIII_4_4_4-18
      *> rules: [1985-GR-VIII.4.4.4-18]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.4.4 GR18 (THE OPEN STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WRONG original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RELOPN.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RFILE ASSIGN TO RELDISK
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  RFILE
           RECORD CONTAINS 10 CHARACTERS.
       01  RREC PIC X(10).
       WORKING-STORAGE SECTION.
       77  EOF-SW PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RFILE.
           MOVE "FIRSTREC01" TO RREC.
           WRITE RREC
               INVALID KEY DISPLAY "WRITE FAILED".
           MOVE "SECONDRC02" TO RREC.
           WRITE RREC
               INVALID KEY DISPLAY "WRITE FAILED".
           CLOSE RFILE.
           OPEN INPUT RFILE.
           READ RFILE AT END MOVE "Y" TO EOF-SW.
           IF EOF-SW = "N" DISPLAY RREC.
           READ RFILE AT END MOVE "Y" TO EOF-SW.
           IF EOF-SW = "N" DISPLAY RREC.
           CLOSE RFILE.
           STOP RUN.
