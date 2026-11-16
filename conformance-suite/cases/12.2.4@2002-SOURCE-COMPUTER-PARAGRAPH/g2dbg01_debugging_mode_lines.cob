      *>--- conformance-case
      *> id: g2dbg01_debugging_mode_lines
      *> rules: [2002-GR-12.2.4.3-4]
      *> accept: [2002]
      *> files: [G2DBG01C.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:12.2.4.3 GR4 - "If the DEBUGGING MODE clause is
      *> specified in a source unit, all debugging lines are compiled
      *> as if the debugging indicator was replaced by three spaces."
      *> cite.py --check -> OK
      *> §2002:6.1.1 indicator table: D and d are each a fixed
      *> "debugging indicator"; §2002:6.1.2: >>D is "a debugging line
      *> when followed by at least one space character" (cite.py OK).
      *> §2002:6.2.7 - "A debugging line is permitted anywhere in a
      *> compilation unit after the SOURCE-COMPUTER paragraph" (cite.py
      *> OK), so a data description entry may be one.
      *> §2002:7.1.2.3 GR13 - a text-word copied from a debugging line
      *> of library text "appears on a debugging line" (cite.py OK).
      *> DERIVATION OF THE EXPECTED OUTPUT (every debugging line
      *> compiled as ordinary source):
      *>   NORMAL; UPPER D (D); LOWER D (d); FLOAT D (>>D);
      *>   DATA DBG - DBGV is declared on a D line and displayed from
      *>   one;
      *>   SPLIT-DEBUG - the D line continues the DISPLAY of the line
      *>   before (the indicator becomes spaces, so the second operand
      *>   joins the statement);
      *>   CNT 2 - ADD on a normal line and ADD on a D line both
      *>   execute;
      *>   COPY NORMAL, COPY DEBUG - the library's D line is compiled
      *>   too.
      *> EACH LEG CAN FAIL: a debugging line treated as a comment drops
      *> its line (or leaves DBGV undeclared, or prints SPLIT- and CNT
      *> 1); a lower-case d not recognized as an indicator is a syntax
      *> error; a COPY D line not compiled drops COPY DEBUG.
      *> The obsolete-element warnings COBOL0903 do not fail the case.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DBG01.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. G2-COMPUTER WITH DEBUGGING MODE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
      D01  DBGV PIC X(8) VALUE "DATA DBG".
       01  CNT  PIC 9 VALUE 0.
       PROCEDURE DIVISION.
           DISPLAY "NORMAL"
      D    DISPLAY "UPPER D"
      d    DISPLAY "LOWER D"
           >>D DISPLAY "FLOAT D"
      D    DISPLAY DBGV
           DISPLAY "SPLIT-"
      D        "DEBUG"
           ADD 1 TO CNT
      D    ADD 1 TO CNT
           DISPLAY "CNT " CNT
           COPY G2DBG01C.
           STOP RUN.
