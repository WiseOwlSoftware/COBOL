      *>--- conformance-case
      *> id: 1985-SR-VI_5_9_3-7
      *> rules: [1985-SR-VI.5.9.3-7]
      *> accept: [85]
      *> source-format: fixed
      *>---
      *> §1985:VI.5.9.3 SR7: "The asterisk when used as the zero
      *> suppression symbol and the clause BLANK WHEN ZERO may not
      *> appear in the same entry." The legal forms: an asterisk
      *> picture with no BLANK WHEN ZERO clause, and BLANK WHEN ZERO
      *> on a picture that suppresses with Z. A holds 5 under
      *> ***9, which puts an asterisk in each leading-zero position
      *> (§1985:VI.5.9.4 GR8d: "Each '*' (asterisk) in the
      *> character-string represents a leading numeric character
      *> position into which an asterisk will be placed when the
      *> content of that position is a leading zero"). B holds zero
      *> under BLANK WHEN ZERO, so it holds nothing but spaces
      *> (§1985:VI.5.4.4 GR1: "the item will contain nothing but
      *> spaces if the value of the item is zero"). A leg fails if a
      *> compiler refuses either entry.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3178SR5937.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A           PIC ***9.
       01  B           PIC Z9 BLANK WHEN ZERO.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 5 TO A.
           MOVE 0 TO B.
           DISPLAY "[" A "]".
           DISPLAY "[" B "]".
           STOP RUN.
