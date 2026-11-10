      *>--- conformance-case
      *> id: pb5070-padding-data-name-two-characters
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5070 - 1985:VII.2.7.3 SR3: "Data-name-1 must be defined in the Data Division as a one-character data item
      *> of the category alphanumeric" (2002:12.3.4.10.2 SR3). WS-PAD is PIC X(2). conformance:pb5070_padding_one_character
      *> is the legal side.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5070PD.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PAD-FILE ASSIGN TO "PB5070P1.DAT"
               PADDING CHARACTER IS "*".
           SELECT PAD-FILE2 ASSIGN TO "PB5070P2.DAT"
               PADDING CHARACTER IS WS-PAD.
       DATA DIVISION.
       FILE SECTION.
       FD  PAD-FILE.
       01  PAD-REC PIC X(10).
       FD  PAD-FILE2.
       01  PAD-REC2 PIC X(10).
       WORKING-STORAGE SECTION.
       01  WS-PAD PIC X(2).
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
