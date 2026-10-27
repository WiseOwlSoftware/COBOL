*>--- conformance-case
*> id: pb3951_length_of_numeric_function_result_not_copied
*> rules: [RV-15.50.4-3, RV-15.14.4-1, DOC-A.1-92]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3951 (DOC-A.1-92): where the size LENGTH and BYTE-LENGTH report for a numeric function's returned value DIFFERS from
      *> GnuCOBOL 3.2.0's, on purpose. ISO leaves the temporary's representation to the implementor (§15.4.1), so CLAUDE.md rule 1's
      *> precedence follows GnuCOBOL only where its answer is a DESCRIPTION; each row here is a figure its temporary has that the value
      *> does not (the same rows pb3514_function_result_device_form_not_copied pins for DISPLAY). The expected values are derived from
      *> the determination, not copied from a run of cobc.
      *> 1. A BINARY64 RESULT IS A BINARY64 ITEM (kb/Work PB3505 declined GnuCOBOL's float image): SQRT, PI and ANNUITY return binary64
      *>    under native arithmetic (§15.4.1), so the temporary is the 8 bytes of a binary64 item. GnuCOBOL gives it a DISPLAY temporary
      *>    of 96 digits for SQRT and PI and 38 for ANNUITY.
      *> 2. NO DIGIT IS INVENTED. GnuCOBOL's DISPLAY temporary takes its digit count from GMP's size ESTIMATE, which exceeds the true count
      *>    by one for -9223372036854775808 (20 there). The DISPLAY item here is exactly the value's 19 digits, and the value past
      *>    2**63 in magnitude, which fits no 8-byte binary item, is that DISPLAY item.
      *> 3. A CALL THE BINDER FOLDS INTO A LITERAL IS MEASURED AS THAT LITERAL'S FIELD. FUNCTION LENGTH(W-X) is 3 and the standard admits
      *>    it as a function-identifier argument (§8.4.3.2.1: "a data item of any class or category"); GnuCOBOL refuses the folded
      *>    literal, and here its field has one digit position, so the length of that field is 1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3951B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-X PIC X(3) VALUE "abc".
       PROCEDURE DIVISION.
           DISPLAY "SQRT=" FUNCTION LENGTH(FUNCTION SQRT(4))
           DISPLAY "PI=" FUNCTION LENGTH(FUNCTION PI)
           DISPLAY "ANNUITY=" FUNCTION LENGTH(FUNCTION ANNUITY(0.05 3))
           DISPLAY "BYTE-SQRT=" FUNCTION BYTE-LENGTH(FUNCTION SQRT(4))
           DISPLAY "NEG-2-63=" FUNCTION LENGTH(FUNCTION NUMVAL("-9223372036854775808"))
           DISPLAY "NEG-2-63M=" FUNCTION LENGTH(FUNCTION NUMVAL("-9223372036854775809"))
           DISPLAY "FOLDED=" FUNCTION LENGTH(FUNCTION LENGTH(W-X))
           DISPLAY "BYTE-FOLDED=" FUNCTION BYTE-LENGTH(FUNCTION BYTE-LENGTH(W-X))
           STOP RUN.
