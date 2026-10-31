      *>--- conformance-case
      *> id: pb4238-qualified-section-header
      *> rules: [FMT-14.2.1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4238. ISO 14.2.1 Format 1 prints the section header as
      *> "section-name-1 SECTION." (cite.py --check 14.2.1 "section-name-1" -> OK):
      *> a section-name being defined is written bare, and 14.4.1 gives
      *> qualification to the procedure-name that refers to a paragraph, so
      *> "S1 OF X SECTION." is refused COBOL5716 at every edition. It used to
      *> declare a section named S1OFX, which "PERFORM S1OFX" then ran.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4238SH.
       PROCEDURE DIVISION.
       S0 SECTION.
       MAIN-P.
           PERFORM S1OFX.
           STOP RUN.
       S1 OF X SECTION.
       P2.
           DISPLAY "IN S1".
