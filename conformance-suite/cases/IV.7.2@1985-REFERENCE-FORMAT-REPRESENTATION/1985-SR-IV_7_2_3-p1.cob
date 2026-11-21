      *>--- conformance-case
      *> id: 1985-SR-IV_7_2_3-p1
      *> rules: [1985-SR-IV.7.2.3-p1]
      *> accept: [85]
      *> files: [P12CPY]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.7.2.3 p1 (REFERENCE FORMAT REPRESENTATION). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-07.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).

       IDENTIFICATION DIVISION.
       PROGRAM-ID. P12BL.
      * 1985 IV.7.2.3: a blank line can appear anywhere (here the
      * first line, and inside a statement); IV.7.2.4: comment lines
      * in library text (P12CPY) and inside a statement.
       PROCEDURE DIVISION.

       MAIN-P.
           DISPLAY

      * A COMMENT LINE INSIDE A STATEMENT.
               "1 OK".
           COPY P12CPY.
           STOP RUN.
