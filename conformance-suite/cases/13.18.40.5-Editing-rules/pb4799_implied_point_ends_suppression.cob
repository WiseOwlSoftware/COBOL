      *>--- conformance-case
      *> id: pb4799_implied_point_ends_suppression
      *> rules: [GR-13.18.40.5-6, GR-13.18.40.5-7]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4799.
      * kb/Work PB4799 - the IMPLIED decimal point V ends zero and
      * floating suppression exactly as the special insertion '.' does.
      * 13.18.40.5 rule 7 a) "the corresponding replacement character is
      * placed into any character position immediately preceding
      * whichever of the following is encountered first" - the first
      * nonzero numeric character, the first position with no zero
      * suppression, or "the decimal point position"; rule 7 b) "If the
      * value is not zero, the result is the same as if the zero
      * suppression were defined only to the left of the decimal point
      * position"; rule 6 says the same of floating insertion. V holds
      * no character position, so the positions right of it are NOT
      * suppression positions: they show their digits, zeros included.
      *   python scripts/spec/cite.py --check 13.18.40.5 "the result is
      *   the same as if the floating insertion editing were defined
      *   only to the left of the decimal point position"  -> OK
      * Values (item V1 PIC 99V99 holding 0.05, V2 holding 7.05, V0
      * holding 0), every line derived by hand from the rules:
      *   ZZVZZ  0.05 -> digits 0005; Z,Z suppress the integer zeros,
      *                  the walk stops at the point; fraction 05 shows:
      *                  [  05]. The defect printed [   5].
      *   **V**  0.05 -> [**05]   (asterisk replacement, same walk)
      *   $$V$$  0.05 -> [ $05]  (first $ is the limit, integer digit 0
      *                  suppressed, $ lands in the position before the
      *                  point, fraction 05 shows)
      *   --V--  0.05 -> [  05]  (positive: the sign position is space)
      *   ZZVZZ  7.05 -> [ 705]
      *   ZZVZZ  0    -> [    ] (rule 7 b: zero, Z: all spaces)
      *   **V**  0    -> [****] (rule 7 b: zero, all asterisks)
      * Each leg can fail: a walk that runs past V blanks the fraction
      * zero ([   5], [ 705] -> [ 7 5]).
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 V1 PIC 99V99 VALUE 0.05.
       01 V2 PIC 99V99 VALUE 7.05.
       01 V0 PIC 99V99 VALUE 0.
       01 EA PIC ZZVZZ.
       01 EB PIC **V**.
       01 EC2 PIC $$V$$.
       01 ED PIC --V--.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE V1 TO EA DISPLAY "A=[" EA "]"
           MOVE V1 TO EB DISPLAY "B=[" EB "]"
           MOVE V1 TO EC2 DISPLAY "C=[" EC2 "]"
           MOVE V1 TO ED DISPLAY "D=[" ED "]"
           MOVE V2 TO EA DISPLAY "E=[" EA "]"
           MOVE V0 TO EA DISPLAY "F=[" EA "]"
           MOVE V0 TO EB DISPLAY "G=[" EB "]"
           STOP RUN.
