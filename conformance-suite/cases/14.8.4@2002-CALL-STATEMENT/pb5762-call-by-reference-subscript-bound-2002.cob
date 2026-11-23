*>--- conformance-case
*> id: pb5762-call-by-reference-subscript-bound-2002
*> rules: [2002-GR-14.8.4.3-3]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB5762 - EC-BOUND-SUBSCRIPT AT A TABLE ELEMENT PASSED BY REFERENCE.
      *> §2002:14.8.4.3 GR3a: "If an exception condition exists, no program is called and execution proceeds as specified in
      *> general rule 3g" (cite.py --check validated), and §2002:8.4.1.2.3 GR2 (--check validated) makes a subscript greater than the
      *> highest permissible occurrence number the EC-BOUND-SUBSCRIPT condition. A table whose element is an argument
      *> BY REFERENCE with a variable subscript is held in cell storage, and a reference to any element of it locates the
      *> element by a window, not by the table accessor; the subscript's range test is owed there as at every other
      *> reference. >>TURN is COBOL-2002 (§2023:7.3.25); the refusal at 85 is pinned by pb5762-call-by-reference-subscript-below-2002.
      *>
      *> WHY EACH LEG CAN FAIL (before PB5762 the window carried no range test, so legs 1 and 2 raised nothing):
      *>   1  IX = 4 (> 3): MOVE T(IX) TO A4        -> DECL-BOUND, then "M1 END"
      *>   2  IX = 4: CALL ... USING T(IX)         -> DECL-BOUND, the callee is NOT called, then "M2 END"
      *>   3  IX = 2 (in range): the same CALL     -> the callee runs on T(2) = "BBBB", then "M3 END"
       >>TURN EC-BOUND-SUBSCRIPT CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5762A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TBL.
          05 T PIC X(4) OCCURS 3 TIMES.
       01 IX PIC 9 VALUE 4.
       01 A4 PIC X(4) VALUE "ARG4".
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-BOUND SECTION.
           USE AFTER EXCEPTION CONDITION EC-BOUND-SUBSCRIPT.
       D-BOUND-P.
           DISPLAY "DECL-BOUND [" FUNCTION EXCEPTION-STATUS "]"
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       M-P.
           MOVE "BBBB" TO T(2)
           MOVE T(IX) TO A4
           DISPLAY "M1 END"
           CALL "PB5762AS" USING T(IX)
           DISPLAY "M2 END"
           MOVE 2 TO IX
           CALL "PB5762AS" USING T(IX)
           DISPLAY "M3 END"
           STOP RUN.
       END PROGRAM PB5762A.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5762AS.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L4 PIC X(4).
       PROCEDURE DIVISION USING L4.
           DISPLAY "S RAN [" L4 "]"
           GOBACK.
       END PROGRAM PB5762AS.
