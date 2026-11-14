      *>--- conformance-case
      *> id: pb5352-read-into-indexed-cites-1985
      *> rules: [1985-GR-IX.4.5.4-7]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5352: the refusal names the rule of the indexed
      *> module, §1985:IX.4.5.4 GR7, which words the INTO rule as the
      *> sequential module does: "describe a group item or an
      *> elementary alphanumeric item". F has two records and F-NUM
      *> is numeric: COBOL1994, citing GR7 of the indexed module.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5352I.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "PB5352I.DAT"
               ORGANIZATION IS INDEXED ACCESS IS RANDOM
               RECORD KEY IS F-K.
       DATA DIVISION.
       FILE SECTION.
       FD  F.
       01  F-REC.
           05 F-K PIC X(6).
       01  F-NUM PIC 9(6).
       WORKING-STORAGE SECTION.
       01  WS-RECV PIC X(6).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN INPUT F.
           READ F INTO WS-RECV INVALID KEY CONTINUE END-READ.
           CLOSE F.
           STOP RUN.
