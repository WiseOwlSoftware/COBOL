      *>--- conformance-case
      *> id: pb5289-send-advancing-index-item
      *> rules: [1985-SR-XIV.3.6.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      * X3.23-1985 XIV 3.6.3 SR3: "Identifier-3 must reference an
      * integer data item." An index data item is no integer data
      * item: it "can be referenced explicitly only in a SEARCH or SET
      * statement, a relation condition, the USING phrase" (VI 5.14.3
      * SR5). A-IDX was accepted because its storage profile is
      * numeric at scale zero (kb/Work PB5289); it is refused with
      * COBOL6171.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5289X.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  MSG PIC X(10) VALUE "HELLO".
       01  A-IDX USAGE INDEX.
       COMMUNICATION SECTION.
       CD  OUT-CD FOR OUTPUT
           DESTINATION COUNT IS OUT-COUNT
           STATUS KEY IS OUT-ST
           SYMBOLIC DESTINATION IS OUT-DEST.
       PROCEDURE DIVISION.
       P-PARA.
           SEND OUT-CD FROM MSG WITH EMI AFTER ADVANCING A-IDX LINES.
           STOP RUN.
