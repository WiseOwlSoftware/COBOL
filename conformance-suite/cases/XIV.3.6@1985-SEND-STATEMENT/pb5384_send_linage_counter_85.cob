      *>--- conformance-case
      *> id: pb5384_send_linage_counter_85
      *> rules: [1985-SR-XIV.3.6.3-2, 1985-SR-XIV.3.6.3-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      * X3.23-1985 XIV 3.6.3 SR2: identifier-2 "must reference a
      * one-character integer without an operational sign"; SR3:
      * identifier-3 "must reference an integer data item". A
      * LINAGE-COUNTER's implicit description "is that of an unsigned
      * integer whose size is equal to the size of integer-1"
      * (VII 1.3.7), so the counter of LINAGE IS 9 is a one-character
      * identifier-2, and the counter of LINAGE IS 60, two
      * characters, is still an identifier-3 (kb/Work PB5384).
      * Each leg can fail: a refusal fails the translation. OPEN
      * OUTPUT sets LINAGE-COUNTER to one (VII 3.7.4 GR9d), and
      * identifier-2 holding 1 is ESI (XIV 3.6.4 GR9), so the first
      * segment arrives with end key 1 only if the counter was read
      * as the indicator; the ADVANCING phrase is ignored by this MCS
      * (docs/CONFORMANCE.md) and the second segment ends the message
      * with end key 2 (EMI).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5384E.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT MCS-DEFS ASSIGN TO "mcs.json".
           SELECT PAGE9 ASSIGN TO "PAGE9.TXT".
           SELECT PAGE60 ASSIGN TO "PAGE60.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  MCS-DEFS.
       01  DEFS-REC PIC X(60).
       FD  PAGE9 LINAGE IS 9 LINES.
       01  P9-REC PIC X(20).
       FD  PAGE60 LINAGE IS 60 LINES.
       01  P60-REC PIC X(20).
       WORKING-STORAGE SECTION.
       01  BUF PIC X(10).
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
           DESTINATION TABLE OCCURS 1 TIMES
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
           MOVE 1 TO OUT-CNT.
           MOVE "LOOP" TO OUT-DEST (1).
           MOVE 2 TO OUT-LEN.
           OPEN OUTPUT PAGE9 PAGE60.
           SEND COUT FROM T-A WITH LINAGE-COUNTER OF PAGE9.
           SEND COUT FROM T-B WITH EMI
               AFTER ADVANCING LINAGE-COUNTER OF PAGE60 LINES.
           CLOSE PAGE9 PAGE60.
           PERFORM RCV-SEG 2 TIMES.
           STOP RUN.
       RCV-SEG.
           MOVE ALL "*" TO BUF.
           RECEIVE CIN SEGMENT INTO BUF
               NO DATA DISPLAY "NO DATA"
               WITH DATA DISPLAY "SEGMENT " IN-LEN " " IN-END
                   " [" BUF "]".
