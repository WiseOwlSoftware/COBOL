*>--- conformance-case
*> id: pb3221_picture_p_beside_extended_float_2023
*> rules: [SR-13.18.40.3-16, SR-13.18.40.3-19, GR-13.18.40.4-14]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3221 - a 'P' string to the RIGHT of an extended editing sign control symbol's floating
      *> string. ISO 1989:2023 13.18.40.5 rule 6 names "the extended editing sign control symbols, if
      *> specified" among the floating insertion symbols, and its second floating symbol "represents the
      *> leftmost limit of the numeric data", so the string's positions are digit positions exactly as a
      *> floating currency string's are. The 'P's of LLLPP therefore stand in the RIGHTMOST digit positions
      *> (13.18.40.3 SR16), and 13.18.40.4 GR14 puts the assumed decimal point "to the right of the string of
      *> 'P's": the value is held in hundreds. SR19 lets a 'V' "immediately follow the last symbol 'P'".
      *> The composition validator counted only '+', '-' and the currency symbol as floating digit positions
      *> while the analyzer counted every floating symbol, so it read the 'P's of LLLPPV as a LEADING string
      *> and refused the 'V' after them (COBOL1935, legal source rejected); both now read the one list.
      *> The negative sibling negative/pb3221-picture-p-between-extended-float holds the shape SR16 refuses.
      *>
      *> P01 LLLPPV with -300: 300 is 3 hundreds; the floating string's two digit positions hold 03, the
      *>     leading zero is replaced and the NEGATIVE literal floats to its left => " -3".
      *> P02 the same item with 1200: 12 hundreds; POSITIVE is not written, so the sign position takes the
      *>     default, one space (13.18.40.3 SR12 c) => " 12".
      *> P03 LLLPP with -300 - the same picture without the 'V', which the old reading already accepted
      *>     and scaled the same way => " -3".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3221PF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P01 PIC LLLPPV EDITING L FOR NEGATIVE IS "-".
       01 P03 PIC LLLPP EDITING L FOR NEGATIVE IS "-".
       PROCEDURE DIVISION.
           MOVE -300 TO P01
           DISPLAY "P01=[" P01 "]"
           MOVE 1200 TO P01
           DISPLAY "P02=[" P01 "]"
           MOVE -300 TO P03
           DISPLAY "P03=[" P03 "]"
           STOP RUN.
