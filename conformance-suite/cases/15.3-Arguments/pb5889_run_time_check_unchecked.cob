      *>--- conformance-case
      *> id: pb5889_run_time_check_unchecked
      *> rules: [AR-15.3-6, DOC-A.1-124]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5889 - an undecided integer argument with
      *> EC-ARGUMENT-FUNCTION checking off.
      *>
      *> §2023:15.3 AR14 (cite.py --check): with checking for
      *> EC-ARGUMENT-FUNCTION not enabled "the implementor defines
      *> the result of the function reference". WiseOwl COBOL's
      *> determination (docs/CONFORMANCE.md DOC-A.1-124) is the
      *> truncated integer, so the run goes on.
      *> EXPECTED: ORD ("B") is 67 in the native collating sequence,
      *> ORD ("B") / 2 is 33.5, its truncated integer 33, and
      *> MOD (33, 7) = 5 (AFTER 0005).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5889U.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A  PIC X VALUE "B".
       01  R  PIC 9(4).
       PROCEDURE DIVISION.
       P1.
           COMPUTE R = FUNCTION MOD (FUNCTION ORD (A) / 2, 7).
           DISPLAY "AFTER " R.
           STOP RUN.
