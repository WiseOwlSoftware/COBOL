      *>--- conformance-case
      *> id: pb5384-send-advancing-refmod
      *> rules: [1985-SR-XIV.3.6.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      * X3.23-1985 XIV 3.6.3 SR3: "Identifier-3 must reference an
      * integer data item." N2 (2:1) is no integer data item:
      * reference modification makes "the categories numeric,
      * numeric edited, and alphanumeric edited ... class and
      * category alphanumeric" (IV 4.3.8.4.4 GR5). It was accepted
      * because the screen read the base item's PICTURE through the
      * reference modification (kb/Work PB5384); it is refused with
      * COBOL6171. ISO 2002 A.2.6.2 SR4 is the same rule.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5384B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  MSG PIC X(10) VALUE "HELLO".
       01  N2 PIC 99 VALUE 12.
       COMMUNICATION SECTION.
       CD  OUT-CD FOR OUTPUT
           DESTINATION COUNT IS OUT-COUNT
           STATUS KEY IS OUT-ST
           SYMBOLIC DESTINATION IS OUT-DEST.
       PROCEDURE DIVISION.
       P-PARA.
           SEND OUT-CD FROM MSG WITH EMI
               BEFORE ADVANCING N2 (2:1) LINES.
           STOP RUN.
