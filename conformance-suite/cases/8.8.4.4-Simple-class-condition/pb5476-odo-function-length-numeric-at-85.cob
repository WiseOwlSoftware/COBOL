      *>--- conformance-case
      *> id: pb5476-odo-function-length-numeric-at-85
      *> rules: [SR-8.8.4.4.3-8]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5476 review R1-1 - the variable-length-group lowering of FUNCTION LENGTH. §1985:VI.6.3.1.2 GR5: "If identifier-1 is a
      *> function-identifier, it must reference an alphanumeric function". LENGTH of an OCCURS DEPENDING ON group is a
      *> function-identifier too. Expected: COBOL6918 at 85; from 2002 the program compiles (§2023:8.8.4.4.3 SR8).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5476O.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N PIC 9 VALUE 3.
       01  VG.
           05  E PIC X OCCURS 1 TO 5 TIMES DEPENDING ON N.
       PROCEDURE DIVISION.
       P1.
           IF FUNCTION LENGTH (VG) IS NUMERIC
               DISPLAY "T" ELSE DISPLAY "F".
           STOP RUN.
