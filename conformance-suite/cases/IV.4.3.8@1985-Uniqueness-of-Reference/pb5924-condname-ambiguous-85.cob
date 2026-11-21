      *>--- conformance-case
      *> id: pb5924-condname-ambiguous-85
      *> rules: [1985-SR-IV.4.3.8.6-p1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:IV.4.3.8.6 (cite.py: OK): "a condition-name must be unique or be made unique through qualification and/or subscripting"
      *> IS-ON is a condition-name of V1 and of V2; the unqualified reference is not unique: refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CNAMB.
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
           IF IS-ON DISPLAY "ON".
           STOP RUN.
