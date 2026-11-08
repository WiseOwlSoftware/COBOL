      *>--- conformance-case
      *> id: pb4933-communication-section-with-items-named-communication-and-cd
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4933 - at COBOL-2014 and 2023, where 8.9 leaves COMMUNICATION and CD free, data
      *> items of those names are user-defined words, and the COMMUNICATION SECTION and its CD
      *> entry (ISO/IEC 1989:2002 Annex A.1, deleted by COBOL-2014) draw the same named removal
      *> COBOL0902 as without the items.
      *> The reservation gate retypes the declared free word to a user-defined word, but keeps
      *> the keyword token a parse read in its header (kb/Work PB4933, refusalWhereFree).
      *> Fails if the header or the CD entry draws COBOL0001.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4933C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 COMMUNICATION PIC X.
       01 CD PIC X.
       COMMUNICATION SECTION.
       CD C1 FOR INPUT.
       01 C1-REC PIC X(87).
       PROCEDURE DIVISION.
       P1.
           STOP RUN.
