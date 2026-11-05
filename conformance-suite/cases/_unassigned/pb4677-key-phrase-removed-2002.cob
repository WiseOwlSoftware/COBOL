      *>--- conformance-case
      *> id: pb4677-key-phrase-removed-2002
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4677 - ISO/IEC 1989:2002 A.2.2.1 and A.2.3.1 print no KEY phrase
      *> (communication-key-phrase-removed-2002); CM102M's COBOL-85 writes it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4677X3.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD C1 FOR INPUT STATUS KEY IS S1.
       PROCEDURE DIVISION.
       MAIN.
           ENABLE INPUT C1 WITH KEY "PW".
           STOP RUN.
