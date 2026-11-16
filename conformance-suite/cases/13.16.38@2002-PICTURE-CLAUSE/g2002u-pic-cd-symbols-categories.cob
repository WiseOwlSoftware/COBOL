      *>--- conformance-case
      *> id: g2002u-pic-cd-symbols-categories
      *> rules: [2002-SR-13.16.38.2-7, 2002-GR-13.16.38.3-7, 2002-GR-13.16.38.3-10, 2002-GR-13.16.38.3-11, 2002-GR-13.16.38.3-13, 2002-GR-13.16.38.4-5]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> Every item under test sits in a record description entry
      *> written after an OUTPUT CD entry, which implicitly redefines
      *> the CD's record area (§2002:A.1.2.2 SR9), so the PICTURE
      *> rules of §2002:13.16.38 reach it. The CD facility draws the
      *> obsolete warning COBOL0903, which does not fail the case.
      *> Numeric items are never DISPLAYed (DISPLAY's conversion of a
      *> numeric item is the implementor's): they are shown through an
      *> edited item, a comparison, or FUNCTION LENGTH into WLEN.
      *> REP §2002:13.16.38.2 SR7 - A(3) and X(K), 9(K)V99 with the
      *>     constant-name K = 4 give sizes 3, 4, 6. RLONG writes the
      *>     30-character constant-name in a 52-character string: the
      *>     integer's length, not the name's, counts toward the 50
      *>     limit (SR4), so it compiles, size 4 + 19 = 23. Counting
      *>     the name refuses the program; a wrong repeat count fails
      *>     the LEN line.
      *> AE  §2002:13.16.38.3 GR7 - XXBXX and AA/A0A are
      *>     alphanumeric-edited: "ABCD" edits to "AB CD" and
      *>     "AB/C0D" (simple insertion, Table 8).
      *> NE  §2002:13.16.38.3 GR10 - NNBNN/N0N is national-edited:
      *>     N"ABCDEF" edits to N"AB CD/E0F", 9 national positions.
      *> NUM §2002:13.16.38.3 GR11 - S99, S99 LEADING SEPARATE, 99V99,
      *>     9PP, VPP9 are numeric: arithmetic on them works.
      *> SIZ §2002:13.16.38.3 GR13 - sizes: S counted only with
      *>     SEPARATE (2, 3), V and P not counted (4, 1, 1), CR and
      *>     DB two each (5), E counted (9), the first currency
      *>     symbol adds the currency string's 3 characters and each
      *>     later one adds 1 (U999 = 6, UU99 = 6); P's algebraic
      *>     value in a comparison (300, .001), an elementary move
      *>     ("  300") and an arithmetic sender (0.001); B 0 / , . Z *
      *>     E insertion and replacement in the edited values.
      *> FIX §2002:13.16.38.4 GR5 - fixed insertion of + - CR DB and
      *>     the currency string at the symbol's own position, with
      *>     Table 9's results for positive, negative and zero values.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2PU01.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CURRENCY SIGN IS "EUR" WITH PICTURE SYMBOL "U".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  K CONSTANT AS 4.
       01  SR7-REPEAT-COUNT-NAME-30-CHARS CONSTANT AS 4.
       01  WLEN  PIC 99.
       01  WSUM  PIC S9V999 VALUE 0.
       01  WE2   PIC -99.
       01  WE4   PIC 99.99.
       01  WE5   PIC 9.999.
       01  WZ5   PIC ZZZZ9.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS CDDC
           TEXT LENGTH IS CDTL
           STATUS KEY IS CDSK
           ERROR KEY IS CDEK
           SYMBOLIC DESTINATION IS CDSD.
       01  CDREC.
           05 RA   PIC A(3).
           05 RK   PIC X(K).
           05 RKN  PIC 9(K)V99.
           05 RLONG
               PIC X(SR7-REPEAT-COUNT-NAME-30-CHARS)XXXXXXXXXXXXXXXXXXX
               .
           05 AE1  PIC XXBXX.
           05 AE2  PIC AA/A0A.
           05 NE1  PIC NNBNN/N0N USAGE NATIONAL.
           05 NS1  PIC S99.
           05 NS2  PIC S99 SIGN LEADING SEPARATE.
           05 NV   PIC 99V99.
           05 NP1  PIC 9PP.
           05 NP2  PIC VPP9.
           05 SBO  PIC 1(4).
           05 SB   PIC 99B99.
           05 SZR  PIC 9900.
           05 SSL  PIC 99/99.
           05 SZ   PIC ZZ,ZZ9.99.
           05 SST  PIC **9.
           05 SFE  PIC +9.99E+99.
           05 SC2  PIC UU99.
           05 FP1  PIC +999.
           05 FM1  PIC -999.
           05 FP2  PIC 999+.
           05 FM2  PIC 999-.
           05 FCR  PIC 999CR.
           05 FDB  PIC 999DB.
           05 FC1  PIC U999.
           05 FC2  PIC 999U.
       PROCEDURE DIVISION.
           MOVE FUNCTION LENGTH(RA) TO WLEN
           DISPLAY "REP RA=" WLEN WITH NO ADVANCING
           MOVE FUNCTION LENGTH(RK) TO WLEN
           DISPLAY " RK=" WLEN WITH NO ADVANCING
           MOVE FUNCTION LENGTH(RKN) TO WLEN
           DISPLAY " RKN=" WLEN WITH NO ADVANCING
           MOVE FUNCTION LENGTH(RLONG) TO WLEN
           DISPLAY " RLONG=" WLEN
           MOVE "XYZ" TO RA
           MOVE "ABCD" TO RK
           DISPLAY "REP [" RA "][" RK "]"
           MOVE "ABCD" TO AE1 AE2
           DISPLAY "AE [" AE1 "][" AE2 "]"
           MOVE N"ABCDEF" TO NE1
           MOVE FUNCTION LENGTH(NE1) TO WLEN
           IF NE1 = N"AB CD/E0F"
               DISPLAY "NE EDITED OK LEN=" WLEN
           ELSE
               DISPLAY "NE BAD LEN=" WLEN
           END-IF
           MOVE -7 TO NS1
           ADD 3 TO NS1
           MOVE NS1 TO WE2
           DISPLAY "NUM NS1=" WE2 WITH NO ADVANCING
           MOVE -7 TO NS2
           MOVE NS2 TO WE2
           DISPLAY " NS2=" WE2 WITH NO ADVANCING
           MOVE 12.34 TO NV
           ADD 1 TO NV
           MOVE NV TO WE4
           DISPLAY " NV=" WE4
           MOVE 12345 TO NP1
           IF NP1 = 300
               DISPLAY "NP1 ALGEBRAIC 300 OK"
           ELSE
               DISPLAY "NP1 BAD"
           END-IF
           MOVE NP1 TO WZ5
           DISPLAY "NP1 MOVE [" WZ5 "]"
           MOVE .00123 TO NP2
           IF NP2 = .001
               DISPLAY "NP2 ALGEBRAIC .001 OK"
           ELSE
               DISPLAY "NP2 BAD"
           END-IF
           ADD NP2 TO WSUM
           MOVE WSUM TO WE5
           DISPLAY "NP2 ADD " WE5
           MOVE FUNCTION LENGTH(NS1) TO WLEN
           DISPLAY "SIZ NS1=" WLEN WITH NO ADVANCING
           MOVE FUNCTION LENGTH(NS2) TO WLEN
           DISPLAY " NS2=" WLEN WITH NO ADVANCING
           MOVE FUNCTION LENGTH(NV) TO WLEN
           DISPLAY " NV=" WLEN WITH NO ADVANCING
           MOVE FUNCTION LENGTH(NP1) TO WLEN
           DISPLAY " NP1=" WLEN WITH NO ADVANCING
           MOVE FUNCTION LENGTH(NP2) TO WLEN
           DISPLAY " NP2=" WLEN
           MOVE FUNCTION LENGTH(SBO) TO WLEN
           DISPLAY "SIZ SBO=" WLEN WITH NO ADVANCING
           MOVE FUNCTION LENGTH(SB) TO WLEN
           DISPLAY " SB=" WLEN WITH NO ADVANCING
           MOVE FUNCTION LENGTH(SZR) TO WLEN
           DISPLAY " SZR=" WLEN WITH NO ADVANCING
           MOVE FUNCTION LENGTH(SSL) TO WLEN
           DISPLAY " SSL=" WLEN WITH NO ADVANCING
           MOVE FUNCTION LENGTH(SZ) TO WLEN
           DISPLAY " SZ=" WLEN WITH NO ADVANCING
           MOVE FUNCTION LENGTH(SST) TO WLEN
           DISPLAY " SST=" WLEN
           MOVE FUNCTION LENGTH(SFE) TO WLEN
           DISPLAY "SIZ SFE=" WLEN WITH NO ADVANCING
           MOVE FUNCTION LENGTH(FP1) TO WLEN
           DISPLAY " FP1=" WLEN WITH NO ADVANCING
           MOVE FUNCTION LENGTH(FCR) TO WLEN
           DISPLAY " FCR=" WLEN WITH NO ADVANCING
           MOVE FUNCTION LENGTH(FDB) TO WLEN
           DISPLAY " FDB=" WLEN WITH NO ADVANCING
           MOVE FUNCTION LENGTH(FC1) TO WLEN
           DISPLAY " FC1=" WLEN WITH NO ADVANCING
           MOVE FUNCTION LENGTH(SC2) TO WLEN
           DISPLAY " SC2=" WLEN
           MOVE B"1010" TO SBO
           IF SBO = B"1010"
               DISPLAY "SBO 1010 OK"
           ELSE
               DISPLAY "SBO BAD"
           END-IF
           MOVE 1234 TO SB SZR SSL
           DISPLAY "INS [" SB "][" SZR "][" SSL "]"
           MOVE 1234.5 TO SZ
           MOVE 5 TO SST
           MOVE 0 TO SFE
           DISPLAY "INS [" SZ "][" SST "][" SFE "]"
           MOVE 12 TO FP1 FM1 FP2 FM2 FCR FDB FC1 FC2
           DISPLAY "POS [" FP1 "][" FM1 "][" FP2 "][" FM2 "]["
               FCR "][" FDB "][" FC1 "][" FC2 "]"
           MOVE -12 TO FP1 FM1 FP2 FM2 FCR FDB FC1 FC2
           DISPLAY "NEG [" FP1 "][" FM1 "][" FP2 "][" FM2 "]["
               FCR "][" FDB "][" FC1 "][" FC2 "]"
           MOVE 0 TO FP1 FM1 FP2 FM2 FCR FDB FC1 FC2
           DISPLAY "ZER [" FP1 "][" FM1 "][" FP2 "][" FM2 "]["
               FCR "][" FDB "][" FC1 "][" FC2 "]"
           STOP RUN.
