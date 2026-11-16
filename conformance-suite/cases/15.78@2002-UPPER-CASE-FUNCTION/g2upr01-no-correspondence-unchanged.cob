      *>--- conformance-case
      *> id: g2upr01-no-correspondence-unchanged
      *> rules: [2002-RV-15.78.3-4]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.78.3 RV4 - "If there is no corresponding uppercase letter
      *>   for a given lowercase letter, that letter is unchanged in the
      *>   returned value; when a locale is in effect for character
      *>   classification and there is no uppercase correspondence
      *>   specified in the locale for a given letter or letters, the
      *>   letter or letters are unchanged in the returned value."
      *> NO LOCALE (G2UPR01): no CHARACTER CLASSIFICATION clause, so the
      *>   classification is the coded character set's (§2002:12.2.5.3
      *>   GR6). 2002 does not state that correspondence; WiseOwl's is the
      *>   Unicode simple case mapping (docs/conformance/3/D-CASE-MAPPING.md;
      *>   kb/Work PB5625 reading D). U+0138 (kra) and U+00DF (sharp s) are
      *>   lowercase letters with no simple uppercase mapping: unchanged,
      *>   ORD 313 and 224 (ORD is the code point plus one). x has one: X,
      *>   ORD 89 (the control that shows the function did map). RV3:
      *>   one-to-one, so the length stays 3.
      *> LOCALE (G2UPR01B): CHARACTER CLASSIFICATION IS TR TR puts tr-TR's
      *>   LC_CTYPE in effect for alphanumeric and national characters
      *>   (§2002:12.2.5.3 GR5a, GR5f; §2002:15.78.3 RV2). tr-TR specifies
      *>   no uppercase for U+0138 or U+00DF: unchanged, ORD 313 and 224;
      *>   it maps i to dotted capital I U+0130, ORD 305, which shows the
      *>   locale is the one in effect (the coded character set gives I,
      *>   ORD 74).
      *> A function that dropped, replaced or mis-mapped an unpaired letter
      *>   (for instance sharp s to "SS", a full rather than simple mapping,
      *>   or to U+1E9E) prints another ordinal.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2UPR01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  NU              PIC N(3) VALUE N"ĸßx".
       01  NL              PIC N(3).
       01  K               PIC 9.
       01  O               PIC 9(5).
       01  L               PIC 9.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE FUNCTION UPPER-CASE(NU) TO NL
           PERFORM VARYING K FROM 1 BY 1 UNTIL K > 3
               MOVE FUNCTION ORD(NL(K:1)) TO O
               DISPLAY "NONE-" K "=" O
           END-PERFORM
           MOVE FUNCTION LENGTH(FUNCTION UPPER-CASE(NU)) TO L
           DISPLAY "NONE-LEN=" L
           CALL "G2UPR01B"
           STOP RUN.
       END PROGRAM G2UPR01.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2UPR01B.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. X CHARACTER CLASSIFICATION IS TR TR.
       SPECIAL-NAMES.
           LOCALE TR IS "tr-TR".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  NU              PIC N(3) VALUE N"ĸßi".
       01  NL              PIC N(3).
       01  K               PIC 9.
       01  O               PIC 9(5).
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE FUNCTION UPPER-CASE(NU) TO NL
           PERFORM VARYING K FROM 1 BY 1 UNTIL K > 3
               MOVE FUNCTION ORD(NL(K:1)) TO O
               DISPLAY "TR-" K "=" O
           END-PERFORM
           GOBACK.
       END PROGRAM G2UPR01B.
