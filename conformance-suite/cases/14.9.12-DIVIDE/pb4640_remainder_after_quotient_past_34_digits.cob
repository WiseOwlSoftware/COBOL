      *>--- conformance-case
      *> id: pb4640_remainder_after_quotient_past_34_digits
      *> rules: [GR-14.9.12.4-6, GR-14.9.12.4-7, DOC-A.1-70, DOC-A.1-123]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> PB4640 - DIVIDE ... GIVING identifier-3 REMAINDER identifier-4
      *> under NATIVE arithmetic when the QUOTIENT has more than 34
      *> digits.
      *>
      *> ISO 14.9.12.4 rule 7: "When native arithmetic is in effect,
      *> the remainder is the result of multiplying the subsidiary
      *> quotient and the divisor and subtracting the product from the
      *> dividend" (cite.py: OK 14.9.12.4). Rule 6 c): the subsidiary
      *> quotient is "derived from the quotient by truncation of digits
      *> at the least significant end and that has the same number of
      *> digits and the same decimal point location as the data item
      *> referenced by identifier-3" (cite.py: OK 14.9.12.4), so it has
      *> identifier-3's scale and every integer digit the quotient has.
      *> Native arithmetic is the implementor's (8.8.1.3: cite.py: OK
      *> 8.8.1.3); CLAUDE.md rule 1 sends that latitude to GnuCOBOL,
      *> whose cob_div_quotient and cob_div_remainder work on the exact
      *> decimal. A quotient past identifier-3 is a size error; with no
      *> SIZE ERROR phrase and checking not enabled, DOC-A.1-70 stores
      *> the remainder anyway (GnuCOBOL 3.2), and with a phrase the
      *> remainder keeps its value (14.9.12.4 6 c).
      *>
      *> The subsidiary quotient was the final-transfer quotient, held
      *> to the 34 digits of the standard-decimal intermediate,
      *> truncated: a 42-digit quotient lost its low-order 8 digits and
      *> the remainder was the dividend less a quotient that was too
      *> small, 99999999994 where 320 is owed.
      *>
      *> EXPECTED VALUES, DERIVED (Python integers) from A = 10**15 - 1
      *> and N = A*A*A + 4321 =
      *>   999999999999997000000000000003000000000004320 (45 digits):
      *>   BY1000 N / 1000 = 42 digits, a size error at PIC S9(18);
      *>          the subsidiary quotient is N // 1000 and the
      *>          remainder N - (N // 1000) * 1000 = 320.
      *>   ROUND  the same with ROUNDED on identifier-3: the
      *>          subsidiary quotient is still the truncated one: 320.
      *>   BY7    N - (N // 7) * 7 = 1 (N / 7 has 45 digits).
      *>   FRAC   N / 0.07 truncated at one place is N * 1000 // 7 in
      *>          tenths; N less that quotient times 0.07 is 0.006 in
      *>          a PIC S9(18)V999 identifier-4: 0.006.
      *>   NEG    the negative dividend -N: the truncation is toward
      *>          zero and the remainder takes the dividend's sign:
      *>          -320.
      *>   PHRASE ON SIZE ERROR on the same statement as BY1000:
      *>          identifier-4 keeps its value, 5, and the phrase
      *>          runs (14.9.12.4 6 c).
      *>   FIT    N / A = A*A, remainder 4321 (N = A * (A*A) + 4321
      *>          with 4321 < A): the quotient has 30 digits and fits
      *>          PIC S9(31), so there is no size error, the NOT ON
      *>          SIZE ERROR phrase runs, Q31 = A*A =
      *>          999999999999998000000000000001 and the remainder is
      *>          4321.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4640REMQUOT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A    PIC 9(18) VALUE 999999999999999.
       01 Q    PIC S9(18).
       01 R    PIC S9(18).
       01 Q1   PIC S9(18)V9.
       01 R1   PIC S9(18)V999.
       01 Q31  PIC S9(31).
       01 R31  PIC S9(18).
       01 E1   PIC -(18)9.
       01 E3   PIC -(18)9.9(3).
       01 E31  PIC -(31)9.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DIVIDE FUNCTION ABS (A * A * A + 4321) BY 1000
               GIVING Q REMAINDER R
           MOVE R TO E1
           DISPLAY "BY1000 " E1
           DIVIDE FUNCTION ABS (A * A * A + 4321) BY 1000
               GIVING Q ROUNDED REMAINDER R
           MOVE R TO E1
           DISPLAY "ROUND  " E1
           DIVIDE FUNCTION ABS (A * A * A + 4321) BY 7
               GIVING Q REMAINDER R
           MOVE R TO E1
           DISPLAY "BY7    " E1
           DIVIDE FUNCTION ABS (A * A * A + 4321) BY 0.07
               GIVING Q1 REMAINDER R1
           MOVE R1 TO E3
           DISPLAY "FRAC   " E3
           DIVIDE FUNCTION INTEGER (0 - A * A * A - 4321) BY 1000
               GIVING Q REMAINDER R
           MOVE R TO E1
           DISPLAY "NEG    " E1
           MOVE 5 TO R
           DIVIDE FUNCTION ABS (A * A * A + 4321) BY 1000
               GIVING Q REMAINDER R
               ON SIZE ERROR DISPLAY "PHRASE SIZE ERROR"
           END-DIVIDE
           MOVE R TO E1
           DISPLAY "PHRASE " E1
           DIVIDE FUNCTION ABS (A * A * A + 4321) BY A
               GIVING Q31 REMAINDER R31
               ON SIZE ERROR DISPLAY "FIT    SIZE ERROR"
               NOT ON SIZE ERROR DISPLAY "FIT    NO SIZE ERROR"
           END-DIVIDE
           MOVE Q31 TO E31
           DISPLAY "FITQ   " E31
           MOVE R31 TO E1
           DISPLAY "FITR   " E1
           STOP RUN.
