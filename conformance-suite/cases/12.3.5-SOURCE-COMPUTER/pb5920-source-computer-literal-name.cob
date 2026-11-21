      *>--- conformance-case
      *> id: pb5920-source-computer-literal-name
      *> rules: [FMT-12.3.5.2, 1985-SR-VI.4.3.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5920 - a computer-name is a system-name, which is a COBOL word, so a literal written in its place
      *> is refused (§1985:VI.4.3.3 SR1 "Computer-name is a system-name."; §2002:8.3.1.1.2, §2014:8.3.1.1.2 and
      *> §2023:8.3.2.3.1 give the formation of every system-name as a COBOL word's with the implementor's restrictions).
      *> The SOURCE-COMPUTER paragraph ends at its period when no name follows, so the grammar meets the literal as
      *> the next CONFIGURATION SECTION paragraph, and the refusal used to say the literal was "not a paragraph of the
      *> CONFIGURATION SECTION" (COBOL1971). It now says what was written: the paragraph's computer-name-1 is no
      *> system-name (COBOL7654), at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5920SL.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. "XYZ".
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "PB5920SL".
           STOP RUN.
