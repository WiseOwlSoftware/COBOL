*>--- conformance-case
*> id: pb1136-invoke-null-identifier-2
*> rules: [SR-14.9.23.3-1, SR-14.9.23.3-7]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.23.3 SR7: "Identifier-2 may be specified only when identifier-1 is a universal object
      *> reference." The predefined NULL is an object reference (SR1) but §8.4.3.7.3 SR2 says it "is not a
      *> universal object reference", so a method name held in a data item is refused through it, while
      *> INVOKE NULL with literal-1 is legal (pb1136_invoke_null_receiver). kb/Work PB1136.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1136N7.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 MN PIC X(4) VALUE "PING".
       PROCEDURE DIVISION.
       MAIN.
           INVOKE NULL MN.
           STOP RUN.
       END PROGRAM PB1136N7.
