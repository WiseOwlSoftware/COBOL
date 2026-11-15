      *>--- conformance-case
      *> id: g2src01_debugging_mode_contained
      *> rules: [2002-GR-12.2.4.3-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:12.2.4.3 GR1 - "All clauses of the SOURCE-COMPUTER
      *> paragraph apply to the source unit in which they are
      *> explicitly or implicitly specified and to any source unit
      *> contained within that source unit." At 2002 the paragraph's
      *> clauses are computer-name-1 and WITH DEBUGGING MODE
      *> (§2002:12.2.4.1), so the rule carries DEBUGGING MODE into
      *> contained programs, where §2002:12.2.4.3 GR4 then applies:
      *> "If the DEBUGGING MODE clause is specified in a source unit,
      *> all debugging lines are compiled as if the debugging
      *> indicator was replaced by three spaces."
      *>   cite.py --check "§2002:12.2.4.3 GR1" "All clauses of the
      *>   SOURCE-COMPUTER paragraph apply to the source unit in which
      *>   they are explicitly or implicitly specified and to any
      *>   source unit contained within that source unit" -> OK
      *>   cite.py --check "§2002:12.2.4.3 GR4" "If the DEBUGGING MODE
      *>   clause is specified in a source unit, all debugging lines
      *>   are compiled as if the debugging indicator was replaced by
      *>   three spaces" -> OK
      *> G2SRC03 has the clause; G2SRC1A is contained in it and
      *> G2SRC1B is contained in G2SRC1A (an indirectly contained
      *> unit is inside the containing source unit, §2002:4 "source
      *> unit": "including any contained source units"). Neither
      *> contained program may have a configuration section
      *> (§2002:12.2.2 SR1), so each gets the clause only through GR1.
      *> Derived output, one line per DISPLAY, every debugging line
      *> compiled as ordinary source:
      *>   OUTER, OUTER-DEBUG        (fixed D line in G2SRC03, GR4)
      *>   INNER1, INNER1-DEBUG      (fixed D line, inherited, GR1)
      *>   INNER1-FLOAT-DEBUG        (floating >>D line, §2002:6.1.2,
      *>                              inherited, GR1)
      *>   INNER2, INNER2-DEBUG      (indirectly contained, GR1)
      *> Each *-DEBUG line fails alone: a compiler that applied the
      *> clause only to its own unit would print OUTER-DEBUG and
      *> none of the INNER ones (§2002:12.2.4.3 GR5 would make
      *> them comment lines).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SRC03.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. X WITH DEBUGGING MODE.
       PROCEDURE DIVISION.
           DISPLAY "OUTER".
      D    DISPLAY "OUTER-DEBUG".
           CALL "G2SRC1A".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SRC1A.
       PROCEDURE DIVISION.
           DISPLAY "INNER1".
      D    DISPLAY "INNER1-DEBUG".
           >>D DISPLAY "INNER1-FLOAT-DEBUG".
           CALL "G2SRC1B".
           GOBACK.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SRC1B.
       PROCEDURE DIVISION.
           DISPLAY "INNER2".
      D    DISPLAY "INNER2-DEBUG".
           GOBACK.
       END PROGRAM G2SRC1B.
       END PROGRAM G2SRC1A.
       END PROGRAM G2SRC03.
