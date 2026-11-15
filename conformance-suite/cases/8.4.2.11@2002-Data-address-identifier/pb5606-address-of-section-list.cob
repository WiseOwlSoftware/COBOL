      *>--- conformance-case
      *> id: pb5606-address-of-section-list
      *> rules: [2002-SR-8.4.2.11.2-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5606. §2002:8.4.2.11.2 SR1: "Identifier-1 shall reference a
      *> data item defined in the file section, working-storage section,
      *> local-storage section, or linkage section." The communication
      *> section is not one of the four, so ADDRESS OF a working-storage item
      *> is legal in a program that also has a CD, and ADDRESS OF an item of
      *> a CD's record is refused (pb5606-address-of-communication-item, the
      *> same program with the operand moved into the CD record). Why it can
      *> fail: the working-storage operand is refused for the CD beside it,
      *> or the address is not a live pointer (NULL is displayed).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5606A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  P   USAGE POINTER.
       01  W1  PIC X(3).
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 A1 PIC X(3).
       PROCEDURE DIVISION.
           SET P TO ADDRESS OF W1.
           IF P = NULL DISPLAY "NULL" ELSE DISPLAY "SET".
           STOP RUN.
