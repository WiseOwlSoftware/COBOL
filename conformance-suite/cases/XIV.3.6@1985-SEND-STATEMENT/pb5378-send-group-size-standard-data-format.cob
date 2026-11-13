      *>--- conformance-case
      *> id: pb5378-send-group-size-standard-data-format
      *> rules: [1985-GR-XIV.3.6.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5378. §1985:XIV.3.6.4 GR3: a text length "outside the
      *> range of zero through the size of the data item referenced by
      *> identifier-1 inclusive" is an error (status 50, nothing sent),
      *> and "the size of an elementary data item or a group item is the
      *> number of characters in standard data format of the item"
      *> (§1985:IV.4.3.4): a binary or packed decimal item counts its
      *> digits, not the bytes it occupies. BIN (PIC 9(4) BINARY) is four
      *> characters, so text length 4 is accepted and 5 is not. GC holds
      *> "AB" and BIN2 (9(4) BINARY, 12): six characters, so 6 is
      *> accepted and sends AB0012, 4 sends AB00 and 7 is refused. GD
      *> holds PK (PIC 9(3) PACKED-DECIMAL, 123) and a table of two
      *> PIC 99 COMP items (5 and 7): 3 + 2 + 2 = 7 characters, 1230507.
      *> A SIGNED binary or packed item is the same size: "The 'S' is not
      *> counted in determining the size (in terms of standard data format
      *> characters) of the elementary item" (§1985:VI.5.9.4 GR8d), the
      *> sign being an operational sign with no character of its own. SB
      *> (S9(4) BINARY, -12) is four characters, so 4 is accepted and 5 is
      *> not; GS (XX, S9(4) BINARY and S9(3) PACKED-DECIMAL) is 2 + 4 + 3
      *> = 9, so 9 is accepted and 10 is not. Only the lengths are shown:
      *> the representation of the sign is the implementor's.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5378A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT MCS-DEFS ASSIGN TO "mcs.json".
       DATA DIVISION.
       FILE SECTION.
       FD  MCS-DEFS.
       01  DEFS-REC PIC X(60).
       WORKING-STORAGE SECTION.
       01  BIN PIC 9(4) BINARY VALUE 12.
       01  GC.
           05  GX   PIC XX VALUE "AB".
           05  BIN2 PIC 9(4) BINARY VALUE 12.
       01  GD.
           05  PK   PIC 9(3) PACKED-DECIMAL VALUE 123.
           05  GT   PIC 99 COMP OCCURS 2 TIMES.
       01  SB PIC S9(4) BINARY VALUE -12.
       01  GS.
           05  GY   PIC XX VALUE "AB".
           05  SB2  PIC S9(4) BINARY VALUE -12.
           05  SP   PIC S9(3) PACKED-DECIMAL VALUE -123.
       01  BUF PIC X(20).
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
           MOVE 5 TO GT (1).
           MOVE 7 TO GT (2).
           MOVE 4 TO OUT-LEN.
           SEND COUT FROM BIN WITH EMI.
           DISPLAY "BIN 4 " OUT-STAT.
           PERFORM RCV-PARA.
           MOVE 5 TO OUT-LEN.
           SEND COUT FROM BIN WITH EMI.
           DISPLAY "BIN 5 " OUT-STAT.
           MOVE 6 TO OUT-LEN.
           SEND COUT FROM GC WITH EMI.
           DISPLAY "GC 6 " OUT-STAT.
           PERFORM RCV-PARA.
           MOVE 4 TO OUT-LEN.
           SEND COUT FROM GC WITH EMI.
           DISPLAY "GC 4 " OUT-STAT.
           PERFORM RCV-PARA.
           MOVE 7 TO OUT-LEN.
           SEND COUT FROM GC WITH EMI.
           DISPLAY "GC 7 " OUT-STAT.
           MOVE 7 TO OUT-LEN.
           SEND COUT FROM GD WITH EMI.
           DISPLAY "GD 7 " OUT-STAT.
           PERFORM RCV-PARA.
           MOVE 4 TO OUT-LEN.
           SEND COUT FROM SB WITH EMI.
           DISPLAY "SB 4 " OUT-STAT.
           PERFORM RCV-LEN-PARA.
           MOVE 5 TO OUT-LEN.
           SEND COUT FROM SB WITH EMI.
           DISPLAY "SB 5 " OUT-STAT.
           MOVE 9 TO OUT-LEN.
           SEND COUT FROM GS WITH EMI.
           DISPLAY "GS 9 " OUT-STAT.
           PERFORM RCV-LEN-PARA.
           MOVE 10 TO OUT-LEN.
           SEND COUT FROM GS WITH EMI.
           DISPLAY "GS 10 " OUT-STAT.
           STOP RUN.
       RCV-PARA.
           MOVE SPACES TO BUF.
           RECEIVE CIN MESSAGE INTO BUF
               NO DATA DISPLAY "NO DATA".
           DISPLAY "MSG " IN-LEN " [" BUF "]".
       RCV-LEN-PARA.
           RECEIVE CIN MESSAGE INTO BUF
               NO DATA DISPLAY "NO DATA".
           DISPLAY "LEN " IN-LEN.
