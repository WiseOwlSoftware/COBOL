      *>--- conformance-case
      *> id: pb5119_format3_odo_length_1985
      *> rules: [GR-14.9.22.4-19]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5119 - a format 3 INSPECT of an OCCURS DEPENDING ON group whose TALLYING changes the DEPENDING ON item,
      *> at COBOL-85.
      *>
      *> §1985:VI.6.18.4 GR17 (checked with scripts/spec/cite.py --check): a format 3 INSPECT "is interpreted and executed
      *> as though two successive INSPECT statements", and only "Subscripting associated with any identifier in the format 2
      *> statement is evaluated only once before executing the format 1 statement" - the length of the group is not named.
      *> §1985:VI.6.18.4 GR1 sizes identifier-1 as a sending item and §1985:VI.5.8.4 GR3 (both checked) uses "the value of
      *> the data item referenced by data-name-1 at the start of the operation", so the REPLACING statement, the second of
      *> the two, reads N at ITS start, after TALLYING raised it.  (From COBOL-2002 item identification, length included, is
      *> done once for the whole statement: pb5119_format3_odo_length.)
      *>
      *> L1: N = 3, G = "ABABAB" (stored at N = 6).  TALLYING counts the A of the 3 characters "ABA": 2, so N = 5.
      *>     REPLACING CHARACTERS BY "Z" then covers the 5 characters of the group: LENGTH 5 ZZZZZ.  Read at its maximum
      *>     (N = 6) the group is "ZZZZZB" (position 6 was never touched).
      *> L2 (control, the counter is no DEPENDING ON object): "ABABAB" at N = 3, TALLYING C FOR ALL "A" REPLACING ALL
      *>     "B" BY "Y": C = 2, "AYA" and the rest unchanged: CONTROL 02 AYABAB.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5119ODO85.
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
