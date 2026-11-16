      *>--- conformance-case
      *> id: g85_f04_01
      *> rules: [1985-GR-IV.4.3.6-1, 1985-GR-IV.4.3.6-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.3.6 GR1 - a numeric receiver: "The data is aligned by
      *> decimal point and is moved to the receiving digit positions with
      *> zero fill or truncation on either end as required"; with no
      *> explicit point the item "is treated as if it has an assumed
      *> decimal point immediately following its rightmost digit".
      *> Lines N1..N4 pin GR1: N1 truncates at both ends (123.456 into
      *> 99V9 is 23.4), N2 zero fills at both ends (1.5 into 999V99 is
      *> 001.50), N3 drops the fraction of a sender into an integer
      *> receiver with no point (12.75 into 999 is 012), N4 treats an
      *> integer sender as pointed after its last digit (1234 into 99V9
      *> is 34.0). A move that aligned by the right end, rounded, or
      *> left-justified would fail each line. N1 and N2 are shown through
      *> their group, whose DISPLAY transfers the item's character positions
      *> (the assumed point occupies none), so no implementor conversion of
      *> a numeric operand (§1985:VI.6.10.4 GR1) enters the expected text.
      *> §1985:IV.4.3.6 GR2 - a numeric edited receiver: "aligned by
      *> decimal point with zero fill or truncation at either end as
      *> required within the receiving character positions",
      *> "except where editing requirements cause replacement of the
      *> leading zeros". E1 zero fills (001.50), E2 has the leading
      *> zeros replaced by Z editing (  1.50), E3 truncates both ends
      *> (12345.678 into Z9.9 is 45.6), E4 has no point so the fraction
      *> drops (7.9 into ZZZ is two spaces and 7).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F0401.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  S4  PIC 9(4) VALUE 1234.
       01  G-N1.
           05  N1  PIC 99V9 VALUE 0.
       01  G-N2.
           05  N2  PIC 999V99 VALUE 0.
       01  N3  PIC 999 VALUE 0.
       01  E1  PIC 999.99.
       01  E2  PIC ZZ9.99.
       01  E3  PIC Z9.9.
       01  E4  PIC ZZZ.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 123.456 TO N1.
           DISPLAY "N1 " G-N1.
           MOVE 1.5 TO N2.
           DISPLAY "N2 " G-N2.
           MOVE 12.75 TO N3.
           DISPLAY "N3 " N3.
           MOVE S4 TO N1.
           DISPLAY "N4 " G-N1.
           MOVE 1.5 TO E1.
           DISPLAY "E1 [" E1 "]".
           MOVE 1.5 TO E2.
           DISPLAY "E2 [" E2 "]".
           MOVE 12345.678 TO E3.
           DISPLAY "E3 [" E3 "]".
           MOVE 7.9 TO E4.
           DISPLAY "E4 [" E4 "]".
           STOP RUN.
