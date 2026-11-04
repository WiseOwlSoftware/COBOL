      *>--- conformance-case
      *> id: pb4515_zero_suppress_stored_value_zero
      *> rules: [GR-13.18.40.5-6, GR-13.18.40.5-7, GR-14.6.8.2-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4515 - an all-zero-suppression picture tests "the value of the data to be stored", and
      *> that is the value the item ENDS UP HOLDING after the store's truncation, not the sending value.
      *> ISO/IEC 1989:2023 13.18.40.5 rule 7 b): "If the value is zero and the zero-suppression symbol is
      *> the symbol 'Z', all character positions of the item will contain the character space. If the value
      *> is zero and the zero-suppression symbol is the symbol '*', all character positions of the item will
      *> contain the character asterisk, but the decimal separator, when specified, will appear in the
      *> item." Rule 6 b) says the same of an all-floating-insertion picture: "If the value is zero, all
      *> character positions will contain the space character."
      *> WHICH value is stored is settled by 14.6.8.2 rule 5: a numeric-edited receiver takes "further
      *> alignment, zero fill or truncation" as 13.18.40 describes, so the HIGH-order digits a picture
      *> cannot show are truncated away: 1000 into a picture of three integer digit positions stores 000.
      *> Until kb/Work PB4515 the zero test read the pre-truncation sending value, so the BLANK WHEN ZERO
      *> guard (kb/Work PB566, which reads the stored digits) blanked while the same stores into the plain
      *> pictures printed a decimal point and zeros of a value that is zero.
      *>
      *> Each expected line is derived from the rules above, not from the compiler:
      *>   STAR-TRUNC   1000 -> PIC ***.**: three integer positions keep 000, two fraction positions 00; zero,
      *>                '*' -> six positions asterisks, the decimal point kept: "***.**".
      *>   ZED-TRUNC    1000 -> PIC ZZZ.ZZ: zero, 'Z' -> all six positions spaces (the point is a position too).
      *>   BWZ-TRUNC    1000 -> PIC ZZ9.99 BLANK WHEN ZERO: the stored 000.00 is zero -> six spaces.
      *>   PLUS-TRUNC   1000 -> PIC ++++.++: three integer digit positions (the first '+' is the symbol itself,
      *>                rule 6 fourth paragraph) keep 000; zero, floating insertion, rule 6 b) -> seven spaces.
      *>   MINUS-TRUNC  1000 -> PIC ----.--: the same with the minus -> seven spaces.
      *>   STAR-ZERO    0 -> PIC ***.**: "***.**" (the sending zero, the case that always worked).
      *>   STAR-NZ      1000.5 -> PIC ***.**: 000.50 is NOT zero -> rule 7 b) first sentence, the result is the
      *>                same as zero suppression to the left of the point only: "***.50".
      *>   ZED-NZ       1000.5 -> PIC ZZZ.ZZ: 000.50 -> "   .50".
      *>   STAR-KEEP    1234 -> PIC ***.**: the stored digits are 234.00 -> "234.00" (high-order 1 truncates).
      *>   STAR-LOW     5 -> PIC ***.**: "**5.00".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4515STORZ.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-STAR   PIC ***.**.
       01 WS-ZED    PIC ZZZ.ZZ.
       01 WS-BWZ    PIC ZZ9.99 BLANK WHEN ZERO.
       01 WS-PLUS   PIC ++++.++.
       01 WS-MINUS  PIC ----.--.
       PROCEDURE DIVISION.
       PB4515-LEAD.
           MOVE 1000 TO WS-STAR WS-ZED WS-BWZ WS-PLUS WS-MINUS
           DISPLAY "STAR-TRUNC[" WS-STAR "]"
           DISPLAY "ZED-TRUNC[" WS-ZED "]"
           DISPLAY "BWZ-TRUNC[" WS-BWZ "]"
           DISPLAY "PLUS-TRUNC[" WS-PLUS "]"
           DISPLAY "MINUS-TRUNC[" WS-MINUS "]"
           MOVE 0 TO WS-STAR
           DISPLAY "STAR-ZERO[" WS-STAR "]"
           MOVE 1000.5 TO WS-STAR WS-ZED
           DISPLAY "STAR-NZ[" WS-STAR "]"
           DISPLAY "ZED-NZ[" WS-ZED "]"
           MOVE 1234 TO WS-STAR
           DISPLAY "STAR-KEEP[" WS-STAR "]"
           MOVE 5 TO WS-STAR
           DISPLAY "STAR-LOW[" WS-STAR "]"
           STOP RUN.
