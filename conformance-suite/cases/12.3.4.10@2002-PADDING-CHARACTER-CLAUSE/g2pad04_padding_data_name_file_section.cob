      *>--- conformance-case
      *> id: g2pad04_padding_data_name_file_section
      *> rules: [2002-SR-12.3.4.10.2-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:12.3.4.10.2 SR3 - data-name-1 shall be "defined in the
      *>   working-storage, local-storage, or linkage section" (cite.py
      *>   OK). A refusal: data-name-1 on line 20 names FPAD, a PIC X
      *>   item of the file section, which the rule excludes; a compiler
      *>   that accepts it (and DISPLAYs ACCEPTED) fails. Expected:
      *>   COBOL6848 at line 20 at --std 2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2PAD04.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "G2PAD04.DAT"
               ORGANIZATION IS SEQUENTIAL
               PADDING CHARACTER IS FPAD.
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       01  R1 PIC X(4).
       01  R1-ALT.
           05 FPAD PIC X.
           05 FILLER PIC X(3).
       WORKING-STORAGE SECTION.
       01  W1 PIC X VALUE "*".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT F1
           MOVE "AB**" TO R1 WRITE R1
           CLOSE F1
           DISPLAY "ACCEPTED"
           STOP RUN.
