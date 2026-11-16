      *>--- conformance-case
      *> id: g2pad03_padding_data_name_numeric
      *> rules: [2002-SR-12.3.4.10.2-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:12.3.4.10.2 SR3 - "Data-name-1 shall be a one-character
      *>   data item of category alphanumeric" (cite.py OK). A refusal:
      *>   data-name-1 on line 20 names PADN, PIC 9, of category
      *>   numeric, which the rule forbids; a compiler that accepts it
      *>   (and DISPLAYs ACCEPTED) fails. Expected: COBOL6848 at line 20
      *>   at --std 2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2PAD03.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "G2PAD03.DAT"
               ORGANIZATION IS SEQUENTIAL
               PADDING CHARACTER IS PADN.
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       01  R1 PIC X(4).
       WORKING-STORAGE SECTION.
       01  PADN PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT F1
           MOVE "AB**" TO R1 WRITE R1
           CLOSE F1
           DISPLAY "ACCEPTED"
           STOP RUN.
