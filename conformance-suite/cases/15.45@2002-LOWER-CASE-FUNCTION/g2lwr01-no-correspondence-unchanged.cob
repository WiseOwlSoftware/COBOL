      *>--- conformance-case
      *> id: g2lwr01-no-correspondence-unchanged
      *> rules: [2002-RV-15.45.3-4]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.45.3 RV4 - "If there is no corresponding lowercase letter
      *>   for a given uppercase letter, that letter is unchanged in the
      *>   returned value; when a locale is in effect for character
      *>   classification and there is no lowercase correspondence
      *>   specified in the locale, the letter or letters are unchanged in
      *>   the returned value."
      *> NO LOCALE (G2LWR01): no CHARACTER CLASSIFICATION clause, so the
      *>   classification is the coded character set's (§2002:12.2.5.3
      *>   GR6). 2002 does not state that correspondence; WiseOwl's is the
      *>   Unicode simple case mapping (docs/conformance/3/D-CASE-MAPPING.md;
      *>   kb/Work PB5625 reading D). U+2102 (double-struck C) and U+03D2
      *>   (upsilon with hook symbol) are uppercase letters with no simple
      *>   lowercase mapping: unchanged, ORD 8451 and 979 (ORD is the code
      *>   point plus one). X has one: x, ORD 121 (the control that shows
      *>   the function did map). RV3: one-to-one, so the length stays 3.
      *> LOCALE (G2LWR01B): CHARACTER CLASSIFICATION IS TR TR puts tr-TR's
      *>   LC_CTYPE in effect for alphanumeric and national characters
      *>   (§2002:12.2.5.3 GR5a, GR5f; §2002:15.45.3 RV2). tr-TR specifies
      *>   no lowercase for U+2102 or U+03D2: unchanged, ORD 8451 and 979;
      *>   it maps I to dotless i U+0131, ORD 306, which shows the locale
      *>   is the one in effect (the coded character set gives i, ORD 106).
      *> A function that dropped, replaced or mis-mapped an unpaired letter
      *>   prints another ordinal.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2LWR01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  NU              PIC N(3) VALUE N"ℂϒX".
       01  NL              PIC N(3).
       01  K               PIC 9.
       01  O               PIC 9(5).
       01  L               PIC 9.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE FUNCTION LOWER-CASE(NU) TO NL
           PERFORM VARYING K FROM 1 BY 1 UNTIL K > 3
               MOVE FUNCTION ORD(NL(K:1)) TO O
               DISPLAY "NONE-" K "=" O
           END-PERFORM
           MOVE FUNCTION LENGTH(FUNCTION LOWER-CASE(NU)) TO L
           DISPLAY "NONE-LEN=" L
           CALL "G2LWR01B"
           STOP RUN.
       END PROGRAM G2LWR01.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2LWR01B.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. X CHARACTER CLASSIFICATION IS TR TR.
       SPECIAL-NAMES.
           LOCALE TR IS "tr-TR".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  NU              PIC N(3) VALUE N"ℂϒI".
       01  NL              PIC N(3).
       01  K               PIC 9.
       01  O               PIC 9(5).
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE FUNCTION LOWER-CASE(NU) TO NL
           PERFORM VARYING K FROM 1 BY 1 UNTIL K > 3
               MOVE FUNCTION ORD(NL(K:1)) TO O
               DISPLAY "TR-" K "=" O
           END-PERFORM
           GOBACK.
       END PROGRAM G2LWR01B.
