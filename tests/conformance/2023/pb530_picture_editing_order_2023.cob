      *> kb/Work PB530 - the COBOL-2023 leg: ISO 1989:2023 13.18.40.3 SR25's second sentence is an ORDER
      *> rule, and these are the spellings that obey it. "When extended editing sign control symbols are used
      *> and two are specified, the first occurrence of the EDITING phrase shall be for the leftmost symbol in
      *> character-string-1 and the second occurrence shall be for the rightmost symbol in character-string-1."
      *> The negative sibling negative/pb530-picture-editing-phrase-order holds the reversed spellings, which
      *> are COBOLNET1984; this golden holds the images they would otherwise have rendered.
      *>
      *> E01 F999.99L, the 'F' phrase first, with -1.5. Both extended symbols render their NEGATIVE literal
      *>     (Table 9: "character-1 NEGATIVE phrase ... literal-2" over a negative value), each at its own
      *>     position, over 3 + 2 digit positions => "(001.50)". The reversed phrase order renders
      *>     ")001.50(" - the same characters at the opposite ends, which is why the rule exists.
      *> E02 L9999.99F with -123.45 - Annex D.24's OWN example, "it is quite common to represent negative
      *>     items by enclosing them in parentheses". Four integer digit positions hold 0123 => "(0123.45)".
      *> E03 9L9F with -12 - a MID-STRING pair, legal under the reading this compiler takes (see the negative
      *>     sibling's header): "the leftmost symbol" is the leftmost of the TWO extended symbols, so the
      *>     rule is satisfied by writing the 'L' phrase first => "1(2)".
      *> E04 99L99 with -12.3 - a SINGLE extended symbol, which SR25 does not constrain at all: its first
      *>     sentence names only '+' and '-' and its second is conditioned on TWO being specified. Four digit
      *>     positions, no decimal point position, so -12.3 is held as 0012 => "00(12".
      *> E05 L$999 with -12 - SR26's SECOND sentence, the LEGAL leg: "the currency symbol when used shall
      *>     be either the leftmost symbol in character-string-1, optionally preceded by character-1". The
      *>     'L' renders its NEGATIVE literal, the fixed '$' its own position => "($012".
      *> E06 999$L with -12 - the other leg, "the rightmost symbol ... optionally followed by character-1"
      *>     => "012$(". The ILLEGAL placement (99$9L) is negative/pb530-picture-editing-currency-placement:
      *>     character-1's transparency to Table 10 makes these two legal and does NOT rescue that one.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB530EOR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 E01 PIC F999.99L EDITING "F" FOR NEGATIVE IS "("
                           EDITING "L" FOR NEGATIVE IS ")".
       01 E02 PIC L9999.99F EDITING "L" FOR NEGATIVE IS "("
                            EDITING "F" FOR NEGATIVE IS ")".
       01 E03 PIC 9L9F EDITING "L" FOR NEGATIVE IS "("
                       EDITING "F" FOR NEGATIVE IS ")".
       01 E04 PIC 99L99 EDITING "L" FOR NEGATIVE IS "(".
       01 E05 PIC L$999 EDITING "L" FOR NEGATIVE IS "(".
       01 E06 PIC 999$L EDITING "L" FOR NEGATIVE IS "(".
       PROCEDURE DIVISION.
           MOVE -1.5 TO E01
           MOVE -123.45 TO E02
           MOVE -12 TO E03
           MOVE -12.3 TO E04
           MOVE -12 TO E05
           MOVE -12 TO E06
           DISPLAY "E01=[" E01 "]"
           DISPLAY "E02=[" E02 "]"
           DISPLAY "E03=[" E03 "]"
           DISPLAY "E04=[" E04 "]"
           DISPLAY "E05=[" E05 "]"
           DISPLAY "E06=[" E06 "]"
           STOP RUN.
