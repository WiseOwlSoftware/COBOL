      *>--- conformance-case
      *> id: g2obj02_classification_cd_record
      *> rules: [2002-GR-12.2.5.3-7]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:12.2.5.3 GR7 - "When the CHARACTER CLASSIFICATION clause
      *> is specified, the cultural convention specification LC_CTYPE
      *> from the specified locales are used for: a) the uppercase and
      *> lowercase mappings of characters for the UPPER-CASE and
      *> LOWER-CASE intrinsic functions. b) the classification of
      *> characters for class tests ALPHABETIC, ALPHABETIC-LOWER,
      *> ALPHABETIC-UPPER, ..."
      *>   cite.py --check "§2002:12.2.5.3 GR7" "the uppercase and
      *>   lowercase mappings of characters for the UPPER-CASE and
      *>   LOWER-CASE intrinsic functions" -> OK
      *>   cite.py --check "§2002:12.2.5.3 GR7" "the classification of
      *>   characters for class tests ALPHABETIC, ALPHABETIC-LOWER,
      *>   ALPHABETIC-UPPER" -> OK
      *> At 2002 the operands include the communication section: a CD
      *> field (IN-Q, the SYMBOLIC QUEUE) and the record under the CD
      *> (IN-K). The same legs run on a working-storage item (W-K).
      *> The classification is tr-TR, where the case pairs are
      *> i / dotted capital I and dotless small i / I.
      *> Legs (CD- on the CD entry, WS- on working storage):
      *>  UP    UPPER-CASE("i") is the dotted capital I, not "I" (a)
      *>  LO    LOWER-CASE("I") is the dotless small i, not "i" (a)
      *>  ALPHA dotless i IS ALPHABETIC: true, it is alphabetic in the
      *>        LC_CTYPE (§2002:8.8.4.1.3.3 GR2 b1); without a locale
      *>        only A-Z, a-z and space qualify (b2), so false
      *>  LOWER dotless i IS ALPHABETIC-LOWER: true (c1; c2 false)
      *>  UPPER dotted capital I IS ALPHABETIC-UPPER: true (d1; d2
      *>        false)
      *> Derived output: CD-UP=TR CD-LO=TR CD-ALPHA=yes CD-LOWER=yes
      *> CD-UPPER=yes WS-UP=TR WS-LO=TR WS-ALPHA=yes WS-LOWER=yes
      *> WS-UPPER=yes, one per line. Each leg prints NAT or no when no
      *> classification is in effect. The record under an input CD
      *> shares the CD area (IN-K overlays the first character of
      *> IN-Q), so every leg moves its own value before it tests it.
      *> The communication facility is obsolete at 2002 (COBOL0903
      *> warning). Non-ASCII appears only inside literals.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2OBJ02.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. X CHARACTER CLASSIFICATION IS TR.
       SPECIAL-NAMES.
           LOCALE TR IS "tr-TR".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  S               PIC X(12).
       01  W-K             PIC X.
       COMMUNICATION SECTION.
       CD  IN-CD FOR INPUT
           SYMBOLIC QUEUE IS IN-Q
           MESSAGE COUNT IS IN-CNT.
       01  IN-REC.
           05 IN-K         PIC X.
           05 FILLER       PIC X(86).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "i" TO IN-Q
           MOVE FUNCTION UPPER-CASE(IN-Q) TO S
           IF S(1:1) = "I" DISPLAY "CD-UP=NAT"
           ELSE DISPLAY "CD-UP=TR" END-IF
           MOVE "I" TO IN-K
           MOVE FUNCTION LOWER-CASE(IN-K) TO S
           IF S(1:1) = "i" DISPLAY "CD-LO=NAT"
           ELSE DISPLAY "CD-LO=TR" END-IF
           MOVE "ı" TO IN-K
           IF IN-K IS ALPHABETIC DISPLAY "CD-ALPHA=yes"
           ELSE DISPLAY "CD-ALPHA=no" END-IF
           IF IN-K IS ALPHABETIC-LOWER DISPLAY "CD-LOWER=yes"
           ELSE DISPLAY "CD-LOWER=no" END-IF
           MOVE "İ" TO IN-K
           IF IN-K IS ALPHABETIC-UPPER DISPLAY "CD-UPPER=yes"
           ELSE DISPLAY "CD-UPPER=no" END-IF
           MOVE "i" TO W-K
           MOVE FUNCTION UPPER-CASE(W-K) TO S
           IF S(1:1) = "I" DISPLAY "WS-UP=NAT"
           ELSE DISPLAY "WS-UP=TR" END-IF
           MOVE "I" TO W-K
           MOVE FUNCTION LOWER-CASE(W-K) TO S
           IF S(1:1) = "i" DISPLAY "WS-LO=NAT"
           ELSE DISPLAY "WS-LO=TR" END-IF
           MOVE "ı" TO W-K
           IF W-K IS ALPHABETIC DISPLAY "WS-ALPHA=yes"
           ELSE DISPLAY "WS-ALPHA=no" END-IF
           IF W-K IS ALPHABETIC-LOWER DISPLAY "WS-LOWER=yes"
           ELSE DISPLAY "WS-LOWER=no" END-IF
           MOVE "İ" TO W-K
           IF W-K IS ALPHABETIC-UPPER DISPLAY "WS-UPPER=yes"
           ELSE DISPLAY "WS-UPPER=no" END-IF
           STOP RUN.
