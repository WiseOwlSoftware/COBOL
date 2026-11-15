      *>--- conformance-case
      *> id: pb5342-send-advancing-all-zero
      *> rules: [SR-8.3.3.6.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ALL ZERO AS SEND ... ADVANCING integer-1 (kb/Work PB5342).
      *> §2023:8.3.3.6.3 SR1 a) (2002: 8.3.1.2.5.2 SR1 a): "If the literal
      *> is restricted to a numeric literal, the only figurative constant
      *> permitted is ZERO (ZEROS, ZEROES) without the ALL phrase."
      *> Integer-1 of ISO/IEC 1989:2002 A.2.6.1 is such a literal, and
      *> A.2.6.2 SR6 lets it be zero. Edge DERIVED at 2002 (VCR Table 7
      *> row 7.28): at 85 the word ALL is redundant, and the 85 twin is
      *> XIV.3.6@1985-SEND-STATEMENT/pb5289_send_integer_operands_85.
      *> Refused at 2002, COBOL0902; from 2014 the whole facility is.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5342A.
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
           SEND OUT-CD FROM MSG WITH EMI
               BEFORE ADVANCING ALL ZERO LINES.
           STOP RUN.
