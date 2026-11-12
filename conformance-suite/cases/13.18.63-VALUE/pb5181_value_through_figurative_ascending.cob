      *>--- conformance-case
      *> id: pb5181_value_through_figurative_ascending
      *> rules: [SR-13.18.63.3-26]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5181 - the legal side. §2023:13.18.63.3 SR26 (cite.py --check OK):
      *> "the value of literal-2 shall be less than the value of literal-3", and
      *> a figurative constant may stand wherever a format prints "literal"
      *> (§1985:IV.4.2.2.2.3 at 85). HIGH-VALUE is the character with the
      *> highest ordinal position in the program collating sequence and
      *> LOW-VALUE the lowest (§2023:8.3.3.6.4 GR6, GR7), SPACE precedes "Z" and
      *> ALL "A" precedes ALL "Z" in the native sequence, so each range below
      *> ascends. DERIVATION (X = "MMM", native sequence, a literal shorter
      *> than the item is extended with spaces): L1 LOW-VALUE THRU HIGH-VALUE
      *> holds; L2 SPACE THRU "Z" holds ("MMM" is above "   " and below "Z  ");
      *> L3 "A" THRU HIGH-VALUE holds; L4 ALL "A" THRU ALL "Z" holds ("AAA" to
      *> "ZZZ"); L5 SPACE THRU "A" does not ("MMM" is above "A  ").
      *> Fails if: any of these ranges is refused, or answers other than above.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5181OK.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  X PIC X(3) VALUE "MMM".
           88 L1 VALUE LOW-VALUE THRU HIGH-VALUE.
           88 L2 VALUE SPACE THRU "Z".
           88 L3 VALUE "A" THRU HIGH-VALUE.
           88 L4 VALUE ALL "A" THRU ALL "Z".
           88 L5 VALUE SPACE THRU "A".
       PROCEDURE DIVISION.
       P1.
           IF L1 DISPLAY "L1 TRUE" ELSE DISPLAY "L1 FALSE".
           IF L2 DISPLAY "L2 TRUE" ELSE DISPLAY "L2 FALSE".
           IF L3 DISPLAY "L3 TRUE" ELSE DISPLAY "L3 FALSE".
           IF L4 DISPLAY "L4 TRUE" ELSE DISPLAY "L4 FALSE".
           IF L5 DISPLAY "L5 TRUE" ELSE DISPLAY "L5 FALSE".
           STOP RUN.
