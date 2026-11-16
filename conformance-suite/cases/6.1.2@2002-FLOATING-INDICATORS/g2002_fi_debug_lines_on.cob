      *>--- conformance-case
      *> id: g2002_fi_debug_lines_on
      *> rules: [2002-SR-6.1.2.1-1, 2002-SR-6.1.2.1-2, 2002-SR-6.1.2.1-4]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> Floating indicators on 2002 debugging lines, WITH DEBUGGING
      *> MODE. A debugging line's program-text area may hold comments
      *> and floating indicators (§2002:6.2.3 "when the indicator area
      *> contains a debugging indicator, a continuation indicator, or a
      *> source indicator"), and a fixed D line becomes a ">>D" line
      *> (§2002:6.4 step 5), so each rule below reaches D and >>D lines.
      *> §2002:6.1.2.1 SR1 - "a space is implied immediately following
      *> a floating comment indicator": "*>X. DISPLAY "BAD"" on the D2
      *> and F4 lines is a comment indicator and comment-text
      *> (§2002:6.2.6.2 "All characters following the floating comment
      *> indicator up to margin R are comment-text"); a scanner that
      *> read "*>X." as one character-string, or ran BAD, fails.
      *> §2002:6.1.2.1 SR2 - an inline comment preceded by a separator
      *> space is accepted on a D line (D1, D2) and a >>D line (F3, F4).
      *> §2002:6.1.2.1 SR4 - a floating literal continuation indicator
      *> continues an alphanumeric (ABCD), a national (N-OK) and a
      *> boolean (B-OK) literal begun on a D line, each by one form;
      *> the continuation is appended after the opening quotation
      *> symbol of the next line (§2002:6.4 step 9), so a dropped or
      *> misplaced piece prints N-BAD / B-BAD or fails to compile.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2FI01.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. X WITH DEBUGGING MODE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WN PIC N(4) VALUE N"NATL".
       01 WB PIC 1(4) VALUE B"1001".
       PROCEDURE DIVISION.
      D    DISPLAY "D1" *> an inline comment on a fixed debugging line
      D    DISPLAY "D2" *>X. DISPLAY "BAD"
           >>D DISPLAY "F3" *> an inline comment on a >>D line
           >>D DISPLAY "F4" *>X. DISPLAY "BAD"
      D    DISPLAY "AB"-
           "CD"
      D    IF WN = N"NA"-
           "TL" DISPLAY "N-OK" ELSE DISPLAY "N-BAD" END-IF
      D    IF WB = B"10"-
           "01" DISPLAY "B-OK" ELSE DISPLAY "B-BAD" END-IF
           DISPLAY "MAIN".
           STOP RUN.
