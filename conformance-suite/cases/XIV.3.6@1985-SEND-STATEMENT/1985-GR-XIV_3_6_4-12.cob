      *>--- conformance-case
      *> id: 1985-GR-XIV_3_6_4-12
      *> rules: [1985-GR-XIV.3.6.4-12]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.3.6.4 GR12 (THE SEND STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
      * X3.23-1985 XIV 3.6.4 general rule 12: on a device where
      * vertical positioning is not applicable the MCS ignores the
      * vertical positioning specified. This implementation's devices
      * are such (the file DEV.out, a line a message).
      * The program first defines its queues, sources and
      * destinations to this implementation's message control
      * system (the file mcs.json of the MCS directory).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SNDGR12.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS LF-CH IS 11 CR-CH IS 14.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT MCS-DEFS ASSIGN TO "mcs.json".
           SELECT DEVICE ASSIGN TO "DEV.out".
       DATA DIVISION.
       FILE SECTION.
       FD  MCS-DEFS.
       01  DEFS-REC PIC X(60).
       FD  DEVICE.
       01  DEV-CH PIC X.
       WORKING-STORAGE SECTION.
       01  T-A PIC X(5) VALUE "AAAAA".
       01  T-B PIC X(5) VALUE "BBBBB".
       01  T-C PIC X(5) VALUE "CCCCC".
       01  N-LINES PIC 9 VALUE 4.
       01  DEV-LINE PIC X(10).
       01  DEV-POS PIC 99.
       01  DEV-EOF PIC X.
       COMMUNICATION SECTION.
       CD  COUT FOR OUTPUT
           DESTINATION COUNT IS OUT-CNT
           TEXT LENGTH IS OUT-LEN
           STATUS KEY IS OUT-STAT
           DESTINATION TABLE OCCURS 3 TIMES
           ERROR KEY IS OUT-ERR
           SYMBOLIC DESTINATION IS OUT-DEST.
       PROCEDURE DIVISION.
       SEED-MCS.
           OPEN OUTPUT MCS-DEFS.
           MOVE "{""destinations"":[{""name"":""DEV""}]}"
               TO DEFS-REC.
           WRITE DEFS-REC.
           CLOSE MCS-DEFS.
       MAIN-PARA.
           MOVE 1 TO OUT-CNT.
           MOVE "DEV" TO OUT-DEST (1).
           MOVE 5 TO OUT-LEN.
           SEND COUT FROM T-A WITH EMI AFTER ADVANCING 2 LINES.
           DISPLAY "AFTER 2 LINES  " OUT-STAT.
           SEND COUT FROM T-B WITH EMI BEFORE ADVANCING N-LINES LINES.
           DISPLAY "BEFORE 4 LINES " OUT-STAT.
           SEND COUT FROM T-C WITH EMI AFTER ADVANCING PAGE.
           DISPLAY "AFTER PAGE     " OUT-STAT.
           PERFORM SHOW-DEVICE.
           STOP RUN.
       SHOW-DEVICE.
           OPEN INPUT DEVICE.
           MOVE SPACES TO DEV-LINE.
           MOVE 0 TO DEV-POS.
           MOVE "N" TO DEV-EOF.
           PERFORM SHOW-CHAR UNTIL DEV-EOF = "Y".
           CLOSE DEVICE.
       SHOW-CHAR.
           READ DEVICE AT END MOVE "Y" TO DEV-EOF.
           IF DEV-EOF = "N"
               IF DEV-CH = LF-CH
                   DISPLAY "DEVICE " DEV-POS " [" DEV-LINE "]"
                   MOVE SPACES TO DEV-LINE
                   MOVE 0 TO DEV-POS
               ELSE
                   IF DEV-CH NOT = CR-CH
                       ADD 1 TO DEV-POS
                       MOVE DEV-CH TO DEV-LINE (DEV-POS:1).
