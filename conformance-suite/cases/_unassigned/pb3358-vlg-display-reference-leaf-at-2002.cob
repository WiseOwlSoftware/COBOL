*>--- conformance-case
*> id: pb3358-vlg-display-reference-leaf-at-2002
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3358 - the strongly-typed variable-length group that holds an object-reference leaf (the
      *> pb3358_vlg_display_reference_leaf program) is a COBOL-2014 program: the DYNAMIC LENGTH clause that makes the
      *> group variable-length (ISO 8.5.1.12.1, 13.18.19) is a 2014 addition, so editions 85 and 2002 reject it with
      *> the edition-band diagnostic COBOLNET0900 BEFORE the group's display format (14.9.11.4 GR7) is ever in
      *> question.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3358NEG.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB3358F.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T1 IS TYPEDEF STRONG.
          05 A PIC X DYNAMIC LENGTH LIMIT IS 10.
          05 R USAGE OBJECT REFERENCE PB3358F.
       01 G1 TYPE T1.
       PROCEDURE DIVISION.
           DISPLAY "[" G1 "]"
           STOP RUN.
       END PROGRAM PB3358NEG.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB3358F.
       OBJECT.
       END OBJECT.
       END CLASS PB3358F.
