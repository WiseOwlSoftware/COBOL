      *> PB79 - GROUP-USAGE BIT (ISO 13.18.29): the group is a bit group AND a bit data item, class and category
      *> boolean, "treated as though it were an elementary data item of usage bit ... described with PICTURE 1(m),
      *> where m is the bit length of the group" (GR1 b): its operand value is its bit string (the subordinates'
      *> boolean positions in order - 8.5.1.6.3 rule 1 puts same-level bit items, elementary or bit group, at
      *> successive bit positions), a MOVE to / from it pads and truncates in boolean positions, FUNCTION LENGTH
      *> is its bit length (15.50.4 r1) with NO trailing filler (the 8.5.1.6.3 NOTE excludes "a record that is
      *> entirely a bit group"), BYTE-LENGTH its bytes. Data-model design D19 + D20. Also pinned: a group-level
      *> USAGE BIT clause applies to its PICTURE-1 subordinates (13.18.60.4 GR1 - the same rule GROUP-USAGE BIT
      *> implies; before D20 the leaves stayed display-form and G occupied 8 bytes), and a bit group NESTED in an
      *> alphanumeric group shares a byte with a preceding same-level bit item (rule 1).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB79BIT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BG GROUP-USAGE BIT.
          05 B1 PIC 1(3) VALUE B"101".
          05 B2 PIC 1(5) VALUE B"11001".
          05 SUB.
             10 B3 PIC 1(4) VALUE B"0110".
       01 BR PIC 1(12) USAGE BIT.
       01 BS PIC 1(6) USAGE BIT.
       01 BD PIC 1(12).
       01 G USAGE BIT.
          05 GA PIC 1(5) VALUE B"10101".
          05 GB PIC 1(3) VALUE B"111".
       01 R2.
          05 P PIC 1(3) USAGE BIT VALUE B"110".
          05 Q GROUP-USAGE BIT.
             10 Q1 PIC 1(2) VALUE B"00".
             10 Q2 PIC 1(3) VALUE B"010".
          05 T PIC X VALUE "t".
       PROCEDURE DIVISION.
           DISPLAY "LEN=" FUNCTION LENGTH(BG) " BYTES=" FUNCTION BYTE-LENGTH(BG).
           MOVE BG TO BR.
           DISPLAY "BR=[" BR "]".
           MOVE BG TO BS.
           DISPLAY "BS=[" BS "]".
           MOVE BG TO BD.
           DISPLAY "BD=[" BD "]".
           MOVE B"111100001111" TO BG.
           DISPLAY "B1=" B1 " B2=" B2 " B3=" B3.
           DISPLAY "BG=[" BG "]".
           IF BG = B"111100001111" DISPLAY "EQ" ELSE DISPLAY "NE" END-IF.
           MOVE B"1" TO BG.
           DISPLAY "BG=[" BG "]".
           IF (BG B-OR B"010000000000") = B"110000000000"
               DISPLAY "OR-EQ" ELSE DISPLAY "OR-NE" END-IF.
           DISPLAY "SUB-LEN=" FUNCTION LENGTH(SUB) " G-LEN=" FUNCTION LENGTH(G)
                   " G-BYTES=" FUNCTION BYTE-LENGTH(G).
           DISPLAY "R2-BYTES=" FUNCTION BYTE-LENGTH(R2) " Q-LEN=" FUNCTION LENGTH(Q).
           MOVE B"11" TO Q1.
           DISPLAY "Q=[" Q "] R2-T=[" T "]".
           STOP RUN.
