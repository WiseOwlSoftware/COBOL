      *>--- conformance-case
      *> id: pb5924-condname-qualified-85
      *> rules: [1985-SR-IV.4.3.8.6-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:IV.4.3.8.6 (cite.py: OK): "a condition-name must be unique or be made unique through qualification and/or subscripting"
      *> IS-ON made unique by qualification (conditional variable, or its record), E-ON by subscripting;
      *> each names exactly one condition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CNQUAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  REC-A.
           05  V1  PIC 9 VALUE 1.
               88  IS-ON  VALUE 1.
       01  REC-B.
           05  V2  PIC 9 VALUE 0.
               88  IS-ON  VALUE 1.
       01  TAB.
           05  E  PIC 9 OCCURS 3 TIMES.
               88  E-ON  VALUE 1.
       PROCEDURE DIVISION.
       P1.
           MOVE 0 TO E (1). MOVE 1 TO E (2). MOVE 0 TO E (3).
           IF IS-ON OF V1 DISPLAY "V1 ON" ELSE DISPLAY "V1 OFF".
           IF IS-ON OF V2 DISPLAY "V2 ON" ELSE DISPLAY "V2 OFF".
           IF IS-ON IN REC-A DISPLAY "A ON" ELSE DISPLAY "A OFF".
           IF IS-ON OF V2 OF REC-B DISPLAY "B ON" ELSE DISPLAY "B OFF".
           IF E-ON (1) DISPLAY "E1 ON" ELSE DISPLAY "E1 OFF".
           IF E-ON (2) DISPLAY "E2 ON" ELSE DISPLAY "E2 OFF".
           STOP RUN.
