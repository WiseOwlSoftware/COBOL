      *>--- conformance-case
      *> id: pb742-record-key-no-organization-clause-85
      *> rules: [1985-GR-VII.2.6.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5997 (the 85 half of pb742-record-key-no-
      *> organization- clause). §1985:VII.2.6.3 GR3 (cite.py: OK):
      *> "When the ORGANIZATION clause is not specified, sequential
      *> organization is implied." SQF writes none, so it is
      *> sequential, and its RECORD KEY clause belongs to the indexed
      *> entry alone. The refusal names the omission in the 1985
      *> wording, "sequential by default", and cites the standard as
      *> ANSI §1985:VII.2.6.3 GR3, never "record sequential" (a
      *> COBOL-2002 phrase) or §2023:12.4.5.10.3 GR6.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P742N85.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SQF ASSIGN TO "pb742n85.dat"
               RECORD KEY IS SQ-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD SQF.
       01 SQ-REC.
          05 SQ-KEY PIC X(5).
          05 SQ-DATA PIC X(5).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN INPUT SQF
           CLOSE SQF
           STOP RUN.
