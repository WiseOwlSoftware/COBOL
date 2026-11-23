      *>--- conformance-case
      *> id: set-index-outside-table-checked
      *> rules: [2002-GR-14.8.35.3-2, 2002-GR-14.8.35.3-3]
      *> accept: [2002]
      *> source-format: fixed
      *>---
      *> SET index-name TO a value outside its table, and UP BY / DOWN BY
      *> past either end, with EC-BOUND-SUBSCRIPT checking enabled
      *> (kb/Work PB5674).
      *> §2002:14.8.35.3 GR2a.1: "If arithmetic-expression-1 is
      *> specified, a subscript is determined by evaluating
      *> arithmetic-expression-1 as described in 8.4.1.2, Subscripts.
      *> If the evaluation of that subscript is successful, index-name-1
      *> is set to a value causing it to refer to the table element
      *> that corresponds in occurrence number to that subscript;
      *> otherwise the execution of the SET statement is unsuccessful
      *> and the content of the receiving operand is unchanged."
      *> §2002:8.4.1.2.3 GR2: a subscript "less than one or greater
      *> than the highest permissible occurrence number" sets the
      *> EC-BOUND-SUBSCRIPT exception condition to exist.
      *> §2002:14.8.35.3 GR3b: the index incremented (UP BY) or
      *> decremented (DOWN BY) is a valid subscript, "otherwise the
      *> EC-BOUND-SUBSCRIPT exception condition is set to exist, the
      *> execution of the SET statement is unsuccessful, and the
      *> content of the receiving operand is unchanged."
      *> T1 is a table of 10.  A: SET TO 3 is valid.  B: TO 11 raises,
      *> IX1 stays 3.  C: TO 10, the highest, is valid.  D: UP BY 1
      *> would give 11: raises, stays 10.  E: DOWN BY 9 gives 1, valid.
      *> F: DOWN BY 1 would give 0: raises, stays 1.  G: TO 0 raises,
      *> stays 1.  H and I: both receivers of SET IX1 IX2 TO 4 take
      *> it, T2 being a table of 5.  J: index-name-2 as the sender
      *> (GR2a.3) is placed unchanged and tests nothing, so IX2 takes 8
      *> although T2 has 5 occurrences.
      *> Each leg fails if the SET stores the value, runs no
      *> declarative, or tests the table for an index sender.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SETIXTBL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T1.
          05 E1 PIC X OCCURS 10 INDEXED BY IX1.
       01 T2.
          05 E2 PIC X OCCURS 5 INDEXED BY IX2.
       01 N PIC 99.
       01 NX PIC XX.
       01 V PIC 99 VALUE 11.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION.
           USE AFTER EXCEPTION CONDITION EC-BOUND-SUBSCRIPT.
       D1P.
           DISPLAY "EC".
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       M SECTION.
       M1.
           >>TURN EC-BOUND-SUBSCRIPT CHECKING ON
           SET IX1 TO 3.
           SET N TO IX1. MOVE N TO NX. DISPLAY "A=" NX.
           SET IX1 TO V.
           SET N TO IX1. MOVE N TO NX. DISPLAY "B=" NX.
           SET IX1 TO 10.
           SET N TO IX1. MOVE N TO NX. DISPLAY "C=" NX.
           SET IX1 UP BY 1.
           SET N TO IX1. MOVE N TO NX. DISPLAY "D=" NX.
           SET IX1 DOWN BY 9.
           SET N TO IX1. MOVE N TO NX. DISPLAY "E=" NX.
           SET IX1 DOWN BY 1.
           SET N TO IX1. MOVE N TO NX. DISPLAY "F=" NX.
           SET IX1 TO 0.
           SET N TO IX1. MOVE N TO NX. DISPLAY "G=" NX.
           SET IX1 IX2 TO 4.
           SET N TO IX1. MOVE N TO NX. DISPLAY "H=" NX.
           SET N TO IX2. MOVE N TO NX. DISPLAY "I=" NX.
           SET IX1 TO 8.
           SET IX2 TO IX1.
           SET N TO IX2. MOVE N TO NX. DISPLAY "J=" NX.
           STOP RUN.
