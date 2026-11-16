      *>--- conformance-case
      *> id: g2-renames-odo-in-range
      *> rules: [2002-SR-13.16.43.2-7]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.43.2 SR7 - "None of the items within the range,
      *> including data-name-2 and data-name-3, if specified, shall be ...
      *> a variable-occurrence data item", which §2002:4 defines as "A
      *> table element that is repeated a variable number of times". The
      *> range N THRU G includes T, described OCCURS 1 TO 3 DEPENDING ON N.
      *> Neither operand has an OCCURS clause nor is subordinate to one
      *> (G contains T; it is not subordinate to it), so SR3 does not reach
      *> it and only SR7 rejects the 66 entry, at its line. Fails if the
      *> entry is accepted.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2REN28.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WREC.
           05 N PIC 9.
           05 G.
              10 C PIC X.
              10 T PIC X OCCURS 1 TO 3 DEPENDING ON N.
       66  X1 RENAMES N THRU G.
       PROCEDURE DIVISION.
           DISPLAY C
           STOP RUN.
