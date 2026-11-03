      *>--- conformance-case
      *> id: pb4305_declarative_sentence_before_first_paragraph_2002
      *> rules: [FMT-14.2.1]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4305 (branch review round 1, R1-2) - 14.2.1 Format 1
      *> prints a declarative section as "section-name-1 SECTION.
      *> use-statement. [ sentence ] ... [ paragraph-name-1. [ sentence ]
      *> ... ] ...": sentences follow the USE statement with no
      *> paragraph-name before them (2002 Annex F.2 item 90 relaxed the
      *> COBOL-85 rule). D1 holds the USE sentence and one more
      *> sentence, and no paragraph. The OPEN of a file that is not
      *> there ends in status 35, so D1 runs: "D1 35", then "DONE".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4305DS.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "pb4305ds-does-not-exist.dat"
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
