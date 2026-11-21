      *>--- conformance-case
      *> id: 1985-SR-IV_4_2_2_4-p1
      *> rules: [1985-SR-IV.4.2.2.4-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.2.2.4 p1 (Character-Strings). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-04.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * 1985-SR-IV.4.2.2.4-p1: a comment-entry may be any combination
      * of characters from the computer's character set.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CMTENT.
       AUTHOR. o'Brien "unbalanced {braces} ~ ^ | \ @ # $ % & ; ,
           lower case, digits 0123 and a period. PROCEDURE DIVISION.
       INSTALLATION. any < > = + - * / ( ) : ! ? _ text.
       DATE-WRITTEN. 21/11/2026 -- "quoted" and 'single'.
       DATE-COMPILED. whatever.
       SECURITY. NONE; COPY REPLACE END-PROGRAM words.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "COMMENT-ENTRIES ACCEPTED".
           STOP RUN.
