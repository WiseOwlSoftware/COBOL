      *>--- conformance-case
      *> id: 1985-GR-III_2-p6
      *> rules: [1985-GR-III.2-p6]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:III.2 p6 ((section preamble)). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/g-01.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * 1985-GR-III.2-p6: an alphanumeric character is any character
      * of the computer's character set: each of the 256 ordinal
      * positions round-trips through a PIC X item.
      * Expect: CHARS 256 OK
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P6AN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  C   PIC X.
       01  I   PIC 999.
       01  N   PIC 999 VALUE 0.
       PROCEDURE DIVISION.
       P0.
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > 256
               MOVE FUNCTION CHAR (I) TO C
               IF FUNCTION ORD (C) = I
                   ADD 1 TO N
               END-IF
           END-PERFORM.
           IF N = 256
               DISPLAY "CHARS " N " OK"
           ELSE
               DISPLAY "CHARS " N " BAD"
           END-IF.
           STOP RUN.
