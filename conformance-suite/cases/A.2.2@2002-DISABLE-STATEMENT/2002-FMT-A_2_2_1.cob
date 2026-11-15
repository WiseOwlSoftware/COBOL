      *>--- conformance-case
      *> id: 2002-FMT-A_2_2_1
      *> rules: [2002-FMT-A.2.2.1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:A.2.2.1 - DISABLE {INPUT [TERMINAL] | I-O TERMINAL |
      *> OUTPUT} cd-name-1 (2002 has no KEY phrase): every arm of the
      *> format, INPUT, INPUT TERMINAL, I-O TERMINAL and OUTPUT, is
      *> written and run against the CD kind A.2.2.2 SR1-SR3 require.
      *> §2002:A.2.2.3 GR1 - a DISABLE of a source or destination
      *> already disconnected takes no action "except that the value
      *> in the status key data item indicates this condition".
      *> Table A.1 (§2002:A.1.2.4): 00 for the first DISABLE of an
      *> enabled path, 15 for a repeat; table A.2 (§2002:A.1.2.5):
      *> error key 5 on the repeat DISABLE OUTPUT (A.1.2.3 GR22
      *> updates the error key only when an error occurs, so the
      *> first, successful DISABLE OUTPUT shows only its status).
      *> Every endpoint is
      *> seeded enabled, with disjoint names (queue Q1, source S1,
      *> terminal T1, destination D1) so no arm affects another.
      *> Each leg presets status XX (and error key X), so an arm that
      *> is refused fails the compile and an arm that does not set
      *> the keys shows the preset.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DIS06.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT MCS-DEFS ASSIGN TO "mcs.json".
       DATA DIVISION.
       FILE SECTION.
       FD  MCS-DEFS.
       01  DEFS-REC PIC X(60).
       COMMUNICATION SECTION.
       CD  CINQ FOR INPUT
           SYMBOLIC QUEUE IS Q-Q
           STATUS KEY IS Q-STAT.
       CD  CINS FOR INPUT
           SYMBOLIC SOURCE IS S-SRC
           STATUS KEY IS S-STAT.
       CD  CIO FOR I-O
           SYMBOLIC TERMINAL IS IO-TERM
           STATUS KEY IS IO-STAT.
       CD  COUT FOR OUTPUT
           DESTINATION COUNT IS O-CNT
           STATUS KEY IS O-STAT
           ERROR KEY IS O-ERR
           SYMBOLIC DESTINATION IS O-DEST.
       PROCEDURE DIVISION.
       SEED-MCS.
           OPEN OUTPUT MCS-DEFS.
           MOVE "{""queues"":[{""name"":""Q1"",""enabled"":true}],"
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """sources"":[{""name"":""S1"",""enabled"":true}],"
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """terminals"":[{""name"":""T1"",""enabled"":true}],"
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """destinations"":[{""name"":""D1"","
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """enabled"":true}]}"
               TO DEFS-REC.
           WRITE DEFS-REC.
           CLOSE MCS-DEFS.
       MAIN-PARA.
           MOVE "Q1" TO Q-Q. MOVE "S1" TO S-SRC.
           MOVE "T1" TO IO-TERM.
           MOVE 1 TO O-CNT. MOVE "D1" TO O-DEST.
           MOVE "XX" TO Q-STAT.
           DISABLE INPUT CINQ.
           DISPLAY "INPUT          " Q-STAT.
           MOVE "XX" TO Q-STAT.
           DISABLE INPUT CINQ.
           DISPLAY "INPUT          " Q-STAT.
           MOVE "XX" TO S-STAT.
           DISABLE INPUT TERMINAL CINS.
           DISPLAY "INPUT TERMINAL " S-STAT.
           MOVE "XX" TO S-STAT.
           DISABLE INPUT TERMINAL CINS.
           DISPLAY "INPUT TERMINAL " S-STAT.
           MOVE "XX" TO IO-STAT.
           DISABLE I-O TERMINAL CIO.
           DISPLAY "I-O TERMINAL   " IO-STAT.
           MOVE "XX" TO IO-STAT.
           DISABLE I-O TERMINAL CIO.
           DISPLAY "I-O TERMINAL   " IO-STAT.
           MOVE "XX" TO O-STAT.
           DISABLE OUTPUT COUT.
           DISPLAY "OUTPUT         " O-STAT.
           MOVE "XX" TO O-STAT. MOVE "X" TO O-ERR.
           DISABLE OUTPUT COUT.
           DISPLAY "OUTPUT         " O-STAT " " O-ERR.
           STOP RUN.
