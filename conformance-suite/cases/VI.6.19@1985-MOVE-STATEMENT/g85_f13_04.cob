      *>--- conformance-case
      *> id: g85_f13_04
      *> rules: [1985-GR-VI.6.19.4-6]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.19.4 GR6 - Table 1, Legality of Types of MOVE
      *> Statements. Every "Yes" cell is exercised once per receiving
      *> category the cell's column names, and each result is the one
      *> the cell's general rule states (§1985:VI.6.19.4 GR4):
      *>   4a alphanumeric (edited) receiver: standard alignment, space
      *>      fill, the operational sign not moved, no de-editing;
      *>   4b numeric (edited) receiver: decimal-point alignment, zero
      *>      fill, absolute value into an unsigned receiver,
      *>      alphanumeric sent as an unsigned integer, numeric edited
      *>      de-edited;
      *>   4c alphabetic receiver: standard alignment, space fill.
      *> Line prefixes name the cell: A alphabetic, X alphanumeric,
      *> E alphanumeric edited, I numeric integer, N numeric
      *> noninteger, D numeric edited sender. The "No" cells are not
      *> written: a GR states no refusal this case could witness.
      *> The numeric receivers are DISPLAYed through their group items
      *> (G-INT, G-NON), so each line shows the receiver's character
      *> content and no implementor-defined DISPLAY conversion
      *> (§1985:VI.6.10.4 GR1). The alphabetic receiver is DISPLAYed
      *> through its group item G-ALPHA: after the X and E moves it
      *> holds digits and "/", content not compatible with its
      *> alphabetic PICTURE, and a direct reference to such content is
      *> undefined (§1985:VI.6.4.7); the group reference is not.
      *> Why each leg can fail: a refusal of any line rejects a move the
      *> table makes legal; a value other than the derived one breaks
      *> the cell's rule.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F1304.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  S-ALPHA PIC A(4) VALUE "ABCD".
       01  S-ALNUM PIC X(4) VALUE "0042".
       01  S-ANED  PIC XX/XX VALUE "12/34".
       01  S-INT   PIC S9(4) VALUE -125.
       01  S-NON   PIC 9(2)V9(3) VALUE 12.345.
       01  S-NED   PIC ZZ9.99.
       01  G-ALPHA.
           05  R-ALPHA PIC A(6).
       01  R-ANED  PIC XXBXXX.
       01  R-ALNUM PIC X(6).
       01  G-INT.
           05  R-INT   PIC 9(4).
       01  G-NON.
           05  R-NON   PIC 9(3)V99.
       01  R-NED   PIC ZZZ9.99.
       PROCEDURE DIVISION.
       P1.
           MOVE S-ALPHA TO R-ALPHA.
           DISPLAY "A-ALPHA [" G-ALPHA "]".
           MOVE S-ALPHA TO R-ANED.
           DISPLAY "A-ANED  [" R-ANED "]".
           MOVE S-ALPHA TO R-ALNUM.
           DISPLAY "A-ALNUM [" R-ALNUM "]".
           MOVE S-ALNUM TO R-ALPHA.
           DISPLAY "X-ALPHA [" G-ALPHA "]".
           MOVE S-ALNUM TO R-ANED.
           DISPLAY "X-ANED  [" R-ANED "]".
           MOVE S-ALNUM TO R-ALNUM.
           DISPLAY "X-ALNUM [" R-ALNUM "]".
           MOVE S-ALNUM TO R-INT.
           DISPLAY "X-INT   [" G-INT "]".
           MOVE S-ALNUM TO R-NON.
           DISPLAY "X-NON   [" G-NON "]".
           MOVE S-ALNUM TO R-NED.
           DISPLAY "X-NED   [" R-NED "]".
           MOVE S-ANED TO R-ALPHA.
           DISPLAY "E-ALPHA [" G-ALPHA "]".
           MOVE S-ANED TO R-ANED.
           DISPLAY "E-ANED  [" R-ANED "]".
           MOVE S-ANED TO R-ALNUM.
           DISPLAY "E-ALNUM [" R-ALNUM "]".
           MOVE S-INT TO R-ANED.
           DISPLAY "I-ANED  [" R-ANED "]".
           MOVE S-INT TO R-ALNUM.
           DISPLAY "I-ALNUM [" R-ALNUM "]".
           MOVE S-INT TO R-INT.
           DISPLAY "I-INT   [" G-INT "]".
           MOVE S-INT TO R-NON.
           DISPLAY "I-NON   [" G-NON "]".
           MOVE S-INT TO R-NED.
           DISPLAY "I-NED   [" R-NED "]".
           MOVE S-NON TO R-INT.
           DISPLAY "N-INT   [" G-INT "]".
           MOVE S-NON TO R-NON.
           DISPLAY "N-NON   [" G-NON "]".
           MOVE S-NON TO R-NED.
           DISPLAY "N-NED   [" R-NED "]".
           MOVE 12.5 TO S-NED.
           MOVE S-NED TO R-ANED.
           DISPLAY "D-ANED  [" R-ANED "]".
           MOVE S-NED TO R-ALNUM.
           DISPLAY "D-ALNUM [" R-ALNUM "]".
           MOVE S-NED TO R-INT.
           DISPLAY "D-INT   [" G-INT "]".
           MOVE S-NED TO R-NON.
           DISPLAY "D-NON   [" G-NON "]".
           MOVE S-NED TO R-NED.
           DISPLAY "D-NED   [" R-NED "]".
           STOP RUN.
