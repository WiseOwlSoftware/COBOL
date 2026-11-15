      *>--- conformance-case
      *> id: g2-sort-table-cd-no-key-anywhere
      *> rules: [2002-SR-14.8.36.2-15]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.36.2 SR15 - "The KEY phrase may be omitted only if
      *> the description of the table referenced by data-name-2
      *> contains a KEY phrase."
      *> Leg: CTAB (a table in a record under an output CD) has no KEY
      *> phrase in its OCCURS clause and SORT CTAB writes none: refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SRTN1.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD COUT FOR OUTPUT.
       01 CREC.
          05 CTAB OCCURS 3 TIMES.
             10 CK PIC 9.
       PROCEDURE DIVISION.
       MAIN.
           MOVE 3 TO CK(1) MOVE 1 TO CK(2) MOVE 2 TO CK(3)
           SORT CTAB
           STOP RUN.
