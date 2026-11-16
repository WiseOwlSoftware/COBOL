      *>--- conformance-case
      *> id: pb5700-copy-library-text-word-323-refused
      *> rules: [2002-SR-7.1.2.2-11]
      *> files: [PB5700L.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.1.2.2 SR11 - "The length of a text-word within
      *> pseudo-text and within library text shall be from 1 through 322
      *> character positions".
      *> The library text holds one text-word of 323 character positions
      *> (a continued alphanumeric literal), the library-text half of
      *> the rule, so the COPY statement is refused at 2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5700C.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       PROCEDURE DIVISION.
       MAIN-P.
           COPY PB5700L.
           STOP RUN.
