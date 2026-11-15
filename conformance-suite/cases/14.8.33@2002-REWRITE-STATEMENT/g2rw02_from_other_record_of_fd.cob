      *>--- conformance-case
      *> id: g2rw02_from_other_record_of_fd
      *> rules: [2002-SR-14.8.33.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.33.2 SR1 - "Record-name-1 and identifier-1 shall
      *>   not reference the same storage area." (cite.py: OK)
      *>   G2RW02-A and G2RW02-B are both record description entries
      *>   of FD F, so each is a redefinition of the one record area
      *>   (§2002:13.16.31.3 GR3 "Multiple level 1 entries subordinate
      *>   to a CD, FD, or SD entry represent implicit redefinitions of
      *>   the same area", cite.py: OK). REWRITE G2RW02-A
      *>   FROM G2RW02-B therefore names the same storage area under
      *>   another name and shall be refused at the REWRITE line.
      *>   Fails if it compiles (a check that compares only names,
      *>   as g2rw01 alone would allow, misses this leg).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RW02.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "g2rw02.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  F.
       01  G2RW02-A PIC X(4).
       01  G2RW02-B PIC X(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN I-O F.
           READ F AT END CONTINUE END-READ.
           REWRITE G2RW02-A FROM G2RW02-B.
           CLOSE F.
           STOP RUN.
