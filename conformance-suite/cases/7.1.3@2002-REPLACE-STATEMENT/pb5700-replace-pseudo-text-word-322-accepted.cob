      *>--- conformance-case
      *> id: pb5700-replace-pseudo-text-word-322-accepted
      *> rules: [2002-SR-7.1.3.2-9]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.1.3.2 SR9 - "The length of a text-word within
      *> pseudo-text shall be from 1 through 322 characters".
      *> Pseudo-text-1 holds ZZ1 and ONE text-word of 322 characters (an
      *> alphanumeric literal of 320 characters with its quotation
      *> marks). The sequence ZZ1 followed by that literal is not in the
      *> text, so nothing is replaced and the program displays OKQ.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5700D.
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
      -    "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"== BY ==ZZ2==.
           DISPLAY "OK" ZZ1.
           STOP RUN.
