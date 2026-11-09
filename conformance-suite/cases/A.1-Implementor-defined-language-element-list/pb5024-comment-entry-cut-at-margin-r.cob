      *>--- conformance-case
      *> id: pb5024-comment-entry-cut-at-margin-r
      *> rules: [DOC-A.1-158]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 1989:2023 6.3.1 (A.1 item 158): margin R is position 72.
      *>   cite.py --check 6.3.1 'Margin R is immediately to the right
      *>   of the rightmost character position of the program-text
      *>   area.'  -> OK
      *> The AUTHOR comment-entry below has a word ending at position
      *> 72 with more text at position 73. The text past margin R is
      *> dropped, and a comment-entry is commentary, so the cut
      *> changes no program and COBOL6688 is not given (kb/Work
      *> PB5024; unit test). The program runs as written.
      *> Derivation: OUT=[OK]
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5024P.
       AUTHOR. SOME NAME THAT RUNS TO THE MARGIN                       XYZ-REST.
       PROCEDURE DIVISION.
       PB5024-MAIN.
           DISPLAY "OK"
           STOP RUN.
