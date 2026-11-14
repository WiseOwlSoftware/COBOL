      *>--- conformance-case
      *> id: pb5171-currency-sign-twice
      *> rules: [FMT-12.3.7.2]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5171. From 2002 the CURRENCY SIGN clause carries an
      *> ellipsis (§2023:12.3.7.2 "[ CURRENCY SIGN IS literal-7 [ WITH PICTURE
      *> SYMBOL literal-8 ] ] ..."), so two clauses with two currency symbols
      *> are legal and each serves its PICTURE strings (§2023:12.3.7.4 GR13:
      *> "it is used to determine which symbol shall be used in a
      *> character-string" of a PICTURE clause; cite.py: OK). A is L12, B is
      *> M12. COBOL-85 takes one clause
      *> (pb5171-currency-sign-twice-85).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5171CT.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CURRENCY SIGN IS "L"
           CURRENCY SIGN IS "M".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A-ITEM PIC L99.
       01  B-ITEM PIC M99.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 12 TO A-ITEM B-ITEM.
           DISPLAY A-ITEM " " B-ITEM.
           STOP RUN.
