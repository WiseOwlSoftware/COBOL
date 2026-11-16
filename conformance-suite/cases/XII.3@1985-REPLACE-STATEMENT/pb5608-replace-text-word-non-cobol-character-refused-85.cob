      *>--- conformance-case
      *> id: pb5608-replace-text-word-non-cobol-character-refused-85
      *> rules: [1985-FMT-XII.3.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XII.3.2: the operands of REPLACE are pseudo-text, whose
      *> words are text words; §1985:III.2 (3): a text word is "Any
      *> other sequence of contiguous COBOL characters except comment
      *> lines and the word 'COPY', bounded by separators". The operand
      *> below is P, U+00A0 NO-BREAK SPACE, Q. U+00A0 is no COBOL
      *> character and no separator, so P Q is no text word and the
      *> program is refused at 85 (kb/Work PB5608). From 2014 a
      *> text-word takes any character of the compile-time coded set,
      *> and the same operand is one text-word
      *> (pb1543_nbsp_is_a_text_word_character).
       REPLACE ==P Q== BY =="OK"==.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5608N85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P PIC X VALUE "1".
       01 Q PIC X VALUE "2".
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "[" P Q "]"
           STOP RUN.
