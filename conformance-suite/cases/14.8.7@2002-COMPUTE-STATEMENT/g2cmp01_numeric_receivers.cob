      *>--- conformance-case
      *> id: g2cmp01_numeric_receivers
      *> rules: [2002-SR-14.8.7.2-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.7.2 SR1 -- "Identifier-1 shall reference either an
      *> elementary numeric item or an elementary numeric-edited item."
      *> The accepted half: working-storage numeric and numeric-edited
      *> receivers, and the 2002 communication description entry's
      *> implicitly described numeric items (an obsolete facility at
      *> 2002, COBOL0903 warning): the input CD's TEXT LENGTH ("an
      *> integer of 4 digits, without an operational sign") and MESSAGE
      *> COUNT ("an integer of 6 digits"), §2002:A.1.2.3 GR2, and the
      *> output CD's DESTINATION COUNT and TEXT LENGTH, PICTURE 9(4),
      *> §2002:A.1.2.3 GR16. The refused half is g2cmp02 / g2cmp03.
      *> Values: 12.5 * 2 = 25 -> N1 PIC 9(3) "025", E1 PIC ZZ9.99
      *> " 25.00"; 10 / 3 ROUNDED into E2 PIC Z9.9 is 3.3 -> " 3.3"
      *> (numeric-edited receivers are displayed as their characters;
      *> a numeric item's conversion for DISPLAY is the implementor's,
      *> §2002:14.8.10.3 GR1, so only integers are displayed raw);
      *> 12 * 3 = 36 -> TL "0036", MC "000036"; DC "0002";
      *> TL + 1 = 37 -> OTL "0037".
      *> Fails if any of these receivers is refused, or a CD item is
      *> stored with a width other than its implicit description.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CMP1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N1 PIC 9(3).
       01 E1 PIC ZZ9.99.
       01 E2 PIC Z9.9.
       COMMUNICATION SECTION.
       CD CIN FOR INPUT
           SYMBOLIC QUEUE IS QN
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           MESSAGE COUNT IS MC.
       CD COUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS OTL.
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE N1 E1 = 12.5 * 2
           COMPUTE E2 ROUNDED = 10 / 3
           COMPUTE TL MC = 12 * 3
           COMPUTE DC = 2
           COMPUTE OTL = TL + 1
           DISPLAY "WS=" N1 " " E1 " " E2
           DISPLAY "CD=" TL " " MC " " DC " " OTL
           STOP RUN.
