      *>--- conformance-case
      *> id: g2cd01_one_line_directives
      *> rules: [2002-SR-7.2.2-1, 2002-SR-7.2.2-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.2.2 SR1 - "A compiler directive shall be specified on
      *>   one line, except for the EVALUATE and the IF directives for
      *>   which specific rules are specified." The 2002 directive set
      *>   includes FLAG-85 (§2002:7.2.11) and FLAG-NATIVE-ARITHMETIC
      *>   (§2002:7.2.12), each written here whole on one line, between
      *>   statements of the procedure division (§2002:7.2.11.2 SR1,
      *>   §2002:7.2.12.2 SR1). The >>IF / >>ELSE / >>END-IF directive
      *>   spans several lines under its own rules (§2002:7.2.13.2).
      *> §2002:7.2.2 SR2 - "A compiler directive shall be preceded only
      *>   by zero, one, or more space characters." The directives start
      *>   at column 8 (zero spaces before them in the program-text
      *>   area, §2002:7.2.2 SR3), column 9 (one) and column 16 (more).
      *> EXPECTED OUTPUT, DERIVED (why each leg can fail):
      *>   A        the program compiles: a one-line FLAG-85 or
      *>            FLAG-NATIVE-ARITHMETIC directive with zero, one or
      *>            more leading spaces is permitted (a refusal of any
      *>            of them fails the compile and prints nothing).
      *>   B        FLAG-85 / FLAG-NATIVE-ARITHMETIC only flag
      *>            (§2002:7.2.11.3 GR1, §2002:7.2.12.3 GR1); they change
      *>            no statement's execution.
      *>   IF-ONE   >>IF 1 = 1 is TRUE, text-1 is included
      *>            (§2002:7.2.13.3 GR2); the multi-line IF directive is
      *>            the exception SR1 names.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CD01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 9 VALUE 0.
       PROCEDURE DIVISION.
           DISPLAY "A"
       >>FLAG-85 ALL ON
        >>FLAG-NATIVE-ARITHMETIC ON
           DISPLAY "B"
               >>FLAG-85 ALL OFF
       >>FLAG-NATIVE-ARITHMETIC OFF
       >>IF 1 = 1
           DISPLAY "IF-ONE"
       >>ELSE
           DISPLAY "IF-BAD"
       >>END-IF
           STOP RUN.
