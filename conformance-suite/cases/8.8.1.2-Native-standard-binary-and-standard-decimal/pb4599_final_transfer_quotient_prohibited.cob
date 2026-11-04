      *>--- conformance-case
      *> id: pb4599_final_transfer_quotient_prohibited
      *> rules: [DOC-A.1-123, GR-8.8.1.2-1]
      *> accept: [2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4599 (docs/CONFORMANCE.md DOC-A.1-123): the
      *> final-transfer quotient of exact operands is the exact
      *> quotient. A = 10**15 - 1, so A * A * A / (A * A) is the
      *> integer A (Python integers): with ROUNDED MODE IS PROHIBITED
      *> the value is exactly representable in the receiver, so no
      *> size error is raised and A is stored (14.7.4.3 rule 7:
      *> PROHIBITED raises only for a value not exactly representable;
      *> cite.py: OK 14.7.4.3). The 34-digit image of the 45-digit
      *> dividend made the quotient 999999999999998.99..., inexact.
      *> ROUNDED MODE IS is a COBOL-2014 phrase (ISO 14.7.4; the
      *> refusal below it is pb4599_final_transfer_quotient_prohibited_2002).
      *>   PROH  A*A*A / (A*A) = 999999999999999, no size error.
      *>   PROHI A*A*A / (A*A*2) = A / 2 = 499999999999999.5 is not
      *>         representable in PIC S9(18): the size error
      *>         EC-SIZE-TRUNCATION, the receiver unchanged.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4599PROHIBITED.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A    PIC 9(18) VALUE 999999999999999.
       01 R    PIC S9(18) VALUE 7.
       01 E1   PIC -(18)9.
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE R ROUNDED MODE IS PROHIBITED = A * A * A / (A * A)
               ON SIZE ERROR DISPLAY "PROH  SIZE ERROR"
               NOT ON SIZE ERROR DISPLAY "PROH  NO SIZE ERROR"
           END-COMPUTE
           MOVE R TO E1
           DISPLAY "PROH  " E1
           COMPUTE R ROUNDED MODE IS PROHIBITED
                   = A * A * A / (A * A * 2)
               ON SIZE ERROR DISPLAY "PROHI SIZE ERROR"
               NOT ON SIZE ERROR DISPLAY "PROHI NO SIZE ERROR"
           END-COMPUTE
           MOVE R TO E1
           DISPLAY "PROHI " E1
           STOP RUN.
