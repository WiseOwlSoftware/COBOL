      *>--- conformance-case
      *> id: pb6011-missing-period-condition-range-85
      *> rules: [1985-SR-IV.7.4-p1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB6011. §1985:IV.7.4 (cite.py: OK): "The last clause is
      *> always terminated by a separator period". A condition-name's
      *> VALUE THRU and a 77 item's VALUE each lose their period before a
      *> level-number that begins its line in area A (§1985:IV.7.4, cite.py:
      *> OK: "Each new data description entry may begin any number of
      *> positions to the right of margin A"): one report at each level-
      *> number, where its period is missing, and nothing else.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NOPER88.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A PIC 9 VALUE 1.
           88 A-OK VALUE 1 THRU 5
       01  B PIC X.
       77  C PIC 9 VALUE 3
       77  D PIC X.
       PROCEDURE DIVISION.
       P0.
           DISPLAY A B C D.
           STOP RUN.
