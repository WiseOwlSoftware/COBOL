      *>--- conformance-case
      *> id: pb5723_search_next_sentence_archaic
      *> rules: [2002-FMT-14.8.34.1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.34.1 Format 1 allows NEXT SENTENCE in place of
      *>   the imperative statement of the AT END and WHEN phrases.
      *> §2002:G.1 item 3 lists the phrase in the SEARCH statement as
      *>   archaic and §2002:3.1.10 requires the optional warning
      *>   (cite.py OK): WiseOwl gives COBOL0903 at the phrase
      *>   (wiseowl/diagnostics/pb5723_search_next_sentence_archaic.json).
      *> DERIVATION: TK (1) = 0 satisfies the WHEN, so NEXT SENTENCE
      *>   transfers control past the next separator period, so the
      *>   AT END phrase's DISPLAY "END" does not run.
      *>   Output: DONE.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5723SR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TB.
          05 TE OCCURS 3 INDEXED BY TI.
             10 TK PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-PARA.
           SET TI TO 1
           SEARCH TE AT END DISPLAY "END"
               WHEN TK (TI) = 0 NEXT SENTENCE.
           DISPLAY "DONE"
           STOP RUN.
