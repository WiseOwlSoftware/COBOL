      *>--- conformance-case
      *> id: pb3373-source-computer-signed-literal-clause
      *> rules: [FMT-12.3.5.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3373 - the CLOSED ISO 12.3.5.2 general format "SOURCE-COMPUTER. [ computer-name-1 ] ."
      *> holds no literal, and a SIGN-LED numeric literal is a literal like any other (ISO 8.3.3.3.2 2):
      *> "If a sign is used, it shall appear as the leftmost character of the literal").  Outside the
      *> list and argument regions -1 is two tokens, MINUS and INTEGERLIT, and the error production's run
      *> could not open at the sign, so the literal drew a bare parse error ("COBOL0001: unexpected '-'")
      *> naming no format.  The run now opens at a sign-led numeric literal as it opens at a one-token
      *> literal: XYZ is the computer-name and -1 is refused BY NAME in the SOURCE-COMPUTER paragraph's
      *> format (COBOL1970), quoted whole, the one diagnostic, at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. N3373SS.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. XYZ -1.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "N3373SS".
           STOP RUN.
