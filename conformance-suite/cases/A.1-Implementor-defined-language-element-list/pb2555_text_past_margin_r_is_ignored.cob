      *>--- conformance-case
      *> id: pb2555_text_past_margin_r_is_ignored
      *> rules: [DOC-A.1-158]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO/IEC 1989:2023 6.3.1: the program-text area terminates with the character position immediately
      *> to the left of margin R, and the rightmost position is the implementor's (Annex A.1 item 158: here
      *> position 72, docs/CONFORMANCE.md DOC-A.1-158). What a line carries past it is not program text.
      *>   cite.py --check 6.3.1 'The program-text area begins in character position 8 and terminates
      *>   with the character position immediately to the left of margin R.'  -> OK
      *> The DISPLAY statement below ends at position 72 with the literal "A"; "B" stands from position
      *> 74, past margin R, so it is not part of the statement and the line shows A, not AB. The same
      *> holds of the identification tag every line carries in positions 73-80. If the reference format
      *> read past margin R the output would be AB (DISPLAY of two literals).
      *> Derivation: OUT=[A]
       IDENTIFICATION DIVISION.                                         PB255501
       PROGRAM-ID. PB2555P.                                             PB255502
       PROCEDURE DIVISION.                                              PB255503
           DISPLAY "A"                                                   "B"
           STOP RUN.                                                    PB255505
