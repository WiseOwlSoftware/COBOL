      *>--- conformance-case
      *> id: g2002_debugging_lines_are_comments
      *> rules: [2002-GR-12.2.4.3-5]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:12.2.4.3 GR5 - "If the DEBUGGING MODE clause is not
      *> specified in a source unit and the source unit is not contained
      *> within a source unit including a DEBUGGING MODE clause, any
      *> debugging lines are compiled as if they were comment lines."
      *> A debugging line is one with the fixed indicator D or d in the
      *> indicator area (§2002:6.1.1) or the floating indicator >>D
      *> followed by a space (§2002:6.1.2).
      *> G2DBG02 has a SOURCE-COMPUTER paragraph without DEBUGGING MODE;
      *> its contained program G2DBG01I has no configuration section and
      *> its container has no DEBUGGING MODE clause. So every debugging
      *> line below, in both units and in the data and procedure
      *> divisions, is a comment line, and the output is exactly
      *> OUTER-1, INNER-1, OUTER-2 and DBG-COUNT=0.
      *> Each leg can fail: a compiled D, d or >>D procedure line adds a
      *> DISPLAY or a MOVE to DBG-COUNT; the data-division debugging line
      *> would declare a second DBG-COUNT (an ambiguous name, a compile
      *> error); the D line naming NO-SUCH-ITEM is a compile error if it
      *> is compiled rather than treated as a comment line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DBG02.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. X.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  DBG-COUNT PIC 9 VALUE 0.
      D01  DBG-COUNT PIC 9 VALUE 7.
       PROCEDURE DIVISION.
           DISPLAY "OUTER-1".
      D    DISPLAY "UPPER-D".
      d    DISPLAY "LOWER-D".
           >>D DISPLAY "FLOAT-D".
      D    ADD 1 TO DBG-COUNT.
      d    ADD 1 TO DBG-COUNT.
           >>D ADD 1 TO DBG-COUNT.
      D    MOVE 1 TO NO-SUCH-ITEM.
           CALL "G2DBG01I".
           DISPLAY "OUTER-2".
           DISPLAY "DBG-COUNT=" DBG-COUNT.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DBG01I.
       PROCEDURE DIVISION.
           DISPLAY "INNER-1".
      D    DISPLAY "INNER-UPPER-D".
      d    DISPLAY "INNER-LOWER-D".
           >>D DISPLAY "INNER-FLOAT-D".
           GOBACK.
       END PROGRAM G2DBG01I.
       END PROGRAM G2DBG02.
