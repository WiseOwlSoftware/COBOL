*>--- conformance-case
*> id: pb5640-invoke-bit-refmod-data-length-2023
*> rules: [SR-14.9.23.3-12]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB5640. §2023:14.9.23.3 SR12: a bit identifier-3 or identifier-4 "shall be described such that it is aligned on a byte boundary and that
      *> subscripting and the leftmost position in a reference modification of that identifier consist of only fixed-point numeric literals or
      *> arithmetic expressions whose result is a positive integer" — the LENGTH is not constrained at 2023, so B8(1:L) with the data-name
      *> L is accepted (2002 and 2014 require the length to be a literal too: pb5640-invoke-bit-refmod-data-length). Why it can fail: the
      *> 2002/2014 whole-reference-modification rule is applied at 2023 and the program is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5640E.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE.
       01 L PIC 9(2) VALUE 8.
       01 BG.
          05 B8 PIC 1(16) USAGE BIT.
       PROCEDURE DIVISION.
       MAIN.
           IF O NOT = NULL
               INVOKE O "TAKEB" USING B8(1:L)
           END-IF.
           DISPLAY "COMPILED".
           STOP RUN.
       END PROGRAM PB5640E.
