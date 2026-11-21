      *>--- conformance-case
      *> id: 1985-GR-VIII_1_1-p1
      *> rules: [1985-GR-VIII.1.1-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.1.1 p1 (INTRODUCTION TO THE RELATIVE I-O MODULE). An
      *> output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-99.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RELSEQ.
      * 1985 VIII.1.1 p1: relative file, ACCESS MODE SEQUENTIAL
      * (level 1) - sequential manner without DYNAMIC.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RFL ASSIGN TO "S13RRELSEQ.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL RELATIVE KEY IS RK
               FILE STATUS IS ST.
       DATA DIVISION.
       FILE SECTION.
       FD  RFL
           LABEL RECORDS ARE STANDARD.
       01  RR PIC X(4).
       WORKING-STORAGE SECTION.
       01  RK PIC 9(4).
       01  ST PIC XX.
       01  EOF PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RFL.
           MOVE "AAAA" TO RR.
               WRITE RR INVALID KEY DISPLAY "IK" END-WRITE.
           DISPLAY "W1 " ST " RK " RK.
           MOVE "BBBB" TO RR.
               WRITE RR INVALID KEY DISPLAY "IK" END-WRITE.
           DISPLAY "W2 " ST " RK " RK.
           MOVE "CCCC" TO RR.
               WRITE RR INVALID KEY DISPLAY "IK" END-WRITE.
           DISPLAY "W3 " ST " RK " RK.
           CLOSE RFL.
           OPEN INPUT RFL.
           PERFORM UNTIL EOF = "Y"
               READ RFL AT END MOVE "Y" TO EOF
               END-READ
               IF EOF = "N" DISPLAY "R " RR " RK " RK " " ST
               ELSE DISPLAY "END " ST
               END-IF
           END-PERFORM.
           CLOSE RFL.
           STOP RUN.
