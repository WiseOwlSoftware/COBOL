      *>--- conformance-case
      *> id: pb5700-copy-pseudo-text-word-323-refused
      *> rules: [2002-SR-7.1.2.2-11]
      *> files: [PB5700A.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.1.2.2 SR11 - "The length of a text-word within
      *> pseudo-text and within library text shall be from 1 through 322
      *> character positions".
      *> Pseudo-text-1 holds ONE text-word of 323 character positions
      *> (an alphanumeric literal of 321 characters with its quotation
      *> marks), one more than 2002 allows, so the program is refused at
      *> 2002. COBOL-2014 and 2023 allow 65,535 (§2014:7.2.2.2 SR11,
      *> §2023:7.2.3.3 SR9).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5700B.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       PROCEDURE DIVISION.
       MAIN-P.
           COPY PB5700A REPLACING =="AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
      -    "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
      -    "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
      -    "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
      -    "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
      -    "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"== BY ==S==.
           STOP RUN.
