*>--- conformance-case
*> id: pb5762-call-by-reference-subscript-below-2002
*> rules: [2002-GR-14.8.4.3-3]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB5762 - THE EDITION FLOOR OF §2002:14.8.4.3 GR3a's EC-BOUND-SUBSCRIPT ON A BY REFERENCE ELEMENT.
      *>   cite.py --check "§2002:14.8.4.3 GR3a" "If an exception condition exists, no program is called and execution proceeds as specified in general rule 3g" -> OK
      *> The exception conditions a CALL's evaluation can set are checked only from COBOL-2002 onward: the >>TURN directive
      *> (§2023:7.3.25) is a 2002 introduction, so at --std 85 the version-conformance pass rejects it (COBOL0900). The
      *> subscripted BY REFERENCE argument is legal COBOL-85; delete the first line and it compiles at 85. The positive
      *> half is pb5762-call-by-reference-subscript-bound-2002.
       >>TURN EC-BOUND-SUBSCRIPT CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5762N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TBL.
          05 T PIC X(4) OCCURS 3 TIMES.
       01 IX PIC 9 VALUE 2.
       PROCEDURE DIVISION.
       MAIN.
           CALL "PB5762NS" USING T(IX)
           STOP RUN.
       END PROGRAM PB5762N.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5762NS.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L4 PIC X(4).
       PROCEDURE DIVISION USING L4.
           GOBACK.
       END PROGRAM PB5762NS.
