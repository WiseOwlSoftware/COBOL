      *>--- conformance-case
      *> id: 2002-SR-A_1_2_2-8
      *> rules: [2002-SR-A.1.2.2-8]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:A.1.2.2 SR8 - "If none of the optional clauses of the
      *> CD entry is specified, a level 01 data description entry
      *> shall follow the CD entry." CD COUT (line 5 of the program)
      *> names none of the output clauses and the PROCEDURE DIVISION
      *> follows it, so no 01 entry follows. A compiler that accepts it
      *> fails. The satisfied branch (an 01 follows) is the accepted
      *> case 2002-SR-A_1_2_2-9.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CDS8.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD COUT FOR OUTPUT.
       PROCEDURE DIVISION.
       P1.
           STOP RUN.
