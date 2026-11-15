      *>--- conformance-case
      *> id: 2002-SR-A_2_2_2-3
      *> rules: [2002-SR-A.2.2.2-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:A.2.2.2 SR3 - OUTPUT phrase: cd-name-1 shall reference
      *> an output CD. The first DISABLE (right CD kind) is accepted;
      *> the second (wrong CD kind) is refused, so the refusal
      *> lands on the violating line only.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DIS09.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CIN FOR INPUT
           SYMBOLIC QUEUE IS IN-Q
           STATUS KEY IS IN-STAT.
       CD  CIO FOR I-O
           SYMBOLIC TERMINAL IS IO-TERM
           STATUS KEY IS IO-STAT.
       CD  COUT FOR OUTPUT
           STATUS KEY IS O-STAT.
       PROCEDURE DIVISION.
       P1.
           DISABLE OUTPUT COUT.
           DISABLE OUTPUT CIN.
           STOP RUN.
