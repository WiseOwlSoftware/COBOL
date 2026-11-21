      *>--- conformance-case
      *> id: pb6011-source-computer-reserved-word-name
      *> rules: [FMT-12.3.5.2, 1985-SR-VI.4.3.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB6011, PB5985 - SOURCE-COMPUTER. MERGE. writes a reserved word where
      *> computer-name-1 stands (§1985:VI.4.3.3 SR1; §1985:III.2). The paragraph ends at its
      *> period when no name follows, so the parse layer met MERGE as the next CONFIGURATION
      *> SECTION paragraph (COBOL1971); it is now COBOL7654, at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6011SC.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. MERGE.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "X".
           STOP RUN.
