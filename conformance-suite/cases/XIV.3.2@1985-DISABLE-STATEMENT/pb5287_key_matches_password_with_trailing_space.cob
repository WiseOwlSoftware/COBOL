      *>--- conformance-case
      *> id: pb5287_key_matches_password_with_trailing_space
      *> rules: [1985-GR-XIV.3.2.4-7, 1985-GR-XIV.3.3.4-6]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.3.2.4 GR7 and §1985:XIV.3.3.4 GR6 (THE DISABLE and ENABLE STATEMENTS). An output test.
      * X3.23-1985 XIV 3.2.4 general rule 7 and 3.3.4 general rule 6:
      * the statement is honored only if the KEY phrase matches the
      * system password. A password defined with a trailing space, or of
      * spaces alone, is matched by the same value, and the matching
      * compares alphanumeric operands, so trailing spaces differ in
      * neither.
      * The program first defines its queues, sources and
      * destinations to this implementation's message control
      * system (the file mcs.json of the MCS directory).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. KEYSPC.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT MCS-DEFS ASSIGN TO "mcs.json".
       DATA DIVISION.
       FILE SECTION.
       FD  MCS-DEFS.
       01  DEFS-REC PIC X(60).
       WORKING-STORAGE SECTION.
       01  MSG PIC X(5) VALUE "HELLO".
       01  KEYX PIC X(10) VALUE SPACES.
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
           DESTINATION TABLE OCCURS 3 TIMES
           ERROR KEY IS OUT-ERR
           SYMBOLIC DESTINATION IS OUT-DEST.
       PROCEDURE DIVISION.
       SEED-MCS.
           OPEN OUTPUT MCS-DEFS.
           MOVE "{""queues"":[{""name"":""Q1"",""enabled"":"
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE "false,""key"":"" ""}],""destinations"":[{"
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """name"":""D1"",""key"":""AB ""},{""name"":"
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """D3"",""key"":"" ""}]}"
               TO DEFS-REC.
           WRITE DEFS-REC.
           CLOSE MCS-DEFS.
       MAIN-PARA.
           MOVE SPACES TO IN-Q IN-S1 IN-S2 IN-S3 IN-SRC.
           MOVE 1 TO OUT-CNT.
           MOVE "D1" TO OUT-DEST (1).
           DISABLE OUTPUT COUT WITH KEY "AB ".
           DISPLAY "D1 AB+SP " OUT-STAT.
           DISABLE OUTPUT COUT WITH KEY "AB".
           DISPLAY "D1 AB    " OUT-STAT.
           MOVE "D3" TO OUT-DEST (1).
           DISABLE OUTPUT COUT WITH KEY KEYX.
           DISPLAY "D3 KEYX  " OUT-STAT.
           MOVE "Q1" TO IN-Q.
           ENABLE INPUT CIN WITH KEY " ".
           DISPLAY "Q1 SPACE " IN-STAT.
           MOVE "D1" TO OUT-DEST (1).
           DISABLE OUTPUT COUT WITH KEY "XY".
           DISPLAY "D1 XY    " OUT-STAT.
           STOP RUN.
