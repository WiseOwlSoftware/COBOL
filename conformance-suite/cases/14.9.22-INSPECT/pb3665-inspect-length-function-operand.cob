*>--- conformance-case
*> id: pb3665-inspect-length-function-operand
*> rules: [SR-14.9.22.3-2]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3665 - the folded FUNCTION LENGTH in an INSPECT REPLACING BY position. It is a function-identifier
*> (8.4.3.2.1) of an integer function, to which 15.2 5) gives no usage, so ISO 14.9.22.3 SR2 refuses it as it
*> refuses FUNCTION INTEGER (pb3665-inspect-integer-function-operand); the fold must not change the answer.
*> Unchanged at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3665IL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 G-A PIC X(3).
          05 G-B PIC X(4).
       01 S PIC X(8) VALUE "ab7cd7ef".
       PROCEDURE DIVISION.
       MAIN.
           INSPECT S REPLACING ALL "7" BY FUNCTION LENGTH(G)
           STOP RUN.
