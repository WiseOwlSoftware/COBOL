      *>--- conformance-case
      *> id: pb5070-padding-literal-two-characters
      *> rules: [GR-12.4.5.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5070 - 1985:VII.2.7.3 SR1: "Literal-1 must be a one-character nonnumeric literal." (2002:12.3.4.10.2 SR1).
      *> "AB" is two characters. conformance:pb5070_padding_one_character is the legal side.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5070PL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PAD-FILE ASSIGN TO "PB5070P1.DAT"
               PADDING CHARACTER IS "AB".
           SELECT PAD-FILE2 ASSIGN TO "PB5070P2.DAT"
               PADDING CHARACTER IS WS-PAD.
       DATA DIVISION.
       FILE SECTION.
       FD  PAD-FILE.
       01  PAD-REC PIC X(10).
       FD  PAD-FILE2.
       01  PAD-REC2 PIC X(10).
       WORKING-STORAGE SECTION.
       01  WS-PAD PIC X.
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
