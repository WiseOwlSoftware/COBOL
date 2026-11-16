      *>--- conformance-case
      *> id: g85_f06_04
      *> rules: [1985-GR-VI.4.5.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.4.5.4 GR1 - every clause of the container's
      *>   SPECIAL-NAMES paragraph applies to the contained program
      *>   G85F0604I (which has no SPECIAL-NAMES of its own), and the
      *>   container's switch condition-names SW1-ON and SW1-OFF are
      *>   referenced from it.
      *> Lines and the clause each pins (each fails if the contained
      *> program sees the default instead of the container's clause):
      *>   DP=    DECIMAL-POINT IS COMMA (the literal 12,5 and the
      *>          edited picture Z9,99)
      *>   CUR=   CURRENCY SIGN IS "#" with DECIMAL-POINT IS COMMA
      *>   CLASS= CLASS HEXDIG
      *>   SYM=   SYMBOLIC CHARACTERS STAR IS 1 IN PUNCT (the first
      *>          character of the alphabet PUNCT, "*")
      *>   SW=    SWITCH-1 IS SW1 with its ON/OFF condition-names, set
      *>          by format 3 SET through the container's mnemonic-name
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F0604.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. XX.
       OBJECT-COMPUTER. XX.
       SPECIAL-NAMES.
           SWITCH-1 IS SW1 ON STATUS IS SW1-ON OFF STATUS IS SW1-OFF
           ALPHABET PUNCT IS "*" "-"
           SYMBOLIC CHARACTERS STAR IS 1 IN PUNCT
           CLASS HEXDIG IS "0" THRU "9" "A" THRU "F"
           CURRENCY SIGN IS "#"
           DECIMAL-POINT IS COMMA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-DUMMY PIC X.
       PROCEDURE DIVISION.
       P-MAIN.
           CALL "G85F0604I".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F0604I.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-E   PIC Z9,99.
       01  WS-ED  PIC ##.##9,99.
       01  WS-X   PIC X(4) VALUE "1A9F".
       01  WS-Y   PIC X(4) VALUE "1G9F".
       01  WS-S   PIC X(3).
       PROCEDURE DIVISION.
       P-INNER.
           MOVE 12,5 TO WS-E.
           DISPLAY "DP=" WS-E.
           MOVE 1234,5 TO WS-ED.
           DISPLAY "CUR=" WS-ED.
           IF WS-X IS HEXDIG
               DISPLAY "CLASS 1A9F=YES"
           ELSE
               DISPLAY "CLASS 1A9F=NO".
           IF WS-Y IS HEXDIG
               DISPLAY "CLASS 1G9F=YES"
           ELSE
               DISPLAY "CLASS 1G9F=NO".
           MOVE ALL STAR TO WS-S.
           DISPLAY "SYM=" WS-S.
           SET SW1 TO ON.
           IF SW1-ON
               DISPLAY "SW AFTER ON=ON"
           ELSE
               DISPLAY "SW AFTER ON=OFF".
           SET SW1 TO OFF.
           IF SW1-OFF
               DISPLAY "SW AFTER OFF=OFF"
           ELSE
               DISPLAY "SW AFTER OFF=ON".
           EXIT PROGRAM.
       END PROGRAM G85F0604I.
       END PROGRAM G85F0604.
