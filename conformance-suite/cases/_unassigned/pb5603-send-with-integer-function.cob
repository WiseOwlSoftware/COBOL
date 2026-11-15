      *>--- conformance-case
      *> id: pb5603-send-with-integer-function
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5603 - ISO/IEC 1989:2002 A.2.6.2 SR3: "Identifier-2
      *> shall reference a one-character integer without an operational
      *> sign." WITH now takes every identifier format, so the rule is
      *> asked of the operand: an intrinsic function's temporary item has
      *> the description its function's definition gives (8.4.2.2.3
      *> GR1), and an integer function is given no size, so it is no
      *> one-character integer. The rule refuses it; a parse error would
      *> be the wrong verdict for a rule the program does break.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5603F.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 MSG PIC X(10) VALUE "HELLO".
       COMMUNICATION SECTION.
       CD OUTCD FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           DESTINATION TABLE OCCURS 1 TIMES
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SYMD.
       PROCEDURE DIVISION.
       MAIN-PARA.
           SEND OUTCD FROM MSG WITH FUNCTION MOD (5 3).
           STOP RUN.
