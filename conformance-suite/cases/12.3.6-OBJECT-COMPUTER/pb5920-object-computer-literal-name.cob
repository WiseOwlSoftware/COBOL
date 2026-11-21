      *>--- conformance-case
      *> id: pb5920-object-computer-literal-name
      *> rules: [FMT-12.3.6.2, 1985-SR-VI.4.4.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5920 - the OBJECT-COMPUTER twin of pb5920-source-computer-literal-name: a computer-name is a system-name,
      *> which is a COBOL word (§1985:VI.4.4.3 SR1 "Computer-name is a system-name."; §2002:8.3.1.1.2, §2014:8.3.1.1.2 and
      *> §2023:8.3.2.3.1). The paragraph's clauses may follow its period with no name, so the grammar meets the literal as
      *> the paragraph's first clause and the refusal used to say it was "not a clause of the OBJECT-COMPUTER paragraph"
      *> (COBOL1970). It now says what was written: the paragraph's computer-name-1 is no system-name (COBOL7654), at
      *> every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5920OL.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. "XYZ".
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "PB5920OL".
           STOP RUN.
