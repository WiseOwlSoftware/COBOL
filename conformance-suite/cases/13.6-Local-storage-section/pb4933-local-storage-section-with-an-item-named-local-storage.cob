      *>--- conformance-case
      *> id: pb4933-local-storage-section-with-an-item-named-local-storage
      *> rules: [FMT-13.6.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4933 - at COBOL-85, where 8.9 leaves LOCAL-STORAGE free, a data item named
      *> LOCAL-STORAGE is a user-defined word, and the LOCAL-STORAGE SECTION (§2023:13.6.2), a
      *> COBOL-2002 section, draws the same edition refusal COBOL0900 as without the item.
      *> The reservation gate retypes the declared free word to a user-defined word, but keeps
      *> the keyword token a parse read in its header (kb/Work PB4933, refusalWhereFree).
      *> Fails if the header draws COBOL0001 "unexpected 'LOCAL-STORAGE'".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4933L.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 LOCAL-STORAGE PIC X.
       LOCAL-STORAGE SECTION.
       01 L1 PIC X.
       PROCEDURE DIVISION.
       P1.
           STOP RUN.
