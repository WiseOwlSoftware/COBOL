      *>--- conformance-case
      *> id: pb5796_range_figurative_class_accepted
      *> rules: [1985-SR-VI.6.13.3-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.13.3 SR4 - "Two operands connected by a THROUGH
      *> phrase must be of the same class." (cite.py --check OK.)
      *> The legal twins of the refused ranges: ZERO also stands for the
      *> numeric value 0 (§1985:IV.4.2.2.2.3 (1)), so ZERO THRU
      *> 9 is numeric to numeric; SPACE THRU "Z" and SPACE THRU
      *> HIGH-VALUE are alphanumeric to alphanumeric. A screen that
      *> refused every figurative would fail each of the three.
      *> Expected: 7 is in ZERO THRU 9; "M" is in SPACE THRU "Z"; "M"
      *> is in SPACE THRU HIGH-VALUE.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5796A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N PIC 9(3) VALUE 7.
       01  C PIC X VALUE "M".
       PROCEDURE DIVISION.
       P1.
           EVALUATE N
               WHEN ZERO THRU 9
                   DISPLAY "NUM IN"
               WHEN OTHER
                   DISPLAY "NUM OUT"
           END-EVALUATE.
           EVALUATE C
               WHEN SPACE THRU "Z"
                   DISPLAY "ALPHA IN"
               WHEN OTHER
                   DISPLAY "ALPHA OUT"
           END-EVALUATE.
           EVALUATE C
               WHEN SPACE THRU HIGH-VALUE
                   DISPLAY "HV IN"
               WHEN OTHER
                   DISPLAY "HV OUT"
           END-EVALUATE.
           STOP RUN.
