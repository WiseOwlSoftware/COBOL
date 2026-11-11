      *>--- conformance-case
      *> id: pb5119_format3_odo_forms
      *> rules: [GR-14.9.22.4-19]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5119 review R1-1 - every FORM of a format 3 INSPECT's identifier-1 whose identification reads an
      *> OCCURS DEPENDING ON object that its own TALLYING counter changes, not only the plain occurs-depending group
      *> (pb5119_format3_odo_length).
      *>
      *> §2023:14.9.22.4 GR19 (checked with scripts/spec/cite.py --check): "Item identification of any identifier in the
      *> format 2 statement is done only once before executing the format 1 statement".  §2023:14.6.4 (checked) makes
      *> step 6, "length evaluation for an occurs-depending group item", part of item identification, before step 7,
      *> reference modification, so an omitted ref-mod length runs to the end of the group AS IDENTIFIED.
      *> §2023:13.18.38.4 GR7 (checked) tests data-name-1's bounds "At the time the subject of entry is referenced",
      *> which for identifier-1 is its identification.  The 2002 text says the same (§2002:14.8.21.3 GR18, §2002:14.5.4
      *> step 6, checked).  N is a TALLYING counter and the DEPENDING ON object; each line DISPLAYs N after the
      *> statement and the record read at its maximum length.  (At --std 85: pb5119_format3_odo_forms_1985.)
      *>
      *> L1 G (2:):   N = 3, G = "ABA", G (2:) = "BA", one A: N = 4; "BA" becomes "ZZ": HAZZBAB.
      *> L2 R (2:):   N = 3, R = "HABA", R (2:) = "ABA", two A: N = 5; "ZZZ": HZZZBAB.
      *> L3 T2 (1:):  N = 1, T2 = "AB" (one occurrence of EG), one A: N = 2; "ZZ": HZZABAB.
      *> L4 G (2:2):  N = 3, G (2:2) = "BA", one A: N = 4; "ZZ": HAZZBAB.
      *> L5 EG (1):   N = 1, EG (1) = "AB", one A: N = 2; "ZZ": HZZABAB.
      *> L6 EG (1) under >>TURN EC-BOUND-ODO CHECKING ON, the table at its maximum: N = 3, EG (1) = "AA", two A: N = 5,
      *>     past integer-2 (3).  N was 3, within the bounds, when EG (1) was identified, and it is identified only once,
      *>     so no EC-BOUND-ODO is raised: "ZZ", HZZAAAA.
       >>TURN EC-BOUND-ODO CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5119FORMS.
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
           MOVE 3 TO N. MOVE "HAAAAAA" TO R2.
           INSPECT EG (1) TALLYING N FOR ALL "A"
               REPLACING CHARACTERS BY "Z".
           DISPLAY "L6 " N.
           MOVE 3 TO N. DISPLAY "L6 " R2.
           STOP RUN.
