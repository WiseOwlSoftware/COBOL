      *>--- conformance-case
      *> id: pb4420-convert-format-word-spelled-with-dotted-capital-i
      *> rules: [GR-8.1.3.2-3, GR-8.1.3.2-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4420 - the source-format and destination-format of
      *> FUNCTION CONVERT are keywords (15.19.2, cite.py --check: OK),
      *> matched on the basic letters alone (8.1.3.2 3) a)); the fold of
      *> 4) b) governs user-defined words only and applies no pair from
      *> U+0130 to i. NATİONAL is therefore no format keyword:
      *> COBOL1514. IntrinsicBinder.IsConvertFormatWord used to compare
      *> the word through the fold and accepted it as NATIONAL. The
      *> control is the same call written NATIONAL. It fails when that
      *> comparison folds U+0130.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4420C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 XM PIC X(4) VALUE "ABCD".
       01 CVR PIC N(8).
       PROCEDURE DIVISION.
           MOVE FUNCTION CONVERT(XM ANY NATİONAL HEX) TO CVR.
           DISPLAY CVR.
           STOP RUN.
