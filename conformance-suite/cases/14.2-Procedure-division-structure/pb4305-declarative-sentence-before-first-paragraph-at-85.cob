      *>--- conformance-case
      *> id: pb4305-declarative-sentence-before-first-paragraph-at-85
      *> rules: [FMT-14.2.1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4305 (branch review round 1, R1-2) - the edition half
      *> of pb4305_declarative_sentence_before_first_paragraph_2002.
      *> COBOL-85 wrote a declarative section as "section-name SECTION.
      *> USE statement. [paragraph-name. [sentence]...]...": a sentence
      *> after the USE statement and before the first paragraph-name is
      *> the 2002 relaxation (Annex F.2 item 90). Expected: COBOL0900 at
      *> 85, at the sentence after the USE statement.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4305DN.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "pb4305dn-does-not-exist.dat"
               FILE STATUS IS F1-ST.
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 F1-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01 F1-ST PIC XX VALUE "00".
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON F1.
           DISPLAY "D1 " F1-ST.
       END DECLARATIVES.
       M1 SECTION.
       P1.
           OPEN INPUT F1.
           DISPLAY "DONE".
           STOP RUN.
