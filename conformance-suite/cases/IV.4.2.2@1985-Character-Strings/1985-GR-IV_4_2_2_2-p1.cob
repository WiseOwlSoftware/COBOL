      *>--- conformance-case
      *> id: 1985-GR-IV_4_2_2_2-p1
      *> rules: [1985-GR-IV.4.2.2.2-p1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.2.2.2 p1 (Character-Strings). A refusal: the rule
      *> forbids line 7 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL0900 there; a compiler that accepts it fails.
      *> Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-03.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * 1985-GR-IV.4.2.2.2-p1: only nonnumeric and numeric literals:
      * a hexadecimal literal X"41" is refused at 85 (control: "A").
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P13HEX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY X"41".
           STOP RUN.
