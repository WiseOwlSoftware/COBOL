      *>--- conformance-case
      *> id: pb4903-debug-item-group
      *> rules: []
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4903. X3.23-1985 USE FOR DEBUGGING general rule 10
      *> describes DEBUG-ITEM as a GROUP of DEBUG-LINE, DEBUG-NAME,
      *> DEBUG-SUB-1 to DEBUG-SUB-3 and DEBUG-CONTENTS, each member
      *> followed by a one-character FILLER, and DEBUG-SUB-n as
      *> PICTURE S9999 SIGN IS LEADING SEPARATE. So the group takes
      *> the group rules of the statements: INITIALIZE works through
      *> its elementary items by category (ISO/IEC 1989:2023
      *> 14.9.20.4 GR4, GR5a2 and GR6: FILLER left alone, numeric to
      *> zero, alphanumeric to spaces, a REPLACING phrase only for
      *> the categories it names), and MOVE CORRESPONDING pairs the
      *> members with same-named items of the other group (the
      *> group-operand rule, 14.9.25.3 SR12, is met).
      *> No user item can share a DEBUG-* name (reserved words at
      *> COBOL-85, so the funnel refuses the declaration), hence no
      *> pair exists between G (GN, GS) and DEBUG-ITEM; the legs
      *> prove the statements are accepted and pair nothing.
      *> A: MOVE CORRESPONDING DEBUG-ITEM TO G leaves G unchanged.
      *> B: ADD CORRESPONDING DEBUG-ITEM TO G leaves G unchanged.
      *> C: MOVE CORRESPONDING G TO DEBUG-ITEM leaves DEBUG-NAME (T)
      *>    and DEBUG-SUB-1 (+0002) as they were.
      *> D: INITIALIZE DEBUG-ITEM REPLACING NUMERIC BY 3 sets the
      *>    three DEBUG-SUB-n and leaves DEBUG-NAME as it was.
      *> E: INITIALIZE DEBUG-ITEM sets DEBUG-NAME to spaces and the
      *>    DEBUG-SUB-n to zero (spaces before the fix).
      *> F: MOVE ALL "X" TO DEBUG-ITEM stores into the FILLER
      *>    entries too, and INITIALIZE leaves FILLER alone (GR5a2):
      *>    the image keeps X in the byte after DEBUG-LINE (7) and
      *>    after DEBUG-NAME (38), DEBUG-LINE is spaces, DEBUG-SUB-1
      *>    (39 to 43) is +0000.
      *> Why each leg can fail: a group modeled as one elementary
      *> alphanumeric item refuses A to C (both operands shall be
      *> group items) and makes E store spaces into DEBUG-SUB-n.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4903G.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TB.
          02 T PIC X(2) OCCURS 3.
       01 G.
          02 GN PIC X(8) VALUE "KEEP".
          02 GS PIC 9(3) VALUE 5.
       01 W PIC X(86).
       PROCEDURE DIVISION.
       DECLARATIVES.
       DB1 SECTION.
           USE FOR DEBUGGING ON ALL REFERENCES OF T.
       DB1-B.
           MOVE CORRESPONDING DEBUG-ITEM TO G.
           DISPLAY "A [" G "]".
           ADD CORRESPONDING DEBUG-ITEM TO G.
           DISPLAY "B [" G "]".
           MOVE CORRESPONDING G TO DEBUG-ITEM.
           DISPLAY "C [" DEBUG-NAME (1:8) "] [" DEBUG-SUB-1 "]".
           INITIALIZE DEBUG-ITEM REPLACING NUMERIC BY 3.
           DISPLAY "D [" DEBUG-NAME (1:8) "] [" DEBUG-SUB-1 "] ["
              DEBUG-SUB-2 "] [" DEBUG-SUB-3 "]".
           INITIALIZE DEBUG-ITEM.
           DISPLAY "E [" DEBUG-NAME (1:8) "] [" DEBUG-SUB-1 "] ["
              DEBUG-SUB-2 "] [" DEBUG-SUB-3 "]".
           IF DEBUG-SUB-1 = 0 AND DEBUG-SUB-3 = 0
              DISPLAY "ZERO"
           END-IF.
           MOVE ALL "X" TO DEBUG-ITEM.
           INITIALIZE DEBUG-ITEM.
           MOVE DEBUG-ITEM TO W.
           DISPLAY "F [" W (1:6) "] [" W (7:1) "] [" W (38:1) "] ["
              W (39:5) "]".
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           MOVE "AB" TO T (2).
           STOP RUN.
