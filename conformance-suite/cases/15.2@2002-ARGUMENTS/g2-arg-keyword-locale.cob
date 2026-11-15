      *>--- conformance-case
      *> id: g2-arg-keyword-locale
      *> rules: [2002-AR-15.2-6, 2002-AR-15.2-7]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.2 AR6 - "A keyword shall be specified in accordance
      *> with the function definition": the 2002 keyword arguments are
      *> ANYCASE and LOCALE of NUMVAL-C and TEST-NUMVAL-C (Table 22
      *> Key2/Key3; §2002:15.55.1, §2002:15.76.1).
      *> §2002:15.2 AR7 - "A locale-name defined in the SPECIAL-NAMES
      *> paragraph shall be specified": LOC1 of the LOCALE clause given
      *> to LOCALE-COMPARE, NUMVAL-C and TEST-NUMVAL-C.
      *> Expected lines, each from the 2002 function definitions; the
      *> locale legs use only content every LC_MONETARY and every
      *> cultural ordering treat alike:
      *>   KW1 1234.50  ANYCASE makes "USD" match "usd" (§2002:15.55.2
      *>                AR3); the value is 1234.50 (15.55.3 RV1).
      *>   KW2 00 01    TEST-NUMVAL-C: 0 when argument-1 conforms; with
      *>                no ANYCASE the case-sensitive "USD" does not
      *>                match, so the first character is in error
      *>                (§2002:15.76.3 RV1).
      *>   KW3 1234.00 1234.00  NUMVAL-C of a digits-only string with
      *>                LOCALE LOC1 and with a bare LOCALE.
      *>   KW4 00       TEST-NUMVAL-C of the same with LOCALE LOC1.
      *>   LC1 = =      LOCALE-COMPARE with LOC1: trailing spaces are
      *>                truncated (§2002:15.40.3 RV2), the operands are
      *>                then the same, so '=' (15.40.3 RV5).
      *> A refused keyword or locale-name, or a wrong value, fails it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2ARG04.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           LOCALE LOC1 IS "C".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X-ABC   PIC X(5) VALUE "abc".
       01 E-A     PIC 9(4).99.
       01 E-B     PIC 9(4).99.
       01 N2A     PIC 99.
       01 N2B     PIC 99.
       PROCEDURE DIVISION.
       P0.
           MOVE FUNCTION NUMVAL-C("usd1,234.50" "USD" ANYCASE) TO E-A
           DISPLAY "KW1 " E-A
           MOVE FUNCTION TEST-NUMVAL-C("usd12.5" "USD" ANYCASE) TO N2A
           MOVE FUNCTION TEST-NUMVAL-C("usd12.5" "USD") TO N2B
           DISPLAY "KW2 " N2A " " N2B
           MOVE FUNCTION NUMVAL-C("1234" LOCALE LOC1) TO E-A
           MOVE FUNCTION NUMVAL-C("1234" LOCALE) TO E-B
           DISPLAY "KW3 " E-A " " E-B
           MOVE FUNCTION TEST-NUMVAL-C("1234" LOCALE LOC1) TO N2A
           DISPLAY "KW4 " N2A
           DISPLAY "LC1 " FUNCTION LOCALE-COMPARE("abc" "abc  " LOC1)
                   " " FUNCTION LOCALE-COMPARE(X-ABC "abc" LOC1)
           STOP RUN.
