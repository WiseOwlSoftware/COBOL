      *>--- conformance-case
      *> id: pb5352-return-into-cites-1985
      *> rules: [1985-GR-XI.4.3.4-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5352: the refusal names the rule in force at 85.
      *> §1985:XI.4.3.4 GR5: "The INTO phrase may be specified in a
      *> RETURN statement: a. If only one record description is
      *> subordinate to the sort-merge file description entry, or b.
      *> If all record-names associated with file-name-1 and the data
      *> item referenced by identifier-1 describe a group item or an
      *> elementary alphanumeric item." SF has two records and S-NUM
      *> is numeric: COBOL1994, citing GR5 of the sort-merge module.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5352R.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "PB5352R.TMP".
       DATA DIVISION.
       FILE SECTION.
       SD  SF.
       01  S-REC.
           05 S-K PIC X(6).
       01  S-NUM PIC 9(6).
       WORKING-STORAGE SECTION.
       01  WS-RECV PIC X(6).
       PROCEDURE DIVISION.
       MAIN-PARA.
           SORT SF ASCENDING S-K
               INPUT PROCEDURE IS FEED
               OUTPUT PROCEDURE IS DRAIN.
           STOP RUN.
       FEED.
           MOVE "1ONE  " TO S-REC.
           RELEASE S-REC.
       DRAIN.
           RETURN SF INTO WS-RECV AT END CONTINUE END-RETURN.
           STOP RUN.
