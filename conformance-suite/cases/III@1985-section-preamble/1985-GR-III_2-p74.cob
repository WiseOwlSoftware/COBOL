      *>--- conformance-case
      *> id: 1985-GR-III_2-p74
      *> rules: [1985-GR-III.2-p74]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:III.2 p74 ((section preamble)). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/g-02.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DEEDIT.
      * III.2 p74: de-edit - the editing characters of a numeric edited
      * item are removed to obtain its unedited numeric value.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  E1              PIC $Z,ZZ9.99CR.
       01  N               PIC S9(4)V99.
       PROCEDURE DIVISION.
       P1.
           MOVE -1234.56 TO E1.
           MOVE E1 TO N.
           IF N = -1234.56 DISPLAY "DE=OK" ELSE DISPLAY "DE=BAD".
           STOP RUN.
