      *>--- conformance-case
      *> id: 1985-GR-IV_5_2-p2
      *> rules: [1985-GR-IV.5.2-p2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.5.2 p2 (A COBOL SOURCE PROGRAM). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-06.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * 1985-GR-IV.5.2-p2: the end of the source program is the absence
      * of further lines (no end program header). p02 ends with the
      * end program header. Expect: END BY ABSENCE
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ENDABS.
       PROCEDURE DIVISION.
       P0.
           DISPLAY "END BY ABSENCE".
           STOP RUN.
