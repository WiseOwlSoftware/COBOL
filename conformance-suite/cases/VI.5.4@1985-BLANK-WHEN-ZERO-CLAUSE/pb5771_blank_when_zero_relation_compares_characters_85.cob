      *>--- conformance-case
      *> id: pb5771_blank_when_zero_relation_compares_characters_85
      *> rules: [1985-GR-VI.5.4.4-2, 1985-GR-VI.6.3.1.1.2-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5771 - at COBOL-85 a blanked BLANK WHEN ZERO item is
      *> a nonnumeric operand holding spaces. §1985:VI.5.4.4 GR1: "the
      *> item will contain nothing but spaces if the value of the item
      *> is zero"; GR2: "the category of the item is considered to be
      *> numeric edited", class alphanumeric, so a relation with a
      *> numeric operand is the nonnumeric comparison of
      *> §1985:VI.6.3.1.1.2 GR1: "the numeric operand is treated as
      *> though it were moved to an elementary alphanumeric data item
      *> of the same size as the numeric data item". 1985 has no rule
      *> that an all-spaces item is zero (that is §2023:13.18.8.4 GR3,
      *> new in 2023; pb509_blank_when_zero_relation is its witness).
      *> Why each leg can fail (the defect read the spaces as zero):
      *>   WP = WZ  "     " with "00000": NE (was EQ).
      *>   WP > WN  "     " with WN S9(5) -3 moved as "00003": a space
      *>            precedes "0" in the native sequence: NOT GT (was GT).
      *>   WP < WZ  "     " with "00000": LT.
      *>   WP = WE  "     " with WE ZZZ99 holding "   00": NE.
      *>   WX = WZ  the moved copy, an X(5) of spaces: NE (control).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5771BWZ85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WP  PIC 9(5) BLANK WHEN ZERO.
       01 WZ  PIC 9(5) VALUE 0.
       01 WN  PIC S9(5) VALUE -3.
       01 WE  PIC ZZZ99.
       01 WX  PIC X(5).
       PROCEDURE DIVISION.
       P1.
           MOVE 0 TO WP.
           MOVE WP TO WX.
           MOVE 0 TO WE.
           DISPLAY "WP=[" WP "] WE=[" WE "]".
           IF WP = WZ DISPLAY "EQ" ELSE DISPLAY "NE".
           IF WP > WN DISPLAY "GT" ELSE DISPLAY "NOT GT".
           IF WP < WZ DISPLAY "LT" ELSE DISPLAY "NOT LT".
           IF WP = WE DISPLAY "E-EQ" ELSE DISPLAY "E-NE".
           IF WX = WZ DISPLAY "X-EQ" ELSE DISPLAY "X-NE".
           STOP RUN.
