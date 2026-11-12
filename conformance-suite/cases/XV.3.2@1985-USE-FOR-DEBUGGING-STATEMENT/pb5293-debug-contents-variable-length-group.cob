      *>--- conformance-case
      *> id: pb5293-debug-contents-variable-length-group
      *> rules: [1985-GR-XV.3.2.4-11]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5293. §1985:XV.3.2.4 GR11 moves DEBUG-CONTENTS "as if
      *> it was an alphanumeric to alphanumeric elementary move" and GR24
      *> c fills it with the content of the referenced data item; the
      *> content of a variable-length group is only the part the OCCURS
      *> DEPENDING ON item selects (§1985:VI.5.8.4 GR3), and the rest of
      *> DEBUG-CONTENTS is space-filled. G holds 5 occurrences, then 2:
      *> the second trigger shows AB and spaces, no trace of VWXYZ.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5293.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. ANYCOMP WITH DEBUGGING MODE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9 VALUE 5.
       01 G.
          05 GE PIC X OCCURS 1 TO 5 DEPENDING ON N.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION.
           USE FOR DEBUGGING ON G.
       D1-P.
           DISPLAY "C=[" DEBUG-CONTENTS (1:8) "]".
       END DECLARATIVES.
       MAIN SECTION.
       P1.
           MOVE "VWXYZ" TO G.
           MOVE 2 TO N.
           MOVE "AB" TO G.
           STOP RUN.
