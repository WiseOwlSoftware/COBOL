      *>--- conformance-case
      *> id: pb4240_keyword_spelled_with_dotted_capital_i
      *> rules: [GR-8.1.3.2-4, GR-7.3.8.4.4-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4240 - ISO 8.1.3.2 GR4 b): "An uppercase extended
      *> letter is treated as though it were folded to its corresponding
      *> lowercase extended letter, if any, in accordance with the case
      *> mapping described in Annex C", and Annex C.2 maps U+0130
      *> (capital I with dot above) to i (0130,0069), the fold WiseOwl
      *> compares every COBOL word by (DESIGN-frontend-grammar 3.13). So
      *> DEFİNED, İS and DİSPLAY below are the words DEFINED, IS and
      *> DISPLAY: the 7.3.8.4.4 defined condition and the DISPLAY
      *> statement. X is defined, so the first >>IF holds and the second
      *> does not. Expected: DEFINED, once. It fails when a keyword is
      *> compared by the host case mapping, which keeps U+0130 apart
      *> from I: the defined condition is then malformed (COBOL1619) and
      *> the DISPLAY statement a parse error. Extended letters are a
      *> COBOL 2002 introduction (8.3.2.1), so the 85 twin is refused:
      *> pb4240-keyword-spelled-with-dotted-capital-i-85.
       >>DEFINE X 1
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4240KW.
       PROCEDURE DIVISION.
       >>IF X DEFİNED
           DİSPLAY "DEFINED".
       >>END-IF
       >>IF X İS NOT DEFİNED
           DISPLAY "NOT DEFINED".
       >>END-IF
           STOP RUN.
