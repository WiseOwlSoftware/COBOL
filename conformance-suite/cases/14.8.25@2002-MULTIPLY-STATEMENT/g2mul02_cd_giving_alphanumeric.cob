      *>--- conformance-case
      *> id: g2mul02_cd_giving_alphanumeric
      *> rules: [2002-SR-14.8.25.2-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.25.2 SR2 - "Identifier-3 shall reference a data
      *>   item of category numeric or numeric-edited." SQ, an input
      *>   CD's SYMBOLIC QUEUE, is an elementary alphanumeric item of
      *>   12 characters (§2002:A.1.2.3 GR2 a), so GIVING SQ is
      *>   refused at the MULTIPLY line. Fails if the CD field's
      *>   implicit category is not screened.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2MUL02.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD CDIN FOR INPUT
           SYMBOLIC QUEUE IS SQ.
       01 INREC PIC X(87).
       PROCEDURE DIVISION.
       MAIN.
           MULTIPLY 2 BY 3 GIVING SQ.
           STOP RUN.
