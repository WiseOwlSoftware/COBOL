      *>--- conformance-case
      *> id: pb4599_final_transfer_quotient_intermediate_prohibited
      *> rules: [DOC-A.1-123, GR-8.8.1.2-1]
      *> accept: [2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4599 review finding R1-5 (docs/CONFORMANCE.md
      *> DOC-A.1-82 and DOC-A.1-123): a native statement that carries
      *> a floating-point literal forms its quotient on the SDIDI
      *> lane, whose intermediate is screened by INTERMEDIATE ROUNDING
      *> IS PROHIBITED: 11.9.11.2 rule 3 d) "If the PROHIBITED phrase
      *> is specified and an intermediate value cannot be represented
      *> exactly in SDIDI form, the EC-SIZE-TRUNCATION exception
      *> condition is set to exist" (cite.py: OK 11.9.11.2). The
      *> quotient of an exact wide dividend over such a literal
      *> divides the EXACT dividend (PB4599) and must stay screened.
      *>
      *> EXPECTED VALUES, DERIVED (Python integers), with
      *> A = 10**15 - 1 and A*A*A = 999999999999997000000000000002
      *> 999999999999999 (45 digits):
      *>   IPX1  A*A*A / (A*A*1.0E0) = A exactly, so it is
      *>         representable in an SDIDI: no size error, A stored.
      *>   IPX2  A*A*A / 7.0E30: A**3 is not divisible by 7 (A mod 7
      *>         = 5, A**3 mod 7 = 6), so the quotient is a
      *>         non-terminating decimal, not exact in 34 digits: the
      *>         size error condition exists. The plain quotient
      *>         A*A*A / (A*A*7) (no literal, no SDIDI lane) is not
      *>         screened and stores the truncated 142857142857142.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4599INTERPROH.
       OPTIONS.
           INTERMEDIATE ROUNDING IS PROHIBITED.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A    PIC 9(18) VALUE 999999999999999.
       01 R    PIC S9(18) VALUE 7.
       01 E1   PIC -(18)9.
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE R = A * A * A / (A * A * 1.0E0)
               ON SIZE ERROR DISPLAY "IPX1  SIZE ERROR"
               NOT ON SIZE ERROR DISPLAY "IPX1  NO SIZE ERROR"
           END-COMPUTE
           MOVE R TO E1
           DISPLAY "IPX1  " E1
           COMPUTE R = A * A * A / 7.0E30
               ON SIZE ERROR DISPLAY "IPX2  SIZE ERROR"
               NOT ON SIZE ERROR DISPLAY "IPX2  NO SIZE ERROR"
           END-COMPUTE
           COMPUTE R = A * A * A / (A * A * 7)
               ON SIZE ERROR DISPLAY "PLAIN SIZE ERROR"
               NOT ON SIZE ERROR DISPLAY "PLAIN NO SIZE ERROR"
           END-COMPUTE
           MOVE R TO E1
           DISPLAY "PLAIN " E1
           STOP RUN.
