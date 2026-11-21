      *>--- conformance-case
      *> id: 1985-GR-XIV_3_4_4-1-repeated-destination
      *> rules: [1985-GR-XIV.3.4.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.3.4.4 GR1 (THE PURGE STATEMENT): "Execution of a PURGE
      *> statement causes the MCS to eliminate any partial message awaiting
      *> transmission to the destinations specified in the CD". Two
      *> occurrences of the DESTINATION TABLE name LOOP, which delivers to
      *> HOLD. Each occurrence is a destination of the statement
      *> (docs/CONFORMANCE.md §3.3) and the SEND before it released the
      *> portion AA to each, so each holds a partial message of its own and
      *> PURGE eliminates both: status 00 and error keys 0 0 (Tables 1 and
      *> 2, §1985:XIV.2.2.5 and §1985:XIV.2.2.6). A second PURGE finds no
      *> partial message for either occurrence: status 70, error keys 4 4.
      *> A SEND with EMI then delivers BB to each, two messages that are
      *> only BB, since no AA of the purged portions is left to join them.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PRG1DUP.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT MCS-DEFS ASSIGN TO "mcs.json".
       DATA DIVISION.
       FILE SECTION.
       FD  MCS-DEFS.
       01  DEFS-REC PIC X(60).
       WORKING-STORAGE SECTION.
       01  BUF PIC X(6).
       01  T-A PIC X(2) VALUE "AA".
       01  T-B PIC X(2) VALUE "BB".
       COMMUNICATION SECTION.
       CD  CIN FOR INPUT
           SYMBOLIC QUEUE IS IN-Q
           SYMBOLIC SUB-QUEUE-1 IS IN-S1
           SYMBOLIC SUB-QUEUE-2 IS IN-S2
           SYMBOLIC SUB-QUEUE-3 IS IN-S3
           MESSAGE DATE IS IN-DATE
           MESSAGE TIME IS IN-TIME
           SYMBOLIC SOURCE IS IN-SRC
           TEXT LENGTH IS IN-LEN
           END KEY IS IN-END
           STATUS KEY IS IN-STAT
           MESSAGE COUNT IS IN-CNT.
       CD  COUT FOR OUTPUT
           DESTINATION COUNT IS OUT-CNT
           TEXT LENGTH IS OUT-LEN
           STATUS KEY IS OUT-STAT
           DESTINATION TABLE OCCURS 2 TIMES
           ERROR KEY IS OUT-ERR
           SYMBOLIC DESTINATION IS OUT-DEST.
       PROCEDURE DIVISION.
       SEED-MCS.
           OPEN OUTPUT MCS-DEFS.
           MOVE "{""queues"":[{""name"":""HOLD""}],"
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """destinations"":[{""name"":""LOOP"","
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """deliverTo"":""HOLD""}]}"
               TO DEFS-REC.
           WRITE DEFS-REC.
           CLOSE MCS-DEFS.
       MAIN-PARA.
           MOVE "HOLD" TO IN-Q.
           MOVE SPACES TO IN-S1 IN-S2 IN-S3.
           MOVE 2 TO OUT-CNT.
           MOVE "LOOP" TO OUT-DEST (1).
           MOVE "LOOP" TO OUT-DEST (2).
           MOVE 2 TO OUT-LEN.
           SEND COUT FROM T-A.
           MOVE ALL "*" TO OUT-ERR (1) OUT-ERR (2).
           PURGE COUT.
           DISPLAY "PURGE " OUT-STAT " " OUT-ERR (1) OUT-ERR (2).
           MOVE ALL "*" TO OUT-ERR (1) OUT-ERR (2).
           PURGE COUT.
           DISPLAY "AGAIN " OUT-STAT " " OUT-ERR (1) OUT-ERR (2).
           SEND COUT FROM T-B WITH EMI.
           ACCEPT CIN MESSAGE COUNT.
           DISPLAY "COUNT " IN-CNT.
           PERFORM RCV-MSG 3 TIMES.
           STOP RUN.
       RCV-MSG.
           MOVE ALL "*" TO BUF.
           RECEIVE CIN MESSAGE INTO BUF
               NO DATA DISPLAY "NO DATA"
               WITH DATA DISPLAY "MESSAGE " IN-LEN " [" BUF "]".
