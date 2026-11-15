      *>--- conformance-case
      *> id: pb5352-read-into-sequential-cites-1985
      *> rules: [1985-GR-VII.4.4.4-7]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5352: the refusal names the rule in force at 85.
      *> §1985:VII.4.4.4 GR7: "The INTO phrase may be specified in a
      *> READ statement: a. If only one record description is
      *> subordinate to the file description entry, or b. If all
      *> record-names associated with file-name-1 and the data item
      *> referenced by identifier-1 describe a group item or an
      *> elementary alphanumeric item." F has two records and F-NUM is
      *> numeric: COBOL1994, citing GR7 of the sequential module.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5352S.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "PB5352S.DAT".
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
           READ F INTO WS-RECV AT END CONTINUE END-READ.
           CLOSE F.
           STOP RUN.
