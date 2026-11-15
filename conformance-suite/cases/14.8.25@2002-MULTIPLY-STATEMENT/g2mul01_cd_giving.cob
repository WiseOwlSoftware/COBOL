      *>--- conformance-case
      *> id: g2mul01_cd_giving
      *> rules: [2002-SR-14.8.25.2-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.25.2 SR2 - "Identifier-3 shall reference a data
      *>   item of category numeric or numeric-edited." Each GIVING
      *>   receiver here is a communication-section item: the input
      *>   CD's TEXT LENGTH TL and MESSAGE COUNT MC (implicitly
      *>   integers of 4 and 6 digits, §2002:A.1.2.3 GR2 h, k), and
      *>   the numeric OC 9(4) and numeric-edited OD ZZZ9 of the
      *>   record under the output CD (§2002:A.1.2.2 SR9). The product
      *>   is stored in each identifier-3 (§2002:14.8.25.3 GR2):
      *>   1  6 x 7 = 42 into TL            -> 0042
      *>   2  12 x 3 = 36 into OD (ZZZ9)    -> "  36"
      *>   3  5 x 5 = 25 into OC and OD     -> 0025, "  25"
      *>   4  TL (42) x 2 = 84 into MC 9(6) -> 000084
      *>   Fails if a CD numeric or numeric-edited GIVING item is
      *>   refused, or edited/zero-filled wrongly. The alphanumeric CD
      *>   field refused as identifier-3 is g2mul02.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2MUL01.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD CDIN FOR INPUT
           SYMBOLIC QUEUE IS SQ
           TEXT LENGTH IS TL
           MESSAGE COUNT IS MC.
       CD CDOUT FOR OUTPUT
           TEXT LENGTH IS OTL.
       01 OUTREC.
          05 FILLER PIC X(10).
          05 OC     PIC 9(4).
          05 OD     PIC ZZZ9.
       PROCEDURE DIVISION.
       MAIN.
           MULTIPLY 6 BY 7 GIVING TL.
           DISPLAY "1 TL=" TL.
           MULTIPLY 12 BY 3 GIVING OD.
           DISPLAY "2 OD=[" OD "]".
           MULTIPLY 5 BY 5 GIVING OC OD.
           DISPLAY "3 OC=" OC " OD=[" OD "]".
           MULTIPLY TL BY 2 GIVING MC.
           DISPLAY "4 MC=" MC.
           STOP RUN.
