      *>--- conformance-case
      *> id: g2002_alnum_literal_forms
      *> rules: [2002-FMT-8.3.1.2.1.1, 2002-SR-8.3.1.2.1.2-1, 2002-GR-8.3.1.2.1.3-3, 2002-GR-8.3.1.2.1.3-4]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.3.1.2.1.1 - format 1 written with both quotation
      *> symbols and format 2 (X"..." and X'...'), each with one or more
      *> characters (the braces), accepted and valued.
      *> §2002:8.3.1.2.1.2 SR1 - "shall be greater than zero and less
      *> than or equal to 160 alphanumeric character positions": lengths
      *> 1
      *> (A3, 'x') and exactly 160 (the continued MOVE literal)
      *> accepted;
      *> the zero-length and 161-position twins in this folder are
      *> refused.
      *> §2002:8.3.1.2.1.2 SR3 - "AB""C" holds one quotation mark.
      *> §2002:8.3.1.2.1.3 GR1 - separators are not part of the value,
      *> so
      *> FUNCTION LENGTH("AB""C") is 4 and LENGTH(X"414243") is 3.
      *> §2002:8.3.1.2.1.3 GR3 - the value is the string of character-1:
      *> lines 1-3 show the characters exactly, doubled quotes
      *> collapsed.
      *> §2002:8.3.1.2.1.3 GR4 - "each of which has the bit
      *> configuration
      *> specified by one occurrence of hex-character-sequence-1": with
      *> the
      *> implementor's two hex digits per character and the ASCII
      *> runtime
      *> set (docs/CONFORMANCE.md DOC-A.1-95), X"414243" is ABC, X'6162'
      *> ab,
      *> X"7A" z and X'30' 0; the item holding "Q" equals X"51" and the
      *> one holding X'6162' equals "ab" (line 7).
      *> EXPECTED, DERIVED: 1[AB"C] 2[D'E"F] 3[Q][x] 4[04]
      *> 5[ABC][ab][z0]
      *> 6[03] 7[HEX-EQ] 8[AB][B] 9[160]. A doubled quote kept twice, a
      *> separator counted, a hex group mis-split or a dropped
      *> continuation
      *> line each change a line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2ALN01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
           01 W160 PIC X(160).
           01 A1 PIC X(4) VALUE "AB""C".
           01 A2 PIC X(5) VALUE 'D''E"F'.
           01 A3 PIC X(1) VALUE "Q".
           01 H1 PIC X(3) VALUE X"414243".
           01 H2 PIC X(2) VALUE X'6162'.
           01 N2 PIC 99.
           01 N3 PIC 999.
       PROCEDURE DIVISION.
           DISPLAY "1[" A1 "]"
           DISPLAY "2[" A2 "]"
           DISPLAY "3[" A3 "][" 'x' "]"
           MOVE FUNCTION LENGTH("AB""C") TO N2
           DISPLAY "4[" N2 "]"
           DISPLAY "5[" H1 "][" H2 "][" X"7A" X'30' "]"
           MOVE FUNCTION LENGTH(X"414243") TO N2
           DISPLAY "6[" N2 "]"
           IF A3 = X"51" AND H2 = "ab"
               DISPLAY "7[HEX-EQ]"
           ELSE
               DISPLAY "7[HEX-NE]"
           END-IF
           MOVE "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
      -    "AAAAAAAAAAAAAAAAAAAAAAAAABBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB
      -    "BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB"
               TO W160
           DISPLAY "8[" W160(80:2) "][" W160(160:1) "]"
           MOVE FUNCTION LENGTH(W160) TO N3
           DISPLAY "9[" N3 "]"
           STOP RUN.
