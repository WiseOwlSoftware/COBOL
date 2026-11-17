      *>--- conformance-case
      *> id: pb5891-standard-binary-argument
      *> rules: [AR-15.3-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5891 review R4-2 - an integer argument written as
      *> an arithmetic expression under ARITHMETIC IS STANDARD-BINARY
      *> draws the mode's own refusal (COBOL0806, kb/Work PB4463),
      *> never a compiler crash.
      *>
      *> §2023:15.3 AR6 (cite.py --check): "An arithmetic
      *> expression that will always result in an integer value
      *> or an integer data item shall be specified". The type 6
      *> classifier asked the standard-binary intermediate's
      *> precision, which no expression is evaluated in, and the
      *> compiler stopped with an unhandled exception.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5891K.
       OPTIONS.
           ARITHMETIC IS STANDARD-BINARY.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N  PIC 99 VALUE 3.
       01  R  PIC 9(18).
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE R = FUNCTION FACTORIAL (N * (N + 1) / 2).
           STOP RUN.
