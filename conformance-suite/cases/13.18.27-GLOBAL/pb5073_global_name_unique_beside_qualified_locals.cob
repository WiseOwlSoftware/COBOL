      *>--- conformance-case
      *> id: pb5073_global_name_unique_beside_qualified_locals
      *> rules: [SR-13.18.27.3-1]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5073 - the LEGAL side of the global-name uniqueness rule. 1985:IV.4.3.8.1 and 2023:8.4.2.1 forbid one name for
      *> a global item AND another data item of its source element; a NON-global name two items share stays legal, told apart
      *> by qualification (2023:8.4.2.2.3), and a GLOBAL name that no other item of the element spells is legal.
      *> conformance:negative/pb5073-global-data-name-duplicated is the refused side.
      *> RULE: cite.py --check "§2023:8.4.2.1" "The same name shall not be used both as the name of an item possessing the
      *>   global attribute and as the name of any other data item described in the source element that describes that global
      *>   data item" -> OK.
      *> DERIVATION. G-ONLY is global and spelled by one item. A is spelled by an item of GRP1 and an item of GRP2, neither
      *> global, so A OF GRP1 and A OF GRP2 name two items ("X", "Y"). The contained program reads G-ONLY through its global
      *> attribute (2023:13.18.27.4 GR2) and displays "G".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5073P.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G-ONLY PIC X VALUE "G" GLOBAL.
       01  GRP1.
           02  A PIC X VALUE "X".
       01  GRP2.
           02  A PIC X VALUE "Y".
       PROCEDURE DIVISION.
       P-MAIN.
           DISPLAY A OF GRP1 A OF GRP2 G-ONLY
           CALL "PB5073Q"
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5073Q.
       PROCEDURE DIVISION.
       Q-MAIN.
           DISPLAY G-ONLY
           EXIT PROGRAM.
       END PROGRAM PB5073Q.
       END PROGRAM PB5073P.
