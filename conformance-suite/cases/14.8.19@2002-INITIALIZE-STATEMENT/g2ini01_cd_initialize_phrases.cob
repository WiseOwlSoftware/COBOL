      *>--- conformance-case
      *> id: g2ini01_cd_initialize_phrases
      *> rules: [2002-FMT-14.8.19.1, 2002-GR-14.8.19.3-5,
      *>    2002-GR-14.8.19.3-6]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> INITIALIZE whose identifier-1 is a group of an output CD's
      *> record (§2002:A.1.2.2 SR8: a CD with no optional clause is
      *> followed by a level 01 entry; its VALUE clauses take effect
      *> at INITIALIZE, §2002:13.16.61.3 GR4). The facility draws the
      *> obsolete warning COBOL0903. Every cite.py --check here is OK.
      *> SCRAMBLE gives every item a non-initial value before each
      *> leg; SHOW prints, in order, FILLER(1:1) GX GN GL GK GE GA GB
      *> GT(1..3), then GW (QQ, or SP for national spaces).
      *> §2002:14.8.19.1 - every arm of the format: identifier-1
      *>   repeated (I5), WITH FILLER (I2), ALL TO VALUE (I1),
      *>   category-name TO VALUE with several names (I4-I6), THEN
      *>   REPLACING with literal-1 and identifier-2 (I4, I6), THEN TO
      *>   DEFAULT (I4); all eleven category names are written.
      *>   Fails if any granted form is refused.
      *> §2002:14.8.19.3 GR5 - a2 "If the FILLER phrase is not
      *>   specified, elementary data items with an explicit or
      *>   implicit FILLER clause" are excluded: the FILLER keeps G
      *>   (I1, I3-I6) and takes its VALUE F only WITH FILLER (I2).
      *>   a3 excludes GKR (REDEFINES GK, PIC 99): GK stays spaces in
      *>   I3 (else 00), Q in I4 (else 00), MM in I6 (else 04).
      *>   c1b: under a VALUE phrase only items whose entry has a
      *>   VALUE clause qualify (GE GA GB GW keep their content in
      *>   I1, I2, I6; in I5 only GL of its category list changes);
      *>   b2: each GT occurrence; c2 REPLACING, c3 DEFAULT, c4 bare.
      *>   (A pointer cannot sit in a CD record group: §2002:13.16.58.2
      *>   SR13 allows USAGE POINTER only at level 1.)
      *> §2002:14.8.19.3 GR6 - a3 the VALUE literal, per occurrence of
      *>   GT (P Q S); b the REPLACING literal / identifier-2 (Q, 3,
      *>   YY, 4); c the table: alphanumeric, alphabetic,
      *>   alphanumeric-edited -> SPACES, numeric, numeric-edited,
      *>   boolean -> ZEROES, national -> national SPACES (I3, I4).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2INZ01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-AL  PIC A(2) VALUE "YY".
       01 W-W   PIC X(2).
       COMMUNICATION SECTION.
       CD CDO FOR OUTPUT.
       01 IR.
          05 IG.
             10 FILLER PIC X VALUE "F".
             10 GX  PIC X(2) VALUE "VV".
             10 GN  PIC 9(2) VALUE 7.
             10 GL  PIC A(2) VALUE "AB".
             10 GK  PIC X(2) VALUE "KK".
             10 GKR REDEFINES GK PIC 9(2).
             10 GE  PIC ZZ9.
             10 GA  PIC XXBX.
             10 GB  PIC 1(2).
             10 GW  PIC N(2).
             10 GT  PIC X OCCURS 3 VALUES ARE "P" "Q" "S" FROM (1).
       PROCEDURE DIVISION.
       MAIN.
           PERFORM SCRAMBLE
           INITIALIZE IG ALL TO VALUE
           DISPLAY "I1 " WITH NO ADVANCING PERFORM SHOW
           PERFORM SCRAMBLE
           INITIALIZE IG WITH FILLER ALL TO VALUE
           DISPLAY "I2 " WITH NO ADVANCING PERFORM SHOW
           PERFORM SCRAMBLE
           INITIALIZE IG
           DISPLAY "I3 " WITH NO ADVANCING PERFORM SHOW
           PERFORM SCRAMBLE
           INITIALIZE IG NUMERIC TO VALUE
               THEN REPLACING ALPHANUMERIC DATA BY "Q"
                              NUMERIC-EDITED DATA BY 3
                              ALPHABETIC DATA BY W-AL
               THEN TO DEFAULT
           DISPLAY "I4 " WITH NO ADVANCING PERFORM SHOW
           PERFORM SCRAMBLE
           INITIALIZE GX GN GL ALPHABETIC ALPHANUMERIC-EDITED TO VALUE
           DISPLAY "I5 " WITH NO ADVANCING PERFORM SHOW
           PERFORM SCRAMBLE
           INITIALIZE IG DATA-POINTER PROGRAM-POINTER OBJECT-REFERENCE
               BOOLEAN NATIONAL NATIONAL-EDITED TO VALUE
               THEN REPLACING NUMERIC DATA BY 4
           DISPLAY "I6 " WITH NO ADVANCING PERFORM SHOW
           STOP RUN.
       SCRAMBLE.
           MOVE "G" TO IG (1:1)
           MOVE "XX" TO GX
           MOVE 99 TO GN
           MOVE "ZZ" TO GL
           MOVE "MM" TO GK
           MOVE 5 TO GE
           MOVE "ABC" TO GA
           MOVE B"11" TO GB
           MOVE N"QQ" TO GW
           MOVE "R" TO GT (1) GT (2) GT (3).
       SHOW.
           MOVE "??" TO W-W
           IF GW = N"QQ" MOVE "QQ" TO W-W END-IF
           IF GW = SPACES MOVE "SP" TO W-W END-IF
           DISPLAY "[" IG (1:1) "][" GX "][" GN "][" GL "][" GK "]["
               GE "][" GA "][" GB "][" GT (1) GT (2) GT (3) "] "
               W-W.
