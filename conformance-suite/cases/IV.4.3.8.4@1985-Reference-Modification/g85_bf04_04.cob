      *>--- conformance-case
      *> id: g85_bf04_04
      *> rules: [1985-GR-IV.4.3.8.4.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.3.8.4.4 GR2 - numeric, numeric edited, alphabetic
      *> and
      *> alphanumeric edited items reference-modified as alphanumeric
      * Each of the four categories GR2 names is "operated upon for
      * purposes of reference modification as if it were redefined as an
      * alphanumeric data item of the same size": its characters,
      * positions counted from the left, are selected and stored as
      * characters. N5 9(5) 12345: (2:3) = 234. NS S9(3) SIGN LEADING
      * SEPARATE -12 is -012 and its sign has an ordinal position (GR1):
      * (1:2) = -0. NE ZZ9.99 holding 3.5 is " 3.50": (3:4) = 3.50. AL
      * A(5) HELLO: (2:3) = ELL. AE XX/XX holding ABCD is AB/CD: (3:3) =
      * /CD. As receivers no conversion or editing happens: "9" into
      * N5(1:1) gives 92345, "X" into NE(1:1) gives "X 3.50", "Z" into
      * AL(5:1) gives HELLZ. An implementation that selected digits of
      * the value (not characters) fails NS; one that edited on receipt
      * fails NENEW.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF0404.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N5 PIC 9(5) VALUE 12345.
       01 NS PIC S9(3) SIGN LEADING SEPARATE VALUE -12.
       01 NE PIC ZZ9.99.
       01 AL PIC A(5) VALUE "HELLO".
       01 AE PIC XX/XX.
       01 W-X5 PIC X(5).
       PROCEDURE DIVISION.
       P1.
           MOVE N5 (2:3) TO W-X5.
           DISPLAY "N5=[" W-X5 "]".
           MOVE NS (1:2) TO W-X5.
           DISPLAY "NS=[" W-X5 "]".
           MOVE 3.5 TO NE.
           MOVE NE (3:4) TO W-X5.
           DISPLAY "NE=[" W-X5 "]".
           MOVE AL (2:3) TO W-X5.
           DISPLAY "AL=[" W-X5 "]".
           MOVE "ABCD" TO AE.
           MOVE AE (3:3) TO W-X5.
           DISPLAY "AE=[" W-X5 "]".
           MOVE "9" TO N5 (1:1).
           DISPLAY "N5NEW=" N5.
           MOVE "X" TO NE (1:1).
           DISPLAY "NENEW=[" NE "]".
           MOVE "Z" TO AL (5:1).
           DISPLAY "ALNEW=" AL.
           STOP RUN.
