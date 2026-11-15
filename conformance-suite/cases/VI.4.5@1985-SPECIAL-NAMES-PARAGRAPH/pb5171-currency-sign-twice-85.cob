      *>--- conformance-case
      *> id: pb5171-currency-sign-twice-85
      *> rules: [1985-FMT-VI.4.5.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5171. §1985:VI.4.5.2 prints "[CURRENCY SIGN IS literal-6]"
      *> with no ellipsis, and §1985:IV.2.1.5: the ellipsis "represents the
      *> position at which the user elects repetition" (cite.py: OK). So the
      *> COBOL-85 paragraph takes one CURRENCY SIGN clause: the second, at
      *> line 19, is refused COBOL2423. From 2002 the clause carries an
      *> ellipsis (pb5171-currency-sign-twice accepts it there).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5171C2.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CURRENCY SIGN IS "L"
           CURRENCY SIGN IS "M".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A-ITEM PIC L99.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 12 TO A-ITEM.
           DISPLAY A-ITEM.
           STOP RUN.
