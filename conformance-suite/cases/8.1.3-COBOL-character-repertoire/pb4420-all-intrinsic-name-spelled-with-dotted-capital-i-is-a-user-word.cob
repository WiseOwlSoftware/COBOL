      *>--- conformance-case
      *> id: pb4420-all-intrinsic-name-spelled-with-dotted-capital-i-is-a-user-word
      *> rules: [SR-12.3.8.3-13, GR-8.1.3.2-4]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4420 (it was kb/Work PB4240's refusal COBOL1649) -
      *> ISO 8.1.3.2 4) b): "An uppercase extended letter is treated as
      *> though it were folded to its corresponding lowercase extended
      *> letter, if any, in accordance with the case mapping described
      *> in Annex C" (cite.py --check: OK). The data-name İNTEGER is not
      *> folded to INTEGER, so FUNCTION ALL INTRINSIC, which identifies
      *> every function name, does not reach it: 8.3.2.1 5) and 12.3.8.3
      *> SR13 (cite.py --check: OK) forbid the intrinsic function names
      *> only. It fails when the declaration screen folds U+0130.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4420Y.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION ALL INTRINSIC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 İNTEGER PIC 9 VALUE 7.
       PROCEDURE DIVISION.
           DISPLAY İNTEGER.
           STOP RUN.
