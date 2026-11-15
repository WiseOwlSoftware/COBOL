      *>--- conformance-case
      *> id: g2-goto-not-last-scope-terminated-call
      *> rules: [2002-SR-14.8.16.2-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.16.2 SR2 with §2002:14.4: "An imperative statement
      *>   ... is a conditional statement that is delimited by its
      *>   explicit scope terminator". CALL ... ON OVERFLOW ... END-CALL
      *>   is therefore imperative, the GO TO before it is not last in
      *>   its sequence, and the sentence is refused (contrast P0 of
      *>   g2-goto-last-before-conditional, where no END-CALL is written).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2GOTO03.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  X   PIC X(4) VALUE "MSG1".
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       PROCEDURE DIVISION.
       MAIN-P.
           GO TO P1 CALL "NOSUCHPGM" ON OVERFLOW DISPLAY "OVF" END-CALL.
       P1.
           DISPLAY "P1" STOP RUN.
