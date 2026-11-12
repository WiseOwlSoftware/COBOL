      *>--- conformance-case
      *> id: pb5190-condition-name-value-oversize-1985
      *> rules: [1985-SR-VI.5.15.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5190 - §1985:VI.5.15.3 SR3 (cite.py --check OK): "Nonnumeric
      *> literals in a VALUE clause of an item must not exceed the size
      *> indicated by the PICTURE clause." A condition-name's PICTURE is its
      *> conditional variable's (§1985:VI.5.15.5 GR1), here X, one position,
      *> and the literal "cd" has two: refused at COBOL-85 (COBOL1740), the
      *> numeric sentence of the same rule having refused 88 VALUE 123 under
      *> PIC 99 already. From 2002 the size sentence names "an elementary
      *> item" and "an explicit PICTURE clause" (§2002:13.16.61.2 SR4), so
      *> the same program is a legal, permanently false condition there
      *> (pb598_condition_name_value_size, accepted from 2002).
      *> Fails if: the program compiles at 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5190OVR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  XV PIC X VALUE "c".
           88  XC VALUE "cd".
       PROCEDURE DIVISION.
       P1.
           SET XC TO TRUE.
           IF XC DISPLAY "XC TRUE" ELSE DISPLAY "XC FALSE".
           STOP RUN.
