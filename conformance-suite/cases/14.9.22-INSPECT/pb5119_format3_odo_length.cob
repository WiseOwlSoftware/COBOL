      *>--- conformance-case
      *> id: pb5119_format3_odo_length
      *> rules: [GR-14.9.22.4-19]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5119 - a format 3 INSPECT of an OCCURS DEPENDING ON group whose TALLYING changes the DEPENDING ON item.
      *>
      *> §2023:14.9.22.4 GR19 (checked with scripts/spec/cite.py --check): a format 3 INSPECT "is interpreted and executed
      *> as though two successive INSPECT statements", and "Item identification of any identifier in the format 2
      *> statement is done only once before executing the format 1 statement".  §2023:14.6.4 step 6 (checked) lists
      *> "length evaluation for an occurs-depending group item" among the item identification steps; the 2002 text
      *> (§2002:14.8.21.3 GR18, §2002:14.5.4 step 6) says the same of a variable-length data item.  So the REPLACING half
      *> works over the length identifier-1 had when the statement began, 3, whatever TALLYING did to N.
      *> (At --std 85 the length is read at the start of each successive statement: pb5119_format3_odo_length_1985.)
      *>
      *> L1: N = 3, G = "ABABAB" (stored at N = 6).  TALLYING counts the A of the 3 characters "ABA": 2, so N = 3 + 2 = 5.
      *>     REPLACING CHARACTERS BY "Z" covers the 3 identified characters: "ZZZ" and positions 4 and 5 keep "BA".
      *>     LENGTH 5 ZZZBA.  Read at its maximum (N = 6) the group is "ZZZBAB".
      *> L2 (control, the counter is no DEPENDING ON object): "ABABAB" at N = 3, TALLYING C FOR ALL "A" REPLACING ALL
      *>     "B" BY "Y": C = 2, "AYA" and the rest unchanged: CONTROL 02 AYABAB.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5119ODO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N   PIC 9.
       01  C   PIC 99 VALUE 0.
       01  G.
           05  E  PIC X OCCURS 1 TO 6 TIMES DEPENDING ON N.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 6 TO N.
           MOVE "ABABAB" TO G.
           MOVE 3 TO N.
           INSPECT G TALLYING N FOR ALL "A"
               REPLACING CHARACTERS BY "Z".
           DISPLAY "LENGTH " N " " G.
           MOVE 6 TO N.
           DISPLAY "FULL " G.
           MOVE "ABABAB" TO G.
           MOVE 3 TO N.
           INSPECT G TALLYING C FOR ALL "A"
               REPLACING ALL "B" BY "Y".
           MOVE 6 TO N.
           DISPLAY "CONTROL " C " " G.
           STOP RUN.
