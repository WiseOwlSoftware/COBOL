      *>--- conformance-case
      *> id: pb4238-qualified-paragraph-header
      *> rules: [FMT-14.2.1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4238. ISO 14.2.1 Format 1 prints the paragraph header as
      *> "paragraph-name-1." (cite.py --check 14.2.1 "section-name-1" -> OK): a name
      *> being DEFINED is a bare user-defined word, and qualification is written
      *> only by a procedure-name that refers to a paragraph (14.4.1, "a
      *> paragraph-name (that may be qualified)"). The header "P1 OF S1." is
      *> refused COBOL5716 at every edition; it used to declare a paragraph named
      *> P1OFS1, which "PERFORM P1OFS1" then ran.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4238PH.
       PROCEDURE DIVISION.
       S1 SECTION.
       MAIN-P.
           PERFORM P1OFS1.
           STOP RUN.
       P1 OF S1.
           DISPLAY "IN P1".
