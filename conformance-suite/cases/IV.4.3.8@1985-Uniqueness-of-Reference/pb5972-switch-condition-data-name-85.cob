      *>--- conformance-case
      *> id: pb5972-switch-condition-data-name-85
      *> rules: [1985-SR-IV.4.3.8.1-L2.1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5972. §1985:IV.4.2.2.1.1 (cite.py: OK) puts
      *> "condition-names, data-names, and record-names" in one
      *> disjoint set, the switch-status condition-name SWON of
      *> SPECIAL-NAMES among them, and §1985:IV.4.3.8.1 SRL2.1
      *> (cite.py: OK): "For each nonunique user-defined name that is
      *> explicitly referenced, uniqueness must be established
      *> through a sequence of qualifiers which precludes any
      *> ambiguity of reference." DISPLAY SWON OF R1 reaches the data
      *> item alone and is legal; the unqualified DISPLAY SWON names
      *> the data item and the switch's condition-name alike, so it
      *> is refused (COBOL1639). A compiler that resolves SWON in the
      *> data-names alone compiles it and shows 1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5972D.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SWITCH-1 IS SW1 ON STATUS IS SWON.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  R1.
           05  SWON        PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       MAIN-1.
           DISPLAY SWON OF R1.
           DISPLAY SWON.
           STOP RUN.
