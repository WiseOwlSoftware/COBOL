      *> kb/Work PB528 - the COBOL-2023 leg: a PICTURE EDITING character-1 is DELIBERATELY TRANSPARENT to the
      *> 13.18.40.6 Table 10 precedence walk, and this golden is what holds that determination in place.
      *> 13.18.40.6 gives a Table-10 precedence to 'es' ALONE - "If the EDITING phrase is specified, the
      *> precedence of 'es' as related to Table 10 ... has the same precedence as the 'cs' symbol in the
      *> column and row of non-floating insertion symbols" - and 13.18.40.3 SR12 makes 'es' the EXTENDED
      *> (FOR-phrase) symbol only: "If literal-1 is specified, character-1 is a fixed editing sign control
      *> symbol. If the FOR phrase is specified, character-1 is an extended editing sign control symbol."
      *> Even for 'es' the 'cs' mapping cannot be applied literally: Table 10's leading-currency-before-
      *> trailing-currency cell is BLANK while SR24 and SR25 expressly sanction TWO extended symbols, "the
      *> first occurrence of the EDITING phrase ... for the leftmost symbol in character-string-1 and the
      *> second occurrence ... for the rightmost symbol". Applying it would reject N02 and N03 below, which
      *> the standard's own text names as legal - so character-1 constrains no neighbour in the walk, and its
      *> placement stays SR8-SR12 / SR25 / SR26, screened by PictureAnalyzer.ValidateEditing.
      *>
      *> N01 9L9 EDITING "L" IS ":" with 12 - 13.18.40.5 rule 3 (simple insertion): character-1 inserts
      *>     literal-1 at each occurrence, sign-independent => "1:2".
      *> N02 LL EDITING "L" IS ":" - the very shape SR12 a's second bullet names as sufficient, "at least
      *>     two occurrences of one of the symbols from the set character-1 ...". What it proves is the
      *>     ACCEPT the Table-10 'cs' mapping would have refused (row 'cs' trailing has a BLANK 'cs'
      *>     leading column); nothing is moved to it, so the DISPLAY shows its initial state => "  ".
      *> N03 9L9F EDITING "L" FOR NEGATIVE IS "-" EDITING "F" FOR POSITIVE IS "+" with -12 - SR24's "either
      *>     one or two extended editing sign control symbols may be used" and SR25's leftmost/rightmost
      *>     placement. Table 9: character-1 with the NEGATIVE phrase renders literal-2 over a negative
      *>     value, and the POSITIVE phrase renders spaces over one => "1-2 ".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB528EDT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N01 PIC 9L9 EDITING "L" IS ":".
       01 N02 PIC LL EDITING "L" IS ":".
       01 N03 PIC 9L9F EDITING "L" FOR NEGATIVE IS "-"
                       EDITING "F" FOR POSITIVE IS "+".
       PROCEDURE DIVISION.
           MOVE 12 TO N01
           MOVE -12 TO N03
           DISPLAY "N01=[" N01 "]"
           DISPLAY "N02=[" N02 "]"
           DISPLAY "N03=[" N03 "]"
           STOP RUN.
