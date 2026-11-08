      *>--- conformance-case
      *> id: pb4623_function_overflow_alone_and_beside_operator
      *> rules: [GR-14.7.5-L2.1, GR-14.7.5-L3.2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4623 - a function's returned value that leaves the
      *> binary64 range is ONE condition whether it is the whole
      *> expression or an operand of an operator. FUNCTION EXP10 (400)
      *> is 1E400, farther from zero than the binary64 intermediate
      *> allows: 14.7.5 case 5 ("if native arithmetic is in effect and
      *> the implementor defines that the range of values allowed for
      *> the intermediate data item is to be checked", DOC-A.1-179: it
      *> is) and no-phrase rule 3 name EC-SIZE-OVERFLOW. FUNCTION EXP10
      *> (-400) is nonzero and nearer to zero than allowed: the same
      *> rules, EC-SIZE-UNDERFLOW. Alone (COMPUTE R = F) and beside an
      *> operator (COMPUTE R = 0 + F) must agree: with the phrase R
      *> keeps its value (phrase rule 1), and under EC-SIZE checking
      *> the USE declarative sees the same name both ways.
       >>TURN EC-SIZE CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4623FNOVF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R   PIC S9(9) VALUE 7.
       PROCEDURE DIVISION.
       DECLARATIVES.
       SZ SECTION.
           USE AFTER EXCEPTION CONDITION EC-SIZE.
       SZ-P.
           DISPLAY "  DECLARATIVE " FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           COMPUTE R = FUNCTION EXP10 (400)
               ON SIZE ERROR DISPLAY "ALONE OVF SIZE ERROR R=" R
           END-COMPUTE
           COMPUTE R = 0 + FUNCTION EXP10 (400)
               ON SIZE ERROR DISPLAY "BESIDE OVF SIZE ERROR R=" R
           END-COMPUTE
           COMPUTE R = FUNCTION EXP10 (-400)
               ON SIZE ERROR DISPLAY "ALONE UNF SIZE ERROR R=" R
           END-COMPUTE
           COMPUTE R = 0 + FUNCTION EXP10 (-400)
               ON SIZE ERROR DISPLAY "BESIDE UNF SIZE ERROR R=" R
           END-COMPUTE
           DISPLAY "NO PHRASE ALONE"
           COMPUTE R = FUNCTION EXP10 (400)
           DISPLAY "R=" R
           DISPLAY "NO PHRASE BESIDE"
           COMPUTE R = 0 + FUNCTION EXP10 (400)
           DISPLAY "R=" R
           STOP RUN.
