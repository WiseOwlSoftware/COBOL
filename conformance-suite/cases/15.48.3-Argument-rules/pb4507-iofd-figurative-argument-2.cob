*>--- conformance-case
*> id: pb4507-iofd-figurative-argument-2
*> rules: [AR-15.48.3-3]
*> source-format: free
*> portable: unverified
*>---
      *> PB4507 - ISO §15.48.3 r3: argument-2 "shall be a data item of the same type as argument-1". A figurative constant
      *> is of class alphanumeric, so the class screen admits it; it is not a data item, so the data-item rule alone refuses it
      *> (the sibling of pb58-iofd-literal). Moving the data-item question to the function-identifier-aware predicate must
      *> keep this refusal.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGIOFDFIGURATIVE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R     PIC 9(7).
       PROCEDURE DIVISION.
           COMPUTE R = FUNCTION INTEGER-OF-FORMATTED-DATE("YYYYMMDD" SPACE).
           STOP RUN.
