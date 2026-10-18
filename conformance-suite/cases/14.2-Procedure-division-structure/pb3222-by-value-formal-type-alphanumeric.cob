*>--- conformance-case
*> id: pb3222-by-value-formal-type-alphanumeric
*> rules: [SR-14.2.2-2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.2.2 SR2 - a data-name in a BY VALUE phrase shall be defined as a data item of
      *> class numeric, message-tag, object, or pointer. A formal described by a TYPE clause
      *> whose TYPEDEF is alphanumeric is of class alphanumeric (8.5.2.1 Table 2) and is refused
      *> (COBOLNET1553): the class is the composed description's, not an absent PICTURE.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3222-BVTA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 XT TYPEDEF PIC X(4).
       LINKAGE SECTION.
       01 AX TYPE XT.
       PROCEDURE DIVISION USING BY VALUE AX.
       MAIN.
           STOP RUN.
