      *>--- conformance-case
      *> id: pb3347-switch-condition-name-ambiguous-if
      *> rules: [SR-8.4.2.2.1-1, SR-8.4.2.2.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3347. SPECIAL-NAMES declares the switch condition-name S-ON and the same source element's data
      *> division declares a level-88 S-ON: one type of user-defined word (8.3.2.2.4), declared twice, so the name is
      *> not unique and 8.4.2.2.1 requires qualification ("1) No other name has the identical spelling"); 8.4.2.2.3
      *> SR1 requires "uniqueness shall be established through a sequence of qualifiers that precludes any ambiguity
      *> of reference" for every non unique name explicitly referenced. The unqualified `IF S-ON` establishes none -
      *> a switch condition-name takes no qualifier, so no spelling of the reference reaches the switch - and must be
      *> refused (COBOL1639); it used to bind the level-88 silently and print ON for an OFF switch. Refused at every
      *> edition: the qualification rules are not edition-dependent.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3347NEGIF.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SWITCH-31 ON STATUS IS S-ON.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 V PIC 9 VALUE 2.
          88 S-ON VALUE 1.
       PROCEDURE DIVISION.
       MAIN.
           IF S-ON DISPLAY "ON" ELSE DISPLAY "OFF".
           STOP RUN.
