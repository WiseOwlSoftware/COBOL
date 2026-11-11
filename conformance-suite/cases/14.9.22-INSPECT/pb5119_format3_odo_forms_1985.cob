      *>--- conformance-case
      *> id: pb5119_format3_odo_forms_1985
      *> rules: [GR-14.9.22.4-19]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5119 review R1-1 - every FORM of a format 3 INSPECT's identifier-1 whose length depends on an OCCURS
      *> DEPENDING ON object that its own TALLYING counter changes, at COBOL-85.
      *>
      *> §1985:VI.6.18.4 GR17 (checked with scripts/spec/cite.py --check): a format 3 INSPECT "is interpreted and executed
      *> as though two successive INSPECT statements", and only "Subscripting associated with any identifier in the format 2
      *> statement is evaluated only once before executing the format 1 statement".  §1985:VI.6.18.4 GR1 sizes identifier-1
      *> as a sending item, and §1985:VI.5.8.4 GR3 a (both checked) takes "the value of the data item referenced by
      *> data-name-1 at the start of the operation", so the REPLACING statement, the second of the two, reads N at ITS
      *> start, after TALLYING raised it, and an omitted reference-modification length runs to the end of the group at
      *> that length.  (From COBOL-2002 the length is identified once: pb5119_format3_odo_forms.)
      *>
      *> L1 G (2:):   N = 3, G (2:) = "BA", one A: N = 4.  REPLACING: G = "ABAB", G (2:) = "BAB": "ZZZ": HAZZZAB.
      *> L2 R (2:):   N = 3, R (2:) = "ABA", two A: N = 5.  REPLACING: R = "HABABA", R (2:) = "ABABA": HZZZZZB.
      *> L3 T2 (1:):  N = 1, T2 = "AB", one A: N = 2.  REPLACING: T2 = "ABAB": HZZZZAB.
      *> L4 G (2:2):  N = 3, G (2:2) = "BA", one A: N = 4.  REPLACING: G (2:2) of "ABAB" is still "BA": HAZZBAB.
      *> L5 EG (1):   N = 1, EG (1) = "AB", one A: N = 2.  REPLACING: EG (1) is "AB" at any N: HZZABAB.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5119FORMS85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N   PIC 9.
       01  R.
           05  H  PIC X.
           05  G.
               10  E  PIC X OCCURS 1 TO 6 TIMES DEPENDING ON N.
       01  R2.
           05  H2 PIC X.
           05  T2.
               10  EG OCCURS 1 TO 3 TIMES DEPENDING ON N.
                   15  EA PIC X.
                   15  EB PIC X.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 6 TO N. MOVE "HABABAB" TO R. MOVE 3 TO N.
           INSPECT G (2:) TALLYING N FOR ALL "A"
               REPLACING CHARACTERS BY "Z".
           DISPLAY "L1 " N.
           MOVE 6 TO N. DISPLAY "L1 " R.
           MOVE "HABABAB" TO R. MOVE 3 TO N.
           INSPECT R (2:) TALLYING N FOR ALL "A"
               REPLACING CHARACTERS BY "Z".
           DISPLAY "L2 " N.
           MOVE 6 TO N. DISPLAY "L2 " R.
           MOVE 3 TO N. MOVE "HABABAB" TO R2. MOVE 1 TO N.
           INSPECT T2 (1:) TALLYING N FOR ALL "A"
               REPLACING CHARACTERS BY "Z".
           DISPLAY "L3 " N.
           MOVE 3 TO N. DISPLAY "L3 " R2.
           MOVE 6 TO N. MOVE "HABABAB" TO R. MOVE 3 TO N.
           INSPECT G (2:2) TALLYING N FOR ALL "A"
               REPLACING CHARACTERS BY "Z".
           DISPLAY "L4 " N.
           MOVE 6 TO N. DISPLAY "L4 " R.
           MOVE 3 TO N. MOVE "HABABAB" TO R2. MOVE 1 TO N.
           INSPECT EG (1) TALLYING N FOR ALL "A"
               REPLACING CHARACTERS BY "Z".
           DISPLAY "L5 " N.
           MOVE 3 TO N. DISPLAY "L5 " R2.
           STOP RUN.
