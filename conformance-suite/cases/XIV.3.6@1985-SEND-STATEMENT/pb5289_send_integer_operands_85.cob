      *>--- conformance-case
      *> id: pb5289_send_integer_operands_85
      *> rules: [1985-SR-XIV.3.6.3-2, 1985-SR-XIV.3.6.3-3,
      *>     1985-SR-XIV.3.6.3-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      * X3.23-1985 XIV 3.6.3 SR2: identifier-2 "must reference a
      * one-character integer without an operational sign"; SR3:
      * identifier-3 "must reference an integer data item"; SR5:
      * "Integer-1 ... may be zero." An integer item has no digit
      * position right of the point (III 2), and "the scaling position
      * character P is not counted in the size of the data item"
      * (VI 5.9.4), so PIC 9P is a one-character integer and PIC 9PP
      * an integer; ALL ZERO is the figurative ZERO (kb/Work PB5289).
      * Each leg can fail: a refusal fails the translation; I-9P
      * holds 20, whose one digit 2 is EMI (XIV 3.6.4 GR9), so the
      * received end key is 2 only if the indicator was read; the
      * ADVANCING phrases are ignored by this MCS (docs/CONFORMANCE.md)
      * and each message must still arrive whole.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5289A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT MCS-DEFS ASSIGN TO "mcs.json".
       DATA DIVISION.
       FILE SECTION.
       FD  MCS-DEFS.
       01  DEFS-REC PIC X(60).
       WORKING-STORAGE SECTION.
       01  BUF PIC X(10).
       01  MSG PIC X(5) VALUE "HELLO".
       01  I-9P PIC 9P VALUE 20.
       01  ADVPP PIC 9PP VALUE 100.
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
           MOVE 5 TO OUT-LEN.
           SEND COUT FROM MSG WITH I-9P.
           PERFORM RCV-MSG.
           SEND COUT FROM MSG WITH EMI AFTER ADVANCING ADVPP LINES.
           PERFORM RCV-MSG.
           SEND COUT FROM MSG WITH EMI
               BEFORE ADVANCING ALL ZERO LINES.
           PERFORM RCV-MSG.
           STOP RUN.
       RCV-MSG.
           MOVE ALL "*" TO BUF.
           RECEIVE CIN MESSAGE INTO BUF
               NO DATA DISPLAY "NO DATA"
               WITH DATA DISPLAY "MESSAGE " IN-LEN " " IN-END
                   " [" BUF "]".
