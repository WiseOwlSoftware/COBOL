      *>--- conformance-case
      *> id: pb4810-debug-sub-and-name
      *> rules: []
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4810. X3.23-1985 USE FOR DEBUGGING, the DEBUG-ITEM members
      *> a data-name trigger sets (docs/CONTROL_FLOW_DESIGN.md D20).
      *> General rule 13: "DEBUG-NAME contains the first 30 characters of the
      *> name that caused the debugging section to be executed. All
      *> qualifiers of the name are separated in DEBUG-NAME by the word 'IN'
      *> or 'OF'. Subscripts/indices, if any, are not entered into
      *> DEBUG-NAME." Rule 14: "If the reference to a data item that
      *> executed is subscripted or indexed, the occurrence number of each
      *> level is entered in DEBUG-SUB-1, DEBUG-SUB-2, DEBUG-SUB-3": the
      *> implicit description makes them S9(4) SIGN LEADING SEPARATE, and a
      *> MOVE to an alphanumeric item does not move the separate sign
      *> (ISO/IEC 1989:2023 section 14.9.25.4 GR6), so W-S1 holds the four
      *> digits of the occurrence number and a space. Rule 11: DEBUG-ITEM is
      *> space filled before each execution, so a member the reference does
      *> not use stays spaces (read here through the DEBUG-ITEM group
      *> image, characters 51 to 55, because spaces are no number). Rule 24
      *> c: DEBUG-CONTENTS is the content of the item at the time the
      *> section runs.
      *> The operand of USE is written WITHOUT subscripts (syntax rule 10),
      *> and the section runs for every occurrence a statement references.
      *> Why each leg can fail: a subscript read before the statement
      *> instead of at the reference gives other occurrence numbers; a
      *> two-level reference of a three-level table leaves DEBUG-SUB-3
      *> spaces; the qualifier chain is not the item's own name.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4810SN.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  TBL.
           02  ROW OCCURS 2.
               03  COL OCCURS 3.
                   04  CELL PIC 9 OCCURS 4 VALUE 0.
       01  R1.
           02  FLD PIC 9 VALUE 0.
       01  R2.
           02  FLD PIC 9 VALUE 0.
       01  I     PIC 9 VALUE 2.
       01  J     PIC 9 VALUE 3.
       01  K     PIC 9 VALUE 4.
       01  W-COL PIC X(4).
       01  W-NAME PIC X(10).
       01  W-CONT PIC X(4).
       01  W-S1  PIC X(5).
       01  W-S2  PIC X(5).
       01  W-S3  PIC X(5).
       01  W-ITEM PIC X(86).
       PROCEDURE DIVISION.
       DECLARATIVES.
       DBG-CELL SECTION.
           USE FOR DEBUGGING ON CELL.
       DBG-CELL-BODY.
           MOVE DEBUG-NAME TO W-NAME.
           MOVE DEBUG-CONTENTS TO W-CONT.
           MOVE DEBUG-SUB-1 TO W-S1.
           MOVE DEBUG-SUB-2 TO W-S2.
           MOVE DEBUG-SUB-3 TO W-S3.
           DISPLAY W-NAME "[" W-CONT "][" W-S1 "][" W-S2 "][" W-S3 "]".
       DBG-COL SECTION.
           USE FOR DEBUGGING ON ALL REFERENCES OF COL.
       DBG-COL-BODY.
           MOVE DEBUG-NAME TO W-NAME.
           MOVE DEBUG-CONTENTS TO W-CONT.
           MOVE DEBUG-SUB-1 TO W-S1.
           MOVE DEBUG-SUB-2 TO W-S2.
           MOVE DEBUG-ITEM TO W-ITEM.
           MOVE W-ITEM (51:5) TO W-S3.
           DISPLAY W-NAME "[" W-CONT "][" W-S1 "][" W-S2 "][" W-S3 "]".
       DBG-FLD SECTION.
           USE FOR DEBUGGING ON FLD IN R2.
       DBG-FLD-BODY.
           MOVE DEBUG-NAME TO W-NAME.
           MOVE DEBUG-CONTENTS TO W-CONT.
           MOVE DEBUG-ITEM TO W-ITEM.
           MOVE W-ITEM (39:5) TO W-S1.
           DISPLAY W-NAME "[" W-CONT "][" W-S1 "]".
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           MOVE 7 TO CELL (I, J, K).
           MOVE 8 TO CELL (1, 1, 1).
           MOVE COL (2, 3) TO W-COL.
           MOVE W-COL TO COL (1, 2).
           MOVE 1 TO FLD OF R1.
           MOVE 2 TO FLD IN R2.
           STOP RUN.
