      *>--- conformance-case
      *> id: pb5700-copy-pseudo-text-word-322-accepted
      *> rules: [2002-SR-7.1.2.2-11]
      *> files: [PB5700A.cpy]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.1.2.2 SR11 - "The length of a text-word within
      *> pseudo-text and within library text shall be from 1 through 322
      *> character positions".
      *> Pseudo-text-1 holds ONE text-word, an alphanumeric literal of
      *> 320 characters with its two quotation marks: 322 character
      *> positions, the longest 2002 allows. It matches nothing of the
      *> library text, so the library text is copied unchanged and the
      *> program displays COPIED.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5700A.
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
      -    "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"== BY ==S==.
           STOP RUN.
