      *>--- conformance-case
      *> id: g2pic01_cd_record_picture
      *> rules: [2002-GR-13.16.38.3-4, 2002-GR-13.16.38.3-5,
      *>    2002-GR-13.16.38.3-8, 2002-GR-13.16.38.3-9,
      *>    2002-GR-13.16.38.3-15, 2002-GR-13.16.38.4-8,
      *>    2002-GR-13.16.38.4-10, 2002-GR-13.16.38.4-12]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> PICTURE on items of a record description entry that follows
      *> a CD entry (§2002:A.1.1); the CD facility draws the obsolete
      *> warning COBOL0903. Named locales only (en-US, id-ID, en-IN),
      *> so the result does not depend on the current locale.
      *> LEN line - §2002:13.16.38.3 GR4: size is the count of symbols
      *>   that are positions: AAA 3, XXX 3, 111 3 boolean positions
      *>   (§2002:15.39.3 RV1), NN 2 national positions (RV2), S9V99 3
      *>   (S without SEPARATE and V are not counted, GR13), 99CR 4,
      *>   ZZ9.99 6, -9.99E+99 9.
      *> F1/F2 - §2002:13.16.38.4 GR8: zero edited into a floating-
      *>   point item has zero digits and positive signs: 0 into
      *>   -9.99E+99 is " 0.00E+00" ('-' positive is a space, GR5
      *>   Table 9); -0.0 into +9.9E+999 is "+0.0E+000". Fails if the
      *>   minus of -0.0 survives.
      *> L2/L3 - §2002:13.16.38.4 GR10: BLANK WHEN ZERO beats locale
      *>   editing: zero gives 12 spaces in L2, while L3 (the same
      *>   picture, no BWZ) gives "0.00" right-justified (GR14 a,
      *>   GR15 a); 42 into L2 is "42.00" right-justified.
      *> L4/L5/L6 - §2002:13.16.38.4 GR12: separators and group size
      *>   come from LC_MONETARY: en-US 1,234,567.89, id-ID
      *>   1.234.567,89, en-IN 12,34,567.89 (a group of 3, then 2),
      *>   each right-justified in SIZE 14. Fails if any is hardcoded.
      *> INITIALIZE line - §2002:14.8.19.3 GR5 replaces only items of
      *>   the named categories: A1 (GR5 alphabetic) becomes QQQ while
      *>   X1 (alphanumeric) keeps XYZ; B1 (GR8 boolean) becomes 010;
      *>   N1 (GR9 national) becomes QZ; K1 (numeric) keeps 12; E1 and
      *>   the format 2 item L4 (§2002:13.16.38.3 GR15, numeric-edited)
      *>   take 7. Fails if any item lands in the wrong category.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2PIC01.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           LOCALE US IS "en-US"
           LOCALE IDN IS "id-ID"
           LOCALE IND IS "en-IN".
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 A1 PIC AAA.
           05 X1 PIC XXX.
           05 B1 PIC 111.
           05 N1 PIC NN.
           05 K1 PIC 99.
           05 S1 PIC S9V99.
           05 C1 PIC 99CR.
           05 E1 PIC ZZ9.99.
           05 F1 PIC -9.99E+99.
           05 F2 PIC +9.9E+999.
           05 L2 PIC ZZZ9.99 LOCALE IS US SIZE IS 12
                 BLANK WHEN ZERO.
           05 L3 PIC ZZZ9.99 LOCALE IS US SIZE IS 12.
           05 L4 PIC ZZZZZZ9.99 LOCALE IS US SIZE IS 14.
           05 L5 PIC ZZZZZZ9.99 LOCALE IS IDN SIZE IS 14.
           05 L6 PIC ZZZZZZ9.99 LOCALE IS IND SIZE IS 14.
       PROCEDURE DIVISION.
           DISPLAY "LEN A1 " FUNCTION LENGTH(A1)
                   " X1 " FUNCTION LENGTH(X1)
                   " B1 " FUNCTION LENGTH(B1)
                   " N1 " FUNCTION LENGTH(N1)
                   " S1 " FUNCTION LENGTH(S1)
                   " C1 " FUNCTION LENGTH(C1)
                   " E1 " FUNCTION LENGTH(E1)
                   " F1 " FUNCTION LENGTH(F1)
           MOVE 0 TO F1
           MOVE -0.0 TO F2
           DISPLAY "F1=[" F1 "] F2=[" F2 "]"
           MOVE 0 TO L2
           MOVE 0 TO L3
           DISPLAY "L2=[" L2 "] L3=[" L3 "]"
           MOVE 42 TO L2
           DISPLAY "L2=[" L2 "]"
           MOVE 1234567.89 TO L4 L5 L6
           DISPLAY "L4=[" L4 "] L5=[" L5 "] L6=[" L6 "]"
           MOVE "ABC" TO A1
           MOVE "XYZ" TO X1
           MOVE B"101" TO B1
           MOVE N"XY" TO N1
           MOVE 12 TO K1
           INITIALIZE CDREC
               REPLACING ALPHABETIC DATA BY "QQQ"
                         BOOLEAN DATA BY B"010"
                         NATIONAL DATA BY N"QZ"
                         NUMERIC-EDITED DATA BY 7
           DISPLAY "A1=[" A1 "] X1=[" X1 "] B1=[" B1 "] N1=[" N1
                   "] K1=[" K1 "]"
           DISPLAY "E1=[" E1 "] L4=[" L4 "]"
           STOP RUN.
