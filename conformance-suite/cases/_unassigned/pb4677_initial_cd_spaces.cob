      *>--- conformance-case
      *> id: pb4677_initial_cd_spaces
      *> rules: []
      *> accept: [85, 2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4677 - ISO/IEC 1989:2002 A.1.2.3 GR6, GR28: when the MCS has not
      *> scheduled the run unit the INITIAL CD's symbolic names are spaces "prior to
      *> the execution of the first procedure division statement", whatever the first
      *> record's VALUE clause holds; the positions after them keep theirs.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4677IN.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD CIN FOR INITIAL INPUT.
       01 R-IN.
           02 R-QUEUES PIC X(48) VALUE "SEEDED".
           02 R-REST PIC X(39) VALUE "KEPT".
       CD CIO FOR I-O
           SYMBOLIC TERMINAL IS TN.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "[" R-QUEUES "]".
           DISPLAY "[" R-REST (1:4) "]".
           STOP RUN.
