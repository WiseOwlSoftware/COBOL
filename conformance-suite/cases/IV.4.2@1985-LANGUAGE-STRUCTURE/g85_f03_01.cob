      *>--- conformance-case
      *> id: g85_f03_01
      *> rules: [1985-SR-IV.4.2.1-6, 1985-SR-IV.4.2.1-8, 1985-SR-IV.4.2.1-9]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.2.1 SR6 - pseudo-text delimiters as separators: each
      *> opening == is preceded by a space, each closing == is followed by
      *> a space, a comma, a semicolon or a period, all in balanced pairs.
      *> Lines 1-3 (AAA BBB CCC) fail if any of those REPLACE operands is
      *> refused or left unreplaced.
      *> §1985:IV.4.2.1 SR8 - a space may precede the separators comma,
      *> semicolon, period and parentheses (lines 4-5); a space before
      *> the closing quotation mark is part of the literal (line 6).
      *> §1985:IV.4.2.1 SR9 - a space after the opening quotation mark is
      *> part of the literal (line 7); a period, comma or parenthesis in a
      *> PICTURE character-string or numeric literal is a PICTURE symbol
      *> or decimal point, and a PICTURE ends at a semicolon (lines 8-9);
      *> separator characters inside a literal or a comment line are not
      *> separators (line 10, and the comment line below).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F0301.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  AAA PIC X(3) VALUE "AAA".
       01  BBB PIC X(3) VALUE "BBB".
       01  CCC PIC X(3) VALUE "CCC".
       01  TBL.
           05  T-ELT PIC X OCCURS 3 TIMES.
       01  ED-N PIC ZZ,ZZ9.99.
       01  SEMI-N PIC 9(3); VALUE 7.
       PROCEDURE DIVISION.
       P1.
      * a comment line: "unpaired == , ;. ( are not separators here
           REPLACE ==XX1== BY ==AAA==, ==XX2== BY ==BBB==;
                   ==XX3== BY ==CCC==.
           DISPLAY XX1.
           DISPLAY XX2.
           DISPLAY XX3.
           MOVE "P" TO T-ELT ( 1 ) .
           MOVE "Q" TO T-ELT ( 2 ) .
           MOVE "R" TO T-ELT (3) .
           DISPLAY T-ELT ( 1 ) , T-ELT ( 2 ) ; T-ELT (3) .
           DISPLAY TBL .
           DISPLAY "[" "AB  " "]".
           DISPLAY "[" "  CD" "]".
           MOVE 1234.5 TO ED-N.
           DISPLAY ED-N.
           DISPLAY SEMI-N.
           DISPLAY "A,B.C;D(E)F==G".
           STOP RUN.
