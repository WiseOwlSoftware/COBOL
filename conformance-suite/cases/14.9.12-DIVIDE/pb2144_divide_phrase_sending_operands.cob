      *>--- conformance-case
      *> id: pb2144_divide_phrase_sending_operands
      *> rules: [FMT-14.9.12.2, GR-14.9.12.4-1, GR-14.9.12.4-7]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB2144 - the INTO and BY operand of every DIVIDE GIVING format is ONE sending
      *> {identifier | literal} (ISO 14.9.12.2 Formats 2-5), parsed by the one arithmeticPhraseOperand
      *> rule the four arithmetic verbs share. This pins the legal shapes of Formats 2-5 with literal
      *> and identifier operands, the REMAINDER formats' operand among them (that arm used to bind
      *> the whole INTO phrase, so a second operand or a ROUNDED compiled clean; the refusals are
      *> the pb2144-divide-* tests).
      *> ISO 14.9.12.4 GR1: "the quotient is the result of dividing the dividend by the divisor"
      *> (cite.py --check 14.9.12.4 -> OK, General rules 1). GR6c: the subsidiary quotient is
      *> "derived from the quotient by truncation of digits at the least significant end" (OK,
      *> General rules 6). GR7: "the remainder is the result of multiplying the subsidiary quotient
      *> and the divisor and subtracting the product from the dividend" (OK, General rules 7).
      *> 14.7.4.1: without ROUNDED, "truncation is relative to the size provided for the resultant
      *> identifier" (OK).
      *> DERIVED BEFORE MEASURING (Q and R are PIC 9(4), so every value prints as four digits):
      *>   DIVIDE 7 INTO 23 GIVING Q REMAINDER R (Format 4): 23 / 7 = 3.28..., Q = 3,
      *>     R = 23 - 3 x 7 = 2                                   -> 0003 0002
      *>   DIVIDE 23 BY 7 GIVING Q REMAINDER R (Format 5): the same division -> 0003 0002
      *>   DIVIDE Y INTO X GIVING Q REMAINDER R, X = 23, Y = 5: 23 / 5 = 4.6, Q = 4,
      *>     R = 23 - 4 x 5 = 3                                   -> 0004 0003
      *>   DIVIDE X BY Y GIVING Q REMAINDER R: the same division -> 0004 0003
      *>   DIVIDE 4 INTO X GIVING Q (Format 2): 23 / 4 = 5.75, truncated -> 0005
      *>   DIVIDE X BY 2 GIVING Q (Format 3): 23 / 2 = 11.5, truncated   -> 0011
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2144P.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 Q PIC 9(4).
       01 R PIC 9(4).
       01 X PIC 9(4) VALUE 23.
       01 Y PIC 9(4) VALUE 5.
       PROCEDURE DIVISION.
       MAIN.
           DIVIDE 7 INTO 23 GIVING Q REMAINDER R
           DISPLAY Q " " R
           DIVIDE 23 BY 7 GIVING Q REMAINDER R
           DISPLAY Q " " R
           DIVIDE Y INTO X GIVING Q REMAINDER R
           DISPLAY Q " " R
           DIVIDE X BY Y GIVING Q REMAINDER R
           DISPLAY Q " " R
           DIVIDE 4 INTO X GIVING Q
           DISPLAY Q
           DIVIDE X BY 2 GIVING Q
           DISPLAY Q
           STOP RUN.
