      *>--- conformance-case
      *> id: pb4305_sentences_before_first_paragraph_2002
      *> rules: [FMT-14.2.1]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4305 - 14.4.3: a paragraph-name may be omitted, "one
      *>   or more successive sentences following the procedure
      *>   division header or a section header" (2002 Annex F.2 item
      *>   90). The procedure division and section S2 each begin with a
      *>   sentence. Expected: "X" then "Y". It fails if either
      *>   sentence is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4305PN.
       PROCEDURE DIVISION.
       S1 SECTION.
           DISPLAY "X"
           PERFORM S2
           STOP RUN.
       S2 SECTION.
           DISPLAY "Y".
