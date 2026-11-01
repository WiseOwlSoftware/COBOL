      *>--- conformance-case
      *> id: pb4298_floating_debugging_fixed
      *> rules: []
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4298 - the FLOATING debugging indicator >>D in FIXED
      *> form (ISO/IEC 1989:2002 6.1.2: "Floating indicators may be used
      *> in fixed-form or free-form reference format"; 6.3.4: a debugging
      *> line has the characteristics of a comment line if the WITH
      *> DEBUGGING MODE clause is not specified in the SOURCE-COMPUTER
      *> paragraph). This program specifies no such clause, so the >>D
      *> line is a comment and never runs. The leg can fail: a >>D line
      *> read as program text is COBOL0001 "unexpected '>'", one read as
      *> source would display DEBUG. The same line under the clause runs
      *> (golden pb4298_floating_debugging_free).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB4298FX.
000300 PROCEDURE DIVISION.
000400     DISPLAY "A".
000500     >>D DISPLAY "DEBUG".
000600 >>D DISPLAY "AREA A".
000700     DISPLAY "B".
000800     STOP RUN.
