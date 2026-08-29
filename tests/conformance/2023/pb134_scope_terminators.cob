      *> kb/Work PB134 - END-ACCEPT (ISO 14.9.1.2, 2002+): the terminator token existed in the lexer and
      *> NO parser rule - `ACCEPT X END-ACCEPT` was a syntax error at every edition while its edition gate
      *> sat registered and dead. It now parses and the 85 leg of the version matrix draws COBOLNET0816
      *> (END-DISPLAY's twin gate went live in the same change - it was accepted at 85 with no gate at all).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. EA2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           ACCEPT W FROM TIME END-ACCEPT
           DISPLAY "OK"
           STOP RUN.
