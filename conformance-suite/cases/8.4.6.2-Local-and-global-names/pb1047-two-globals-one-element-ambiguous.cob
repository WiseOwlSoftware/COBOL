      *>--- conformance-case
      *> id: pb1047-two-globals-one-element-ambiguous
      *> rules: [GR-8.4.6.2.1-3, GR-13.18.27.4-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB1047 / PB5150: two global X items in ONE source element. §2023:8.4.6.2.1 3) b) 1. narrows a name duplicated
      *> ACROSS nested source elements to the nearest element that declares it (positive twin:
      *> 85/pb1047_nearest_declaring_element); it does not make two same-named globals of ONE element legal. §2023:8.4.2.1
      *> forbids them at the DECLARATION: "The same name shall not be used both as the name of an item possessing the global
      *> attribute and as the name of any other data item described in the source element that describes that global data
      *> item", and the subordinates of a GLOBAL record possess the attribute (§2023:13.18.27.4 GR1: "All data-names
      *> subordinate to a global name are global names"). The refusal is COBOL6862 at the later X, before any reference
      *> (it was COBOL1639, at the reference, before kb/Work PB5150 adjudicated the screen).
      *> RULE: cite.py --check "§2023:8.4.2.1" "The same name shall not be used both as the name of an item possessing the
      *>   global attribute" -> OK; cite.py --check "§2023:13.18.27.4 GR1" "All data-names subordinate to a global name are
      *>   global names" -> OK.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1047A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G1 GLOBAL.
          05 X PIC X(3) VALUE "G1X".
       01 G2 GLOBAL.
          05 X PIC X(3) VALUE "G2X".
       PROCEDURE DIVISION.
       P-MAIN.
           CALL "PB1047B"
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1047B.
       PROCEDURE DIVISION.
       P-MAIN.
           DISPLAY "X=" X
           EXIT PROGRAM.
       END PROGRAM PB1047B.
       END PROGRAM PB1047A.
