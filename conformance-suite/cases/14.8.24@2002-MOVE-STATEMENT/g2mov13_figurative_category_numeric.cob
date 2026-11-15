      *>--- conformance-case
      *> id: g2mov13_figurative_category_numeric
      *> rules: [2002-GR-14.8.24.3-5]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.24.3 GR5 - "Alphanumeric, boolean, national, and
      *>   numeric literals belong to the categories alphanumeric,
      *>   boolean, national, and numeric, respectively. The category
      *>   of figurative constants when used in the MOVE statement
      *>   depends on the category of the receiving operand as shown
      *>   in table 18". The 2002 Table 18 rows exercised here are the
      *>   ones a later edition removed: ZERO to Numeric and to
      *>   Numeric-edited is numeric; HIGH-VALUE, LOW-VALUE and QUOTE
      *>   to Numeric is alphanumeric, to Numeric-edited alphanumeric
      *>   "if usage is display" and national "if usage is national".
      *> The program collating sequences make each LOW-VALUE a digit
      *>   (§2002:12.2.5.3 GR9, §2002:12.2.6.3 GR7 and GR9): the
      *>   alphanumeric one is "5" (ALF5) and the national one N"7"
      *>   (NAT7); §2002:8.3.1.2.5.3 GR6 - a context requiring
      *>   national characters uses the national sequence. So an
      *>   alphanumeric LOW-VALUE moves as the unsigned integer 555
      *>   and a national one as 777 (§2002:14.8.24.3 GR4 b) 3.b),
      *>   with no EC-DATA-INCOMPATIBLE. Receivers are integers only:
      *>   the non-integer receivers are the separate GR4 lead.
      *> Line 1-3: ZERO is numeric to N, NS, NE, NN, NNE: zero, a
      *>   generated positive sign (+000), ZZ9.99 edits to "  0.00".
      *> Line 4: LOW-VALUE to N 9(3) and NI ZZ9 (display) is the
      *>   alphanumeric "5": 555 and 555.
      *> Line 5: LOW-VALUE to NN 9(3) USAGE NATIONAL is alphanumeric
      *>   (the Numeric row has no usage split): 555; to NNI ZZ9 USAGE
      *>   NATIONAL it is national: 777. Fails if the numeric-edited
      *>   national receiver takes the alphanumeric "5", or the
      *>   numeric national one the national "7".
      *> Line 6: the literal sentence - "12" (alphanumeric) to 9(3)
      *>   is the integer 12 (GR4 b) 3.c): 012; -12 (numeric) to X(3)
      *>   drops the sign (GR4 a)): "12 "; N"34" (national) to 9(3):
      *>   034; B"101" (boolean) to X(3) is its boolean value: 101.
      *> Line 7: HIGH-VALUE, QUOTE (and LOW-VALUE to NS) to numeric
      *>   and numeric-edited items are valid moves at 2002 (category
      *>   alphanumeric or national, Table 17 Yes), so they compile;
      *>   they sit on a path never taken because their content is not
      *>   numeric and the result would be undefined (GR4 b)).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2MOV13.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. G2HOST
           PROGRAM COLLATING SEQUENCE
               FOR ALPHANUMERIC IS ALF5
               FOR NATIONAL IS NAT7.
       SPECIAL-NAMES.
           ALPHABET ALF5 IS "5"
           ALPHABET NAT7 FOR NATIONAL IS N"7".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N    PIC 9(3).
       01 NS   PIC S9(3) SIGN LEADING SEPARATE.
       01 NE   PIC ZZ9.99.
       01 NI   PIC ZZ9.
       01 NN   PIC 9(3) USAGE NATIONAL.
       01 NNE  PIC ZZ9.99 USAGE NATIONAL.
       01 NNI  PIC ZZ9 USAGE NATIONAL.
       01 X3   PIC X(3).
       01 Y3   PIC X(3).
       01 FLG  PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN.
           MOVE ZERO TO N NS NE NN NNE.
           DISPLAY "1 N=" N " NS=" NS " NE=[" NE "]".
           DISPLAY "2 NN=" NN " NNE=[" NNE "]".
           IF N IS NUMERIC AND NS IS NUMERIC
               DISPLAY "3 ZERO-NUMERIC"
           ELSE
               DISPLAY "3 ZERO-NOT-NUMERIC"
           END-IF.
           MOVE LOW-VALUE TO N NI.
           DISPLAY "4 N=" N " NI=[" NI "]".
           MOVE LOW-VALUE TO NN NNI.
           DISPLAY "5 NN=" NN " NNI=[" NNI "]".
           MOVE "12" TO N.
           MOVE -12 TO X3.
           MOVE N"34" TO NN.
           MOVE B"101" TO Y3.
           DISPLAY "6 N=" N " X3=[" X3 "] NN=" NN " Y3=" Y3.
           IF FLG = "Y"
               MOVE HIGH-VALUE TO N NE NN NNE
               MOVE QUOTE TO N NE NN NNE
               MOVE LOW-VALUES TO NS
               MOVE HIGH-VALUES TO NS
               MOVE QUOTES TO NS
           END-IF.
           DISPLAY "7 ACCEPTED".
           STOP RUN.
