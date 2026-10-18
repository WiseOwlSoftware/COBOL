*>--- conformance-case
*> id: pb3222-by-value-formal-type-numeric
*> rules: [SR-14.2.2-2]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.2.2 SR2 - a data-name in a BY VALUE phrase shall be defined as a data item of
      *> class numeric, message-tag, object, or pointer. The class of a formal described by a
      *> TYPE clause is the class of the type's description (8.5.2.1 Table 2), so a formal typed
      *> by a numeric TYPEDEF is admitted. The leg can fail if the class is read before the TYPE
      *> clause has composed its subject (the formal then has no PICTURE and was refused).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3222-BVTN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NT TYPEDEF PIC 9(4).
       LINKAGE SECTION.
       01 NX TYPE NT.
       PROCEDURE DIVISION USING BY VALUE NX.
       MAIN.
           STOP RUN.
