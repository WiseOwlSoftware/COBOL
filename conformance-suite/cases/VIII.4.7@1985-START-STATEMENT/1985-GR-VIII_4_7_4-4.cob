      *>--- conformance-case
      *> id: 1985-GR-VIII_4_7_4-4
      *> rules: [1985-GR-VIII.4.7.4-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.7.4 GR4 (THE START STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. STARTGT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT REL-FILE ASSIGN TO RELFILE
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS REL-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD  REL-FILE
           LABEL RECORDS ARE STANDARD.
       01  REL-REC PIC X(5).
       WORKING-STORAGE SECTION.
       77  REL-KEY PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT REL-FILE.
           MOVE 1 TO REL-KEY.
           MOVE "REC01" TO REL-REC.
           WRITE REL-REC INVALID KEY DISPLAY "WRITE FAIL".
           MOVE 2 TO REL-KEY.
           MOVE "REC02" TO REL-REC.
           WRITE REL-REC INVALID KEY DISPLAY "WRITE FAIL".
           MOVE 5 TO REL-KEY.
           MOVE "REC05" TO REL-REC.
           WRITE REL-REC INVALID KEY DISPLAY "WRITE FAIL".
           CLOSE REL-FILE.
           OPEN INPUT REL-FILE.
           MOVE 3 TO REL-KEY.
           START REL-FILE KEY IS GREATER THAN REL-KEY
               INVALID KEY DISPLAY "START1 INVALID".
           READ REL-FILE NEXT RECORD AT END DISPLAY "EOF".
           DISPLAY REL-REC.
           MOVE 5 TO REL-KEY.
           START REL-FILE KEY IS GREATER THAN REL-KEY
               INVALID KEY DISPLAY "START2 INVALID"
               NOT INVALID KEY DISPLAY "START2 OK".
           CLOSE REL-FILE.
           STOP RUN.
