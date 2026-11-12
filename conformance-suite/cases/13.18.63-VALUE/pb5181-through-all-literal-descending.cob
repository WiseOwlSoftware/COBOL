      *>--- conformance-case
      *> id: pb5181-through-all-literal-descending
      *> rules: [SR-13.18.63.3-26]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5181 - §2023:13.18.63.3 SR26 (cite.py --check OK): "the value of
      *> literal-2 shall be less than the value of literal-3" when THROUGH is
      *> specified; §1985:VI.5.15.5 GR2 says it for COBOL-85 ("Wherever the THRU
      *> phrase is used, literal-2 must be less than literal-3"), and a
      *> figurative constant may stand wherever "literal" is printed
      *> (§1985:IV.4.2.2.2.3). ALL "Z" repeats to the item size and so follows ALL "A" in the native sequence.
      *> The range was accepted and the condition-name never true.
      *> Fails if: the program compiles at any edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5181AL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  X PIC X VALUE "M".
           88 C1 VALUE ALL "Z" THRU ALL "A".
       PROCEDURE DIVISION.
       P1.
           STOP RUN.
