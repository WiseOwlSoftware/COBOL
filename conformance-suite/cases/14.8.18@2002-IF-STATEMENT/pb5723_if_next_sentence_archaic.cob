      *>--- conformance-case
      *> id: pb5723_if_next_sentence_archaic
      *> rules: [2002-FMT-14.8.18.1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.18.1 Format 2 (historic): IF condition-1 THEN
      *>   {statement-1 | NEXT SENTENCE} ... with the NOTE "NEXT
      *>   SENTENCE is an archaic feature and its use should be
      *>   avoided." (cite.py --check -> OK).
      *> §2002:G.1 item 3 lists the phrase as archaic and §2002:3.1.10
      *>   requires the optional archaic-element warning (cite.py OK):
      *>   WiseOwl gives COBOL0903 at the NEXT SENTENCE phrase
      *>   (wiseowl/diagnostics/pb5723_if_next_sentence_archaic.json).
      *> DERIVATION: sentence 1: G = 2 is true, so NEXT SENTENCE
      *>   transfers control past the period and the ELSE DISPLAY does
      *>   not run. Sentence 2: G = 3 is false, the ELSE NEXT SENTENCE
      *>   arm runs and DISPLAY "SKIPPED" does not.
      *>   Output: AFTER.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5723IF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G PIC 9 VALUE 2.
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF G = 2 NEXT SENTENCE ELSE DISPLAY "ELSE".
           IF G = 3 DISPLAY "SKIPPED" ELSE NEXT SENTENCE.
           DISPLAY "AFTER"
           STOP RUN.
