      *>--- conformance-case
      *> id: pb5172_class_in_alphabet_2002
      *> rules: [GR-12.3.7.4-12]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5172 - the CLASS clause's IN alphabet-name phrase. §2023:12.3.7.4 GR12 a: a numeric literal is "the
      *> ordinal number of a character within the relevant native character set, or, when the IN phrase is
      *> specified, within the character set referenced by alphabet-name-4". AL lists "Z" then "Y", so in AL ordinal
      *> 1 is Z and ordinal 2 is Y: W ("ZY") is in C1 (IN-ALPHABET TRUE). C2 has no IN phrase, so its ordinals 1
      *> and 2 are the first two native characters, neither Z nor Y (NATIVE FALSE).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5172CI.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET AL IS "Z" "Y"
           CLASS C1 IS 1 THRU 2 IN AL
           CLASS C2 IS 1 THRU 2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W PIC X(2) VALUE "ZY".
       PROCEDURE DIVISION.
       P1.
           IF W IS C1 DISPLAY "IN-ALPHABET TRUE"
               ELSE DISPLAY "IN-ALPHABET FALSE".
           IF W IS C2 DISPLAY "NATIVE TRUE"
               ELSE DISPLAY "NATIVE FALSE".
           STOP RUN.
