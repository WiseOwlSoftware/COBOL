      *>--- conformance-case
      *> id: pb5700-replace-pseudo-text-word-323-refused
      *> rules: [2002-SR-7.1.3.2-9]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.1.3.2 SR9 - "The length of a text-word within
      *> pseudo-text shall be from 1 through 322 characters".
      *> Pseudo-text-1 holds a text-word of 323 characters, one more
      *> than 2002 allows, so the program is refused at 2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5700E.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 ZZ1 PIC X VALUE "Q".
       PROCEDURE DIVISION.
       MAIN-P.
           REPLACE ==ZZ1 "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
      -    "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
      -    "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
      -    "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
      -    "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
      -    "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"== BY ==ZZ2==.
           DISPLAY "OK" ZZ1.
           STOP RUN.
