      *>--- conformance-case
      *> id: g2rw05_from_alphabetic_cd_item
      *> rules: [2002-SR-14.8.33.2-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.33.2 SR6 - identifier-1 "shall be valid as a
      *>   sending operand in a MOVE statement specifying record-name-1
      *>   as the receiving operand" (cite.py: OK). CA is an alphabetic
      *>   item of the record of an output CD; §2002:14.8.24.2 SR8
      *>   sends the move to table 17 (cite.py: OK), where an
      *>   alphabetic sender to a numeric receiver is "No". REWRITE
      *>   G2RW05-N (PIC 9(3)) FROM CA shall be refused at the REWRITE
      *>   line; the valid CD sender CN is g2rw03 L2. Fails if it
      *>   compiles. The CD draws COBOL0903 (§2002:G.2) besides.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RW05.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "g2rw05.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  F.
       01  G2RW05-N PIC 9(3).
       COMMUNICATION SECTION.
       CD  COUT FOR OUTPUT.
       01  CREC.
           05 CA PIC A(3).
           05 CN PIC 9(3).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN I-O F.
           READ F AT END CONTINUE END-READ.
           REWRITE G2RW05-N FROM CA.
           CLOSE F.
           STOP RUN.
