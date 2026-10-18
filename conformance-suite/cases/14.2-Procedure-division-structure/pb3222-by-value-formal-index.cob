*>--- conformance-case
*> id: pb3222-by-value-formal-index
*> rules: [SR-14.2.2-2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.2.2 SR2 - a data-name in a BY VALUE phrase of the procedure division header shall
      *> be of class numeric, message-tag, object, or pointer. A USAGE INDEX data item is of class
      *> index (8.5.2.1 Table 2), though its storage model is numeric, so it is refused
      *> (COBOLNET1553) exactly as the argument side refuses an index operand BY VALUE.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3222-BVFI.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LX USAGE INDEX.
       PROCEDURE DIVISION USING BY VALUE LX.
       MAIN.
           STOP RUN.
