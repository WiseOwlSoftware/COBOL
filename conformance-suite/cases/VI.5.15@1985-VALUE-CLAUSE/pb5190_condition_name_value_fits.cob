      *>--- conformance-case
      *> id: pb5190_condition_name_value_fits
      *> rules: [1985-SR-VI.5.15.3-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5190 - the legal side of §1985:VI.5.15.3 SR3 (cite.py --check
      *> OK): "Nonnumeric literals in a VALUE clause of an item must not exceed
      *> the size indicated by the PICTURE clause." A condition-name takes its
      *> conditional variable's PICTURE (§1985:VI.5.15.5 GR1: "The
      *> characteristics of a condition-name are implicitly those of its
      *> conditional variable."), here X(3). A literal of exactly 3 characters
      *> and one of 2 (aligned as for an alphanumeric item, left with space
      *> fill, §1985:VI.5.15.4 GR1b) both fit.
      *> DERIVATION: SET XC TO TRUE stores "ABC" in XV, so XC is true and XD
      *> ("AB " after the space fill) is false; SET XD TO TRUE stores "AB " and
      *> reverses both. The oversize literal is refused at 85, see
      *> pb5190-condition-name-value-oversize-1985.
      *> Fails if: either condition-name is refused, or SET stores other than
      *> the literal.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5190FIT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  XV PIC X(3) VALUE SPACES.
           88  XC VALUE "ABC".
           88  XD VALUE "AB".
       PROCEDURE DIVISION.
       P1.
           SET XC TO TRUE.
           IF XC DISPLAY "XC TRUE [" XV "]" ELSE DISPLAY "XC FALSE".
           IF XD DISPLAY "XD TRUE" ELSE DISPLAY "XD FALSE".
           SET XD TO TRUE.
           IF XC DISPLAY "XC TRUE" ELSE DISPLAY "XC FALSE".
           IF XD DISPLAY "XD TRUE [" XV "]" ELSE DISPLAY "XD FALSE".
           STOP RUN.
