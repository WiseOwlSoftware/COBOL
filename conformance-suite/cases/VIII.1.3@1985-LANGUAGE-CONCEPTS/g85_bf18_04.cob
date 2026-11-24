      *>--- conformance-case
      *> id: g85_bf18_04
      *> rules: [1985-GR-VIII.1.3.4-1, 1985-GR-VIII.1.3.4-L2.1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.1.3.4 GR1 - "Successful Completion. The input-output
      *> statement was successfully executed." §1985:VIII.1.3.4 GRL2.1
      *> lists its three values for a relative file:
      *>   a. 00 - successful, no further information: every OPEN,
      *>      WRITE, READ, REWRITE, DELETE, START and CLOSE of RL.
      *>   b. 04 - "A READ statement is successfully executed but the
      *>      length of the record being processed does not conform to
      *>      the fixed file attributes for that file": not produced
      *>      here. WiseOwl COBOL validates the attributes at OPEN (39),
      *>      and the one way past that check, a reader opened on a
      *>      zero-byte file while another connector writes it, is an
      *>      OPEN of a file in the open mode, 41 at 85
      *>      (§1985:VII.1.3.5; kb/Work PB6161).
      *>   c. 05 - an OPEN of an OPTIONAL file that is not present; "If
      *>      the open mode is I-O or extend, the file has been
      *>      created": OPEN I-O OPT reports 05, and an OPEN INPUT after
      *>      its CLOSE finds the file (00) and empty (READ NEXT, at end
      *>      10).
      *> By §1985:VIII.1.3.4 no USE AFTER STANDARD EXCEPTION procedure
      *> runs for a Successful Completion condition: USE-COUNT stays 0
      *> through every 00 and 05 above. A last READ NEXT on RL with
      *> no AT END phrase is the at end condition, which is not
      *> successful (§1985:VIII.4.5.4 GR10c: the procedure runs once,
      *> so the 0 is not a procedure that never fires. Each leg fails on
      *> a wrong status value or a USE procedure run for a success.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF1804.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RL ASSIGN TO "G85BF1804A.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS RL-KEY
               FILE STATUS IS RL-ST.
           SELECT OPTIONAL OPT ASSIGN TO "G85BF1804B.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS OPT-ST.
           SELECT SQ ASSIGN TO "G85BF1804C.DAT"
               FILE STATUS IS SQ-ST.
           SELECT WF ASSIGN TO "G85BF1804C.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS WF-KEY
               FILE STATUS IS WF-ST.
           SELECT RDF ASSIGN TO "G85BF1804C.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RD-KEY
               FILE STATUS IS RD-ST.
       DATA DIVISION.
       FILE SECTION.
       FD  RL.
       01  RL-REC PIC X(6).
       FD  OPT.
       01  OPT-REC PIC X(6).
       FD  SQ.
       01  SQ-REC PIC X(4).
       FD  WF
           RECORD IS VARYING IN SIZE FROM 1 TO 20 CHARACTERS
               DEPENDING ON WF-LEN.
       01  WF-REC PIC X(20).
       FD  RDF
           RECORD IS VARYING IN SIZE FROM 5 TO 10 CHARACTERS
               DEPENDING ON RD-LEN.
       01  RD-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01  RL-ST PIC XX.
       01  OPT-ST PIC XX.
       01  SQ-ST PIC XX.
       01  WF-ST PIC XX.
       01  RD-ST PIC XX.
       01  RL-KEY PIC 9(4).
       01  WF-KEY PIC 9(4).
       01  RD-KEY PIC 9(4).
       01  WF-LEN PIC 99.
       01  RD-LEN PIC 99.
       01  USE-COUNT PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       RL-ERR SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON RL OPT RDF.
       RL-ERR-PARA.
           ADD 1 TO USE-COUNT.
           DISPLAY "USE RAN " RL-ST.
       END DECLARATIVES.
       MAIN-SECT SECTION.
       MAIN-PARA.
      * Status 00 on each statement of a relative file.
           OPEN OUTPUT RL.
           DISPLAY "A OPEN-OUTPUT " RL-ST.
           MOVE 1 TO RL-KEY.
           MOVE "REC001" TO RL-REC.
           WRITE RL-REC INVALID KEY DISPLAY "W1 INVALID".
           DISPLAY "A WRITE-1 " RL-ST.
           MOVE 2 TO RL-KEY.
           MOVE "REC002" TO RL-REC.
           WRITE RL-REC INVALID KEY DISPLAY "W2 INVALID".
           DISPLAY "A WRITE-2 " RL-ST.
           CLOSE RL.
           DISPLAY "A CLOSE " RL-ST.
           OPEN I-O RL.
           DISPLAY "A OPEN-I-O " RL-ST.
           MOVE 2 TO RL-KEY.
           READ RL INVALID KEY DISPLAY "R2 INVALID".
           DISPLAY "A READ " RL-ST " " RL-REC.
           MOVE "REC00X" TO RL-REC.
           REWRITE RL-REC INVALID KEY DISPLAY "RW INVALID".
           DISPLAY "A REWRITE " RL-ST.
           MOVE 1 TO RL-KEY.
           DELETE RL INVALID KEY DISPLAY "DEL INVALID".
           DISPLAY "A DELETE " RL-ST.
           MOVE 1 TO RL-KEY.
           START RL KEY IS NOT LESS THAN RL-KEY
               INVALID KEY DISPLAY "START INVALID".
           DISPLAY "A START " RL-ST.
           READ RL NEXT RECORD AT END DISPLAY "N1 AT END".
           DISPLAY "A READ-NEXT " RL-ST " " RL-REC.
      * Status 05 on the OPEN of an absent OPTIONAL file.
           OPEN I-O OPT.
           DISPLAY "C OPEN-I-O " OPT-ST.
           CLOSE OPT.
           DISPLAY "C CLOSE " OPT-ST.
           OPEN INPUT OPT.
           DISPLAY "C REOPEN " OPT-ST.
           READ OPT NEXT RECORD AT END DISPLAY "C EMPTY".
           DISPLAY "C READ " OPT-ST.
           CLOSE OPT.
           DISPLAY "USE-COUNT " USE-COUNT.
      * The procedure is live: an at end READ with no AT END phrase.
           READ RL NEXT RECORD.
           DISPLAY "D READ-NEXT " RL-ST.
           DISPLAY "USE-COUNT " USE-COUNT.
           CLOSE RL.
           STOP RUN.
