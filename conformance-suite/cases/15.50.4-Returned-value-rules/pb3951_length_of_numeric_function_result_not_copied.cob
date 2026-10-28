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
      *>    it as a function-identifier argument (§8.4.3.2.1 makes it a data item, §15.50.3 r1 admits any); GnuCOBOL refuses the folded
      *>    literal, and here its field has one digit position, so the length of that field is 1.
      *> 4. A COPY OF A USAGE NATIONAL ARGUMENT IS ITS ITEM'S POSITIONS AND ITS ITEM'S BYTES (review R1-1 of the PB3951 branch). MIN and MAX
      *>    return the CONTENT of an argument, so the temporary is a copy of that national item: FUNCTION LENGTH counts national character
      *>    positions (§15.50.4 r2: 5 for PIC S9(5) USAGE NATIONAL, 6 once SIGN IS LEADING SEPARATE adds a character position, §13.18.52.4 GR6) and
      *>    FUNCTION BYTE-LENGTH the bytes of those positions (§15.14.4 r1: two per position, 10 and 12), exactly as for the item itself. GnuCOBOL
      *>    3.2.0 calls its national support unfinished and answers 10 to both, for the item and for its copy.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3951B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-X PIC X(3) VALUE "abc".
       01 NN PIC S9(5) USAGE NATIONAL VALUE -12.
       01 NU PIC 9(5) USAGE NATIONAL VALUE 12.
       01 NS PIC S9(5) USAGE NATIONAL SIGN IS LEADING SEPARATE VALUE -12.
       PROCEDURE DIVISION.
           DISPLAY "SQRT=" FUNCTION LENGTH(FUNCTION SQRT(4))
           DISPLAY "PI=" FUNCTION LENGTH(FUNCTION PI)
           DISPLAY "ANNUITY=" FUNCTION LENGTH(FUNCTION ANNUITY(0.05 3))
           DISPLAY "BYTE-SQRT=" FUNCTION BYTE-LENGTH(FUNCTION SQRT(4))
           DISPLAY "NEG-2-63=" FUNCTION LENGTH(FUNCTION NUMVAL("-9223372036854775808"))
           DISPLAY "NEG-2-63M=" FUNCTION LENGTH(FUNCTION NUMVAL("-9223372036854775809"))
           DISPLAY "FOLDED=" FUNCTION LENGTH(FUNCTION LENGTH(W-X))
           DISPLAY "BYTE-FOLDED=" FUNCTION BYTE-LENGTH(FUNCTION BYTE-LENGTH(W-X))
           DISPLAY "NAT-MIN=" FUNCTION LENGTH(FUNCTION MIN(NN NU))
           DISPLAY "BYTE-NAT-MIN=" FUNCTION BYTE-LENGTH(FUNCTION MIN(NN NU))
           DISPLAY "NAT-MAX=" FUNCTION LENGTH(FUNCTION MAX(NN NU))
           DISPLAY "BYTE-NAT-MAX=" FUNCTION BYTE-LENGTH(FUNCTION MAX(NN NU))
           DISPLAY "NAT-SEP=" FUNCTION LENGTH(FUNCTION MIN(NS NU))
           DISPLAY "BYTE-NAT-SEP=" FUNCTION BYTE-LENGTH(FUNCTION MIN(NS NU))
           STOP RUN.
