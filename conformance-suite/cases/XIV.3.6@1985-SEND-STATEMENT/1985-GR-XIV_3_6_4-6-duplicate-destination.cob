      *>--- conformance-case
      *> id: 1985-GR-XIV_3_6_4-6-duplicate-destination
      *> rules: [1985-GR-XIV.3.6.4-6]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.3.6.4 GR6 (THE SEND STATEMENT): a SEND "releases only a
      *> single portion of a message segment or a single portion of a message
      *> to the MCS", and ESI, EMI and EGI close the segment, message and
      *> group (§1985:XIV.3.6.4 GR10). Two occurrences of the DESTINATION
      *> TABLE name LOOP, which delivers to HOLD. Each occurrence is a
      *> destination of the statement (docs/CONFORMANCE.md §3.3), so each
      *> receives the whole message with a segment and message control of its
      *> own: two messages, each the segments AA and BB, and no segment of
      *> one is merged into the other. The error keys are set per occurrence.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SND6DUP.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT MCS-DEFS ASSIGN TO "mcs.json".
       DATA DIVISION.
       FILE SECTION.
       FD  MCS-DEFS.
       01  DEFS-REC PIC X(60).
       WORKING-STORAGE SECTION.
       01  BUF PIC X(12).
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
           SEND COUT FROM T-A WITH ESI.
           ACCEPT CIN MESSAGE COUNT.
           DISPLAY "AFTER ESI " OUT-STAT " " IN-CNT " " OUT-ERR (1)
               OUT-ERR (2).
           SEND COUT FROM T-B WITH EMI.
           ACCEPT CIN MESSAGE COUNT.
           DISPLAY "AFTER EMI " OUT-STAT " " IN-CNT " " OUT-ERR (1)
               OUT-ERR (2).
           PERFORM RCV-SEG 5 TIMES.
           STOP RUN.
       RCV-SEG.
           MOVE ALL "*" TO BUF.
           RECEIVE CIN SEGMENT INTO BUF
               NO DATA DISPLAY "NO DATA"
               WITH DATA DISPLAY "SEGMENT " IN-LEN " " IN-END
                   " [" BUF "]".
