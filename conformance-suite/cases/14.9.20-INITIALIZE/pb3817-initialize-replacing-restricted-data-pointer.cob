*>--- conformance-case
*> id: pb3817-initialize-replacing-restricted-data-pointer
*> rules: [SR-14.9.20.3-4, GR-14.9.20.4-4, GR-13.18.60.4-23, SR-14.9.39.3-19]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3817 - the data-pointer sibling of pb3817-initialize-replacing-restricted-program-pointer.
      *> 14.9.20.4 GR4 makes the REPLACING store of a data-pointer "SET receiving-operand TO sending-operand";
      *> 14.9.39.3 SR19 holds that SET ("If identifier-5 references a restricted data-pointer, identifier-6 shall be
      *> the predefined address NULL or shall reference a data-pointer restricted to the same type") and 13.18.60.4
      *> GR23 ("A restricted data-pointer shall contain only the predefined address NULL or the address of a data
      *> item of the specified type"). PR is restricted to TT; PU is an unrestricted data-pointer, so INITIALIZE
      *> would store an address of unknown type into it.
      *> cite.py: OK  14.9.39.3 19)  /  OK  13.18.60.4 23)
      *> Refused COBOL0869 (pointer-operand-shape, the code the Format-7 route reports SR19 under) at every edition
      *> that has TYPEDEF; below 2002 the whole family draws the introduction gate instead.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3817N3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TT TYPEDEF STRONG.
          05 TT-A PIC X(4).
       01 PTR-T TYPEDEF USAGE POINTER TO TT.
       01 PR TYPE PTR-T.
       01 PU USAGE POINTER.
       PROCEDURE DIVISION.
       MAIN-P.
           INITIALIZE PR REPLACING DATA-POINTER DATA BY PU
           STOP RUN.
       END PROGRAM PB3817N3.
