      *>--- conformance-case
      *> id: pb5289-send-advancing-symbolic-character
      *> rules: [1985-SR-XIV.3.6.3-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      * X3.23-1985 XIV 3.6.3 SR4: "The mnemonic-name is defined in
      * the SPECIAL-NAMES paragraph"; SR3: identifier-3 "must
      * reference an integer data item". BEL is a symbolic-character,
      * a figurative constant (IV 4.2.2.2.3), so it is neither, and
      * the SEND below is no SEND format. It was accepted because the
      * symbolic-character fell past both screens (kb/Work PB5289);
      * it is refused with COBOL6171.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5289S.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS BEL IS 8.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  MSG PIC X(10) VALUE "HELLO".
       COMMUNICATION SECTION.
       CD  OUT-CD FOR OUTPUT
           DESTINATION COUNT IS OUT-COUNT
           STATUS KEY IS OUT-ST
           SYMBOLIC DESTINATION IS OUT-DEST.
       PROCEDURE DIVISION.
       P-PARA.
           SEND OUT-CD FROM MSG WITH EMI AFTER ADVANCING BEL.
           STOP RUN.
