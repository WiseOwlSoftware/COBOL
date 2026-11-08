      *>--- conformance-case
      *> id: pb4641_float_edited_receiver_quotient
      *> rules: [DOC-A.1-123, GR-8.8.1.2-1]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4641 review R1-1: a floating-point edited receiver
      *> may carry a 1 to 36 digit significand (13.18.40.3 SR15; cite.py
      *> OK), so the final-transfer SDIDI quotient must not be jammed
      *> to an odd 34th digit for it: the receiver would read the jam
      *> as exact. 2.0E0 / 9.0E0 = .2222... (non-terminating): a
      *> 34-digit significand holds 34 twos and a 33-digit one 33 twos.
      *> EXPECTED, DERIVED (Python Decimal): 2/9 = .222222...
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4641FLOATEDQUOT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 F1  PIC +.9(33)E+99.
       01 F2  PIC +.9(34)E+99.
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE F1 = 2.0E0 / 9.0E0
           DISPLAY "F1 " F1
           COMPUTE F2 = 2.0E0 / 9.0E0
           DISPLAY "F2 " F2
           STOP RUN.
