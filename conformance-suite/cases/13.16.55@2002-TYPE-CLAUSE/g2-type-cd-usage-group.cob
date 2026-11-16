      *>--- conformance-case
      *> id: g2-type-cd-usage-group
      *> rules: [2002-SR-13.16.55.2-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.55.2 SR5 - "No group item to which the subject of
      *> the entry is subordinate shall contain a GROUP-USAGE, SIGN, or
      *> USAGE clause." The record after a CD is an ordinary data
      *> description entry (§2002:A.1.1), so the rule reaches it: A, a
      *> record under an output CD, carries USAGE DISPLAY and X TYPE TT
      *> is subordinate to it. Expected: COBOL1538 at line 25 (X).
      *> Fails if the CD record's entries escape the check.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2TYP05.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  TT TYPEDEF.
           05  TT1 PIC X.
       COMMUNICATION SECTION.
       CD  CD-OUT FOR OUTPUT
           TEXT LENGTH IS CO-LEN
           STATUS KEY IS CO-STAT.
       01  A USAGE DISPLAY.
           05  X TYPE TT.
       PROCEDURE DIVISION.
           DISPLAY "BAD".
           STOP RUN.
