      *>--- conformance-case
      *> id: pb5891-degree-over-cap
      *> rules: [AR-15.3-6, AR-15.64.3-1]
      *> accept: [85, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5891 refused (N ** 13 - N) / 13, whose degree
      *> passes the grid test's cap.
      *>
      *> kb/Work PB5889 (the ruling of 2026-11-21): the integer-
      *> argument screen refuses an argument only when it can name a
      *> value of the operands at which the expression is not an
      *> integer, and accepts the rest with a run-time check of the
      *> value. §2023:15.3 AR6 (cite.py --check): "An arithmetic
      *> expression that will always result in an integer value or an
      *> integer data item shall be specified"; §1989:A.2.2 AR4
      *> (cite.py --check) the same at COBOL-85.
      *>
      *> By Fermat's little theorem N ** 13 - N is a multiple of 13
      *> for every N, so the argument always results in an integer;
      *> the proof's cap leaves it undecided, and it is accepted and
      *> checked at run time. EXPECTED, with N = 2: (8192 - 2) / 13 =
      *> 630, MOD (630, 7) = 0.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5891A3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N  PIC 99 VALUE 2.
       01  R  PIC 9(18).
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE R = FUNCTION MOD ((N ** 13 - N) / 13, 7).
           DISPLAY R.
           STOP RUN.
