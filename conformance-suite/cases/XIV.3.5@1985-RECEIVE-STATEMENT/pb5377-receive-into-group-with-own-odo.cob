      *>--- conformance-case
      *> id: pb5377-receive-into-group-with-own-odo
      *> rules: [1985-GR-XIV.3.5.4-3, 1985-GR-XIV.3.5.4-8,
      *>     1985-GR-XIV.3.5.4-9]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5377. §1985:XIV.3.5.4 GR3 transfers the message "to
      *> the receiving character positions of the area referenced by
      *> identifier-1", and §1985:VI.5.8.4 GR3b makes a group that holds
      *> its own OCCURS DEPENDING ON object, as a receiving item, a group
      *> of its MAXIMUM length: GOD is ten positions (GN, then GT up to
      *> nine times) whatever GN holds, GN being 3 before each RECEIVE.
      *> GR8b: "If a message size is less than the area referenced by
      *> identifier-1" the message is left aligned and the positions
      *> past it are unchanged, and GR9b says the same of a segment; so
      *> the nine-character message and segment give TEXT LENGTH 0009
      *> with END KEY 2 and 1, the whole text in GOD(1:9), and the tenth
      *> position and, for the four-character message, every position
      *> past the text, still the asterisks the area held. The message
      *> text begins with its own GN, 5 for the first, so GN changes.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5377A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT MCS-DEFS ASSIGN TO "mcs.json".
       DATA DIVISION.
       FILE SECTION.
       FD  MCS-DEFS.
       01  DEFS-REC PIC X(60).
       WORKING-STORAGE SECTION.
       01  GOD.
           05  GN PIC 9.
           05  GT PIC X OCCURS 1 TO 9 DEPENDING ON GN.
       01  GSAVE PIC X(10).
       01  MSG9 PIC X(9) VALUE "5ABCDEFGH".
       01  MSG4 PIC X(4) VALUE "3ABC".
       01  MSG2 PIC X(2) VALUE "XY".
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
           MOVE 9 TO OUT-LEN.
           SEND COUT FROM MSG9 WITH EMI.
           MOVE 9 TO GN.
           MOVE ALL "*" TO GOD.
           MOVE 3 TO GN.
           RECEIVE CIN MESSAGE INTO GOD
               NO DATA DISPLAY "NO DATA".
           DISPLAY "MESSAGE " IN-LEN " " IN-END " " GN.
           MOVE 9 TO GN.
           MOVE GOD TO GSAVE.
           DISPLAY "GOD [" GSAVE(2:9) "]".
           SEND COUT FROM MSG9 WITH ESI.
           MOVE 2 TO OUT-LEN.
           SEND COUT FROM MSG2 WITH EMI.
           MOVE 9 TO GN.
           MOVE ALL "*" TO GOD.
           MOVE 3 TO GN.
           RECEIVE CIN SEGMENT INTO GOD
               NO DATA DISPLAY "NO DATA".
           DISPLAY "SEGMENT " IN-LEN " " IN-END " " GN.
           MOVE 9 TO GN.
           MOVE GOD TO GSAVE.
           DISPLAY "GOD [" GSAVE(2:9) "]".
           RECEIVE CIN SEGMENT INTO GSAVE
               NO DATA DISPLAY "NO DATA".
           MOVE 4 TO OUT-LEN.
           SEND COUT FROM MSG4 WITH EMI.
           MOVE 9 TO GN.
           MOVE ALL "*" TO GOD.
           MOVE 1 TO GN.
           RECEIVE CIN MESSAGE INTO GOD
               NO DATA DISPLAY "NO DATA".
           DISPLAY "SHORT " IN-LEN " " IN-END " " GN.
           MOVE 9 TO GN.
           MOVE GOD TO GSAVE.
           DISPLAY "GOD [" GSAVE(2:9) "]".
           STOP RUN.
