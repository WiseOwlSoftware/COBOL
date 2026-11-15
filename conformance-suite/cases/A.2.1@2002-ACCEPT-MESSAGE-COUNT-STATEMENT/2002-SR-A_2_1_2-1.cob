      *>--- conformance-case
      *> id: 2002-SR-A_2_1_2-1
      *> rules: [2002-SR-A.2.1.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:A.2.1.2 SR1 - "Cd-name-1 shall reference an input CD."
      *> ACCEPT ... MESSAGE COUNT names an output CD (line 24) and is
      *> refused; the same statement on the input CD (line 23) is
      *> accepted, so the refusal lands on the output CD only.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2AMC04.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CIN FOR INPUT
           SYMBOLIC QUEUE IS IN-Q
           STATUS KEY IS IN-STAT
           MESSAGE COUNT IS IN-CNT.
       CD  COUT FOR OUTPUT
           STATUS KEY IS O-STAT.
       PROCEDURE DIVISION.
       P1.
           ACCEPT CIN MESSAGE COUNT.
           ACCEPT COUT MESSAGE COUNT.
           STOP RUN.
