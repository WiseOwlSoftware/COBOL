      *>--- conformance-case
      *> id: pb4420-filler-spelled-with-dotted-capital-i-is-a-constant-name
      *> rules: [GR-8.1.3.2-4, FMT-13.10.2]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4420 - ISO 8.1.3.2 4) b): "An uppercase extended
      *> letter is treated as though it were folded to its corresponding
      *> lowercase extended letter, if any, in accordance with the case
      *> mapping described in Annex C" (cite.py --check: OK). FİLLER is
      *> not the reserved word FILLER (8.9) and is not folded to it, so
      *> it is a user-defined word that names the constant (13.10.2
      *> requires a constant-name-1, cite.py --check: OK); the constant-
      *> entry funnel (DataBinder.DeclareConstant) used to compare the
      *> name with FILLER through the fold, took the entry for an
      *> unnamed one and refused it (COBOL1547). It fails when that
      *> comparison folds U+0130.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4420F.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FİLLER CONSTANT AS 5.
       PROCEDURE DIVISION.
           DISPLAY FİLLER.
           STOP RUN.
