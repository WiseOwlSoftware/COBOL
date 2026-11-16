      *>--- conformance-case
      *> id: g2002_currency_string_and_symbol
      *> rules: [2002-GR-12.2.6.3-13]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:12.2.6.3 GR13 - the CURRENCY SIGN clause gives a currency
      *> string "placed into numeric-edited data items when they are used
      *> as receiving items and de-edited from the data item when it is
      *> used as a sending item"; "Literal-7 represents the value of the
      *> currency string"; "If the CURRENCY SIGN clause is specified with
      *> the PICTURE SYMBOL phrase, literal-8 is used as the currency
      *> symbol"; without it "literal-7 is used as the currency symbol";
      *> "If the currency symbol is a lowercase letter, it is treated as
      *> its uppercase equivalent."
      *> Clause 1: string EUR, symbol literal-8 "g". Clause 2: no PICTURE
      *> SYMBOL, so "u" is both the string and the symbol. Each picture
      *> uses its symbol once, as a fixed insertion character; the first
      *> currency symbol adds the length of the currency string to the
      *> item (§2002:13.16.38.3 GR13d).
      *>   G-UP  PIC G999.99, G-LO PIC g999.99: 12.5 -> EUR012.50 both,
      *>         since the lowercase symbol g is treated as G.
      *>   U-UP  PIC U999.99, U-LO PIC u999.99: 12.5 -> u012.50 both:
      *>         the string is literal-7 "u" as written (lowercase), the
      *>         symbol u is treated as U.
      *>   N     MOVE G-UP TO a PIC 9(4)V99 item: de-editing removes EUR,
      *>         value 12.50, tested by a comparison (the DISPLAY form
      *>         of a V item is the implementor's, §2002:14.8.10.3 GR1)
      *>         (§2002:14.8.24.3 GR4b).
      *>   Z     MOVE G-UP TO a PIC ZZZ9.99 item: de-edited then edited,
      *>           12.50.
      *> A leg fails if the string or symbol is taken from the wrong
      *> literal, if a lowercase symbol is not matched to its uppercase
      *> form (the picture would be refused or edited as another symbol),
      *> or if the currency string is not removed on de-editing.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SPN07.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CURRENCY SIGN IS "EUR" WITH PICTURE SYMBOL "g"
           CURRENCY SIGN IS "u".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G-UP PIC G999.99.
       01  G-LO PIC g999.99.
       01  U-UP PIC U999.99.
       01  U-LO PIC u999.99.
       01  N    PIC 9(4)V99.
       01  Z    PIC ZZZ9.99.
       PROCEDURE DIVISION.
           MOVE 12.5 TO G-UP G-LO U-UP U-LO.
           DISPLAY "G-UP [" G-UP "]".
           DISPLAY "G-LO [" G-LO "]".
           DISPLAY "U-UP [" U-UP "]".
           DISPLAY "U-LO [" U-LO "]".
           MOVE G-UP TO N.
           IF N = 12.5
               DISPLAY "N=12.50"
           ELSE
               DISPLAY "N-WRONG".
           MOVE G-UP TO Z.
           DISPLAY "Z=[" Z "]".
           STOP RUN.
