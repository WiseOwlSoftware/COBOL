      *>--- conformance-case
      *> id: 1985-GR-VI_6_3_1_1_2-1-scaled
      *> rules: [1985-GR-VI.6.3.1.1.2-1]
      *> accept: [85]
      *> source-format: fixed
      *>---
      *> kb/Work PB5187. §1985:VI.6.3.1.1.2 GR1: "the numeric operand is
      *> treated as though it were moved to an elementary alphanumeric
      *> data item of the same size as the numeric data item (in terms
      *> of standard data format characters), and the content of this
      *> alphanumeric data item were then compared to the nonnumeric
      *> operand". PP is PIC 99PP, and "The scaling position character
      *> 'P' is not counted in the size of the data item"
      *> (§1985:VI.5.9.4 GR8), so it is two characters long and the
      *> temporary is two characters: the move sends the four digit
      *> positions 1200 (§1985:VI.6.19.4 GR4, "all digit positions
      *> specified with this symbol are considered to have the value
      *> zero and are counted in the size of the sending operand"), and
      *> the two character receiver keeps the leftmost two, 12. The
      *> relation is therefore the one T2 = "12" makes after MOVE PP TO
      *> T2: PP = "12" is true, and PP = "1200" compares 12 with 1200,
      *> the shorter operand extended on the right by spaces, so it is
      *> false. A compiler that compares the four digit image fails
      *> both.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. REL2S.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  PP  PIC 99PP VALUE 1200.
       01  T2  PIC XX.
       01  T4  PIC X(4).
       PROCEDURE DIVISION.
       P1.
           MOVE PP TO T2.
           MOVE PP TO T4.
           DISPLAY "MOVE TO XX=[" T2 "] MOVE TO X(4)=[" T4 "]".
           IF PP = "12"
               DISPLAY "EQUAL 12"
           ELSE
               DISPLAY "NOT EQUAL 12".
           IF PP = "1200"
               DISPLAY "EQUAL 1200"
           ELSE
               DISPLAY "NOT EQUAL 1200".
           IF T2 = "12" DISPLAY "T2 EQUAL 12".
           STOP RUN.
