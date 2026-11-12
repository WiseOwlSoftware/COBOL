      *>--- conformance-case
      *> id: pb5172-class-in-alphabet-at-85
      *> rules: [GR-12.3.7.4-12]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5172 - the edition half of pb5172_class_in_alphabet_2002. §1985:VI.4.5.2 prints the CLASS clause
      *> with no IN phrase, and §1985:VI.4.5.4 GR10 takes a numeric literal as "The ordinal number of a character
      *> within the native character set". Expected: COBOL0900 at 85, at the IN phrase.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5172NCI.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET AL IS "Z" "Y"
           CLASS C1 IS 1 THRU 2 IN AL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W PIC X(2) VALUE "ZY".
       PROCEDURE DIVISION.
       P1.
           IF W IS C1 DISPLAY "TRUE" ELSE DISPLAY "FALSE".
           STOP RUN.
