      *>--- conformance-case
      *> id: pb6011-value-list-continued-in-area-b-85
      *> rules: [1985-SR-IV.7.3.3-p3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB6011. §1985:IV.7.3.3 (cite.py: OK): "Successive sentences
      *> or entries either begin in area B of the same line" or of the next
      *> line. A condition-name's VALUE list continued in area B is one
      *> entry, an integer of it beginning that line included; the next
      *> entry's 01 begins in area A after the period. B-MID is true for 4.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. VLISTB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A PIC 9 VALUE 4.
           88 B-MID VALUE 1 2
               3 THRU 5.
       01  B PIC X VALUE "B".
       PROCEDURE DIVISION.
       P0.
           IF B-MID DISPLAY "MID " B ELSE DISPLAY "OUT " B.
           STOP RUN.
