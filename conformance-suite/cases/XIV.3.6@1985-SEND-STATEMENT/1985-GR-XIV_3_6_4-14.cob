      *>--- conformance-case
      *> id: 1985-GR-XIV_3_6_4-14
      *> rules: [1985-GR-XIV.3.6.4-14]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.3.6.4 GR14 (THE SEND STATEMENT): "On a device where
      *> vertical positioning is applicable and the ADVANCING phrase
      *> is not specified, automatic advancing will be provided by the
      *> implementor to act as if the user had specified AFTER
      *> ADVANCING 1 LINE." The same two messages go to destination
      *> D1 with no ADVANCING phrase and to D2 with AFTER ADVANCING 1
      *> LINE. If vertical positioning is applicable, GR14 makes the
      *> first act as the second; if it is not, "the MCS will ignore
      *> the vertical positioning specified or implied" (GR12). Under
      *> either, the two device files are byte for byte the same
      *> ("D1 D2 IDENTICAL"). This implementation's devices take
      *> variable-length messages, the line end after a message being
      *> its delimiter (docs/CONFORMANCE.md 3.3), so each message is
      *> one delimited line of 5 characters. The test fails if a
      *> device honours ADVANCING with any other default.
      *> The program first defines its destinations to this
      *> implementation's message control system (the file mcs.json
      *> of the MCS directory).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SNDGR14.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS LF-CH IS 11 CR-CH IS 14.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT MCS-DEFS ASSIGN TO "mcs.json".
           SELECT DEV-1 ASSIGN TO "D1.out".
           SELECT DEV-2 ASSIGN TO "D2.out".
       DATA DIVISION.
       FILE SECTION.
       FD  MCS-DEFS.
       01  DEFS-REC.
           05 DEFS-1 PIC X(31).
           05 DEFS-2 PIC X(29).
       FD  DEV-1.
       01  D1-CH PIC X.
       FD  DEV-2.
       01  D2-CH PIC X.
       WORKING-STORAGE SECTION.
       01  T-A PIC X(5) VALUE "AAAAA".
       01  T-B PIC X(5) VALUE "BBBBB".
       01  D1-EOF PIC X.
       01  D2-EOF PIC X.
       01  SAME-FLAG PIC X.
       01  DEV-LINE PIC X(10).
       01  DEV-POS PIC 99.
       COMMUNICATION SECTION.
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
           MOVE "{""destinations"":[{""name"":""D1""},"
               TO DEFS-1.
           MOVE "{""name"":""D2""}]}" TO DEFS-2.
           WRITE DEFS-REC.
           CLOSE MCS-DEFS.
       MAIN-PARA.
           MOVE 1 TO OUT-CNT.
           MOVE 5 TO OUT-LEN.
           MOVE "D1" TO OUT-DEST (1).
           SEND COUT FROM T-A WITH EMI.
           DISPLAY "D1 A " OUT-STAT.
           SEND COUT FROM T-B WITH EMI.
           DISPLAY "D1 B " OUT-STAT.
           MOVE "D2" TO OUT-DEST (1).
           SEND COUT FROM T-A WITH EMI AFTER ADVANCING 1 LINE.
           DISPLAY "D2 A " OUT-STAT.
           SEND COUT FROM T-B WITH EMI AFTER ADVANCING 1 LINE.
           DISPLAY "D2 B " OUT-STAT.
           PERFORM COMPARE-DEVICES.
           PERFORM SHOW-D1.
           STOP RUN.
       COMPARE-DEVICES.
           OPEN INPUT DEV-1 DEV-2.
           MOVE "N" TO D1-EOF D2-EOF.
           MOVE "Y" TO SAME-FLAG.
           PERFORM COMPARE-BYTE UNTIL D1-EOF = "Y" OR D2-EOF = "Y".
           IF D1-EOF NOT = D2-EOF
               MOVE "N" TO SAME-FLAG.
           CLOSE DEV-1 DEV-2.
           IF SAME-FLAG = "Y"
               DISPLAY "D1 D2 IDENTICAL"
           ELSE
               DISPLAY "D1 D2 DIFFER".
       COMPARE-BYTE.
           READ DEV-1 AT END MOVE "Y" TO D1-EOF.
           READ DEV-2 AT END MOVE "Y" TO D2-EOF.
           IF D1-EOF = "N" AND D2-EOF = "N"
               IF D1-CH NOT = D2-CH
                   MOVE "N" TO SAME-FLAG.
       SHOW-D1.
           OPEN INPUT DEV-1.
           MOVE SPACES TO DEV-LINE.
           MOVE 0 TO DEV-POS.
           MOVE "N" TO D1-EOF.
           PERFORM SHOW-CHAR UNTIL D1-EOF = "Y".
           CLOSE DEV-1.
       SHOW-CHAR.
           READ DEV-1 AT END MOVE "Y" TO D1-EOF.
           IF D1-EOF = "N"
               IF D1-CH = LF-CH
                   DISPLAY "D1 " DEV-POS " [" DEV-LINE "]"
                   MOVE SPACES TO DEV-LINE
                   MOVE 0 TO DEV-POS
               ELSE
                   IF D1-CH NOT = CR-CH
                       ADD 1 TO DEV-POS
                       MOVE D1-CH TO DEV-LINE (DEV-POS:1).
