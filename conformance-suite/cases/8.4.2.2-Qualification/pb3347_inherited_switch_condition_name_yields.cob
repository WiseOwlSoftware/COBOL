      *>--- conformance-case
      *> id: pb3347_inherited_switch_condition_name_yields
      *> rules: [SR-8.4.2.2.1-1]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3347. 8.4.6.1: "each source element may use identical user-defined words to name items independent
      *> of the use of these user-defined words by other source elements", and a reference to a name a CONTAINING
      *> element's configuration section declares is "to the item which that source element describes ... rather
      *> than to the item, possessing the same name, described in another source element". The outer program's
      *> SPECIAL-NAMES declares the switch condition-name S-ON; the contained program describes a level-88 of that
      *> spelling and its unqualified `IF S-ON` is the level-88 - the pair is NOT non-unique (they belong to
      *> different source elements), unlike the same-element pair of pb3347_switch_condition_name_beside_level88.
      *>   cite.py --check 8.4.6.1 "rather than to the item, possessing the same name, described in another source
      *>     element" -> OK
      *> V = 1 makes the contained level-88 true -> INNER-ON; SWITCH-31 is OFF (not set externally), so a binding to
      *> the switch's ON status would print INNER-OFF. A leg fails if the contained reference reads the switch or is
      *> diagnosed as ambiguous.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3347OUTER.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SWITCH-31 ON STATUS IS S-ON.
       PROCEDURE DIVISION.
       MAIN.
           CALL "PB3347INNER".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3347INNER.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 V PIC 9 VALUE 1.
          88 S-ON VALUE 1.
       PROCEDURE DIVISION.
       MAIN.
           IF S-ON DISPLAY "INNER-ON" ELSE DISPLAY "INNER-OFF".
           EXIT PROGRAM.
       END PROGRAM PB3347INNER.
       END PROGRAM PB3347OUTER.
