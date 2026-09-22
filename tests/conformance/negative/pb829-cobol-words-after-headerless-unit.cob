      *> reject-at: 2023
       PROGRAM-ID. PB829CW.
      *> kb/Work PB829 (sibling sweep) - ISO 7.3.10.3 SR1: "The COBOL-WORDS directive may be
      *> specified only before the first IDENTIFICATION DIVISION within a compilation group."  The
      *> division header is optional (11.2.1), so this unit's identification division begins at the
      *> PROGRAM-ID line above, and the directive below is INSIDE it.  The COBOL-WORDS stage looked
      *> only for the header line, so the directive was accepted in silence; it now shares the ONE
      *> unit-start test with the LEAP-SECOND stage, which already knew the header-less forms.
       >>COBOL-WORDS UNDEFINE "ZQX"
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "PB829CW".
           STOP RUN.
