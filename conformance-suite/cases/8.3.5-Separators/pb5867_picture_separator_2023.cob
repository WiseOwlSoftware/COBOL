      *>--- conformance-case
      *> id: pb5867_picture_separator_2023
      *> rules: [SR-8.3.5-2]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5867 - §2023:8.3.5 SR2: "The COBOL characters comma
      *> and semicolon, immediately followed by a space, are separators
      *> that may be used anywhere the separator space is used." So a
      *> comma or semicolon and a space may stand between PICTURE (PIC,
      *> PICTURE IS) and the character-string, in the CURRENCY SIGN
      *> clause's PICTURE SYMBOL phrase too, and at the start of a
      *> continuation of the entry on the next line:
      *>   H  picture is, x(2)   X(2)       "HH"            H=HH
      *>   M  PIC; U9(3).99      U = "EUR"  MOVE 12.5       M=EUR012.50
      *>   E  PIC ,999           ,999       VALUE 5         E=,005
      *>   G  PIC 9,999;         9,999      VALUE 1234      G=1,234
      *>   J  PIC / ; 9(2)       9(2)       66              J=66
      *> In PIC ,999 no space follows the comma, so it is the string's
      *> comma insertion symbol and not the separator.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5867B.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CURRENCY SIGN IS "EUR" WITH PICTURE, SYMBOL "U".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  H picture is, x(2) VALUE "HH".
       01  M PIC; U9(3).99.
       01  E PIC ,999 VALUE 5.
       01  G PIC 9,999; VALUE 1234.
       01  J PIC
               ; 9(2) VALUE 66.
       PROCEDURE DIVISION.
       P1.
           MOVE 12.5 TO M.
           DISPLAY "H=" H " M=" M.
           DISPLAY "E=" E " G=" G " J=" J.
           STOP RUN.
