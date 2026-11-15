      *>--- conformance-case
      *> id: g2rfm01_refmod_cd_numeric_display_and_national
      *> rules: [2002-GR-8.4.2.3.3-2, 2002-GR-8.4.2.3.3-3]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.4.2.3.3 GR2 - an item "explicitly or implicitly described
      *> as usage DISPLAY and its category is other than alphanumeric" is
      *> "operated upon for purposes of reference modification as if it
      *> were redefined as a data item of class and category alphanumeric
      *> of the same size". The CD fields are implicitly described
      *> integers: MESSAGE COUNT PICTURE 9(6), TEXT LENGTH PICTURE 9(4)
      *> (§2002:A.1.2.3 GR2k, GR2h), DESTINATION COUNT PICTURE 9(4)
      *> (§2002:A.1.2.3 GR16a), usage DISPLAY by default.
      *>   MC = 123456: MC(4:3) "456", MC(2:) "23456" (GR5c: no length
      *>   runs to the rightmost position).
      *>   TL = 42 is "0042": TL(1:2) "00", TL(3:2) "42".
      *>   MOVE "XY" TO MC(1:2): the unique item is alphanumeric (GR6c),
      *>   an alphanumeric move, so MC(1:6) is "XY3456". A numeric view
      *>   (or a numeric conversion) fails these legs.
      *>   DC = 12 is "0012": DC(1:2) "00", DC(3:2) "12".
      *>   NE PIC Z9.99 (numeric-edited, display) = 3.5 is " 3.50":
      *>   NE(2:4) "3.50".
      *> §2002:8.4.2.3.3 GR3 - usage NATIONAL, category other than
      *> national, is operated upon "as if it were redefined as a data
      *> item of class and category national of the same size". ONUM is
      *> PIC 9(3) USAGE NATIONAL in a record description entry under an
      *> output CD (§2002:A.1.2.2 SR9: it implicitly redefines the CD's
      *> record area). ONUM = 789: ONUM(2:2) "89" (national positions;
      *> a byte view would split characters). MOVE N"Z" TO ONUM(1:1) is
      *> a national move into a national unique item (GR6c): "Z89".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RFM01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  NE  PIC Z9.99.
       COMMUNICATION SECTION.
       CD  CIN FOR INPUT
           SYMBOLIC QUEUE IS QN
           TEXT LENGTH IS TL
           MESSAGE COUNT IS MC.
       CD  COUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS OTL.
       CD  CNAT FOR OUTPUT.
       01  OREC.
           02  ONUM PIC 9(3) USAGE NATIONAL.
           02  FILLER PIC X(20).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 123456 TO MC.
           MOVE 42 TO TL.
           DISPLAY "MC [" MC(4:3) "][" MC(2:) "]".
           DISPLAY "TL [" TL(1:2) "][" TL(3:2) "]".
           MOVE "XY" TO MC(1:2).
           DISPLAY "MV [" MC(1:6) "]".
           MOVE 12 TO DC.
           DISPLAY "DC [" DC(1:2) "][" DC(3:2) "]".
           MOVE 3.5 TO NE.
           DISPLAY "NE [" NE(2:4) "]".
           MOVE 789 TO ONUM.
           DISPLAY "NA [" ONUM(2:2) "]".
           MOVE N"Z" TO ONUM(1:1).
           DISPLAY "NM [" ONUM(1:3) "]".
           STOP RUN.
