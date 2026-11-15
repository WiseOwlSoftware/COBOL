      *>--- conformance-case
      *> id: g2_class_zero_length
      *> rules: [2002-GR-8.8.4.1.3.3-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.8.4.1.3.3 GR1 - every class phrase over a 2002
      *> zero-length item is false without NOT:
      *>   "If the data item referenced by identifier-1 is a
      *>   zero-length item, the truth value of the class condition
      *>   without the word NOT is false."     cite.py: OK GR1
      *> The 2002 zero-length items exercised (§2002:8.5.4, cite.py
      *> OK): item 1 "A group data item containing only a
      *> variable-occurrence data item in which the number of
      *> occurrences is zero" (G, N = 0); item 2 "A group data item
      *> containing only a subordinate zero-length item" (H over HG);
      *> item 3 "A data item defined with the ANY LENGTH clause
      *> corresponding to an argument or returning item that is a
      *> zero-length item" (LK in the contained G2CLZS; §2002:13.16.2.3
      *> GR1a, cite.py OK). G is passed BY CONTENT, so the argument
      *> has its current length: "For a variable-occurrence data item
      *> passed by content, the length of the argument is determined
      *> by the rules of the OCCURS clause for a sending data item"
      *> (§2002:14.7.1.1, cite.py OK); BY REFERENCE it would have the
      *> maximum length 5 and not be zero-length (kb/Work PB5621).
      *> No CHARACTER CLASSIFICATION clause, so no locale is in effect
      *> for classification (§2002:12.2.5.3 GR6, cite.py OK).
      *> DERIVATION:
      *>   Z-* (G, N = 0): NUMERIC, ALPHABETIC, ALPHABETIC-LOWER,
      *>     ALPHABETIC-UPPER, BOOLEAN, alphabet-name, class-name are
      *>     each F by GR1; IS NOT ALPHABETIC is T by GR1 + GR3.
      *>   H-*: H is zero-length (item 2): ALPHA=F, NUM=F.
      *>   S0-* (ANY LENGTH LK bound to G at N = 0): ALPHA=F,
      *>     NOTNUM=T.
      *>   N = 2, G = "AB": not zero-length, so GR2 b) 2 decides:
      *>     upper-case letters only -> NZ-ALPHA=T; GR2 a) 2: not
      *>     digits -> NZ-NUM=F; S2-ALPHA=T through LK (length 2).
      *> Each F leg fails if the empty content is read as vacuously
      *> "consisting entirely of" the class's characters (true).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CLZ01.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET G2STD IS STANDARD-1
           CLASS G2LET IS "A" THRU "Z".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N PIC 9 VALUE 0.
       01  G.
           05  E PIC X OCCURS 0 TO 5 TIMES DEPENDING ON N.
       01  H.
           05  HG.
               10  HE PIC X OCCURS 0 TO 5 TIMES DEPENDING ON N.
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF G IS NUMERIC
               DISPLAY "Z-NUM=T" ELSE DISPLAY "Z-NUM=F" END-IF
           IF G IS ALPHABETIC
               DISPLAY "Z-ALPHA=T" ELSE DISPLAY "Z-ALPHA=F" END-IF
           IF G IS ALPHABETIC-LOWER
               DISPLAY "Z-LOWER=T" ELSE DISPLAY "Z-LOWER=F" END-IF
           IF G IS ALPHABETIC-UPPER
               DISPLAY "Z-UPPER=T" ELSE DISPLAY "Z-UPPER=F" END-IF
           IF G IS BOOLEAN
               DISPLAY "Z-BOOL=T" ELSE DISPLAY "Z-BOOL=F" END-IF
           IF G IS G2STD
               DISPLAY "Z-ALPHABET=T" ELSE DISPLAY "Z-ALPHABET=F"
           END-IF
           IF G IS G2LET
               DISPLAY "Z-CLASS=T" ELSE DISPLAY "Z-CLASS=F" END-IF
           IF G IS NOT ALPHABETIC
               DISPLAY "Z-NOTALPHA=T" ELSE DISPLAY "Z-NOTALPHA=F"
           END-IF
           IF H IS ALPHABETIC
               DISPLAY "H-ALPHA=T" ELSE DISPLAY "H-ALPHA=F" END-IF
           IF H IS NUMERIC
               DISPLAY "H-NUM=T" ELSE DISPLAY "H-NUM=F" END-IF
           CALL "G2CLZS" USING BY CONTENT G
           MOVE 2 TO N
           MOVE "AB" TO G
           IF G IS ALPHABETIC
               DISPLAY "NZ-ALPHA=T" ELSE DISPLAY "NZ-ALPHA=F" END-IF
           IF G IS NUMERIC
               DISPLAY "NZ-NUM=T" ELSE DISPLAY "NZ-NUM=F" END-IF
           CALL "G2CLZS" USING BY CONTENT G
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CLZS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  CALLS PIC 9 VALUE 0.
       LINKAGE SECTION.
       01  LK PIC X ANY LENGTH.
       PROCEDURE DIVISION USING LK.
       S-PARA.
           ADD 1 TO CALLS
           IF CALLS = 1
               IF LK IS ALPHABETIC
                   DISPLAY "S0-ALPHA=T" ELSE DISPLAY "S0-ALPHA=F"
               END-IF
               IF LK IS NOT NUMERIC
                   DISPLAY "S0-NOTNUM=T" ELSE DISPLAY "S0-NOTNUM=F"
               END-IF
           ELSE
               IF LK IS ALPHABETIC
                   DISPLAY "S2-ALPHA=T" ELSE DISPLAY "S2-ALPHA=F"
               END-IF
           END-IF
           GOBACK.
       END PROGRAM G2CLZS.
       END PROGRAM G2CLZ01.
