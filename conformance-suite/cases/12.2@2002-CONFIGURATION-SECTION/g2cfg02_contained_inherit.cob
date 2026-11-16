      *>--- conformance-case
      *> id: g2cfg02_contained_inherit
      *> rules: [2002-GR-12.2.3-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:12.2.3 GR1 - "The entries explicitly or implicitly
      *> specified in the configuration section of a source unit that
      *> contains other source units apply to each directly or
      *> indirectly contained source unit."  cite.py --check -> OK
      *> §2002:12.2.2 SR1 - "The configuration section shall not be
      *> specified in a program that is contained within another
      *> program" (cite.py OK): inheritance is the only route of these
      *> entries into G2CFG02I (directly contained) and G2CFG02J
      *> (indirectly contained).
      *> THE 2002 REACH: every paragraph of the container - SOURCE-
      *> COMPUTER WITH DEBUGGING MODE (a 2002-only entry;
      *> §2002:12.2.4.3 GR1 and GR4, cite.py OK), OBJECT-COMPUTER
      *> PROGRAM COLLATING SEQUENCE, SPECIAL-NAMES ALPHABET and
      *> DECIMAL-POINT IS COMMA, REPOSITORY FUNCTION ALL INTRINSIC.
      *> DERIVATION OF THE EXPECTED OUTPUT:
      *>   OUTER, OUTER DEBUG - the container's own debugging line is
      *>   compiled (§2002:12.2.4.3 GR4).
      *>   I1/J1 DP 2,5 / 3,5 - inherited DECIMAL-POINT IS COMMA
      *>   (§2002:12.2.6.3 GR14a/b).
      *>   I1/J1 PCS REV - inherited collating sequence REV, "B" before
      *>   "A" (§2002:12.2.5.3 GR9; §2002:12.2.6.3 GR7k2).
      *>   I1/J1 FN IN / JN - inherited FUNCTION ALL INTRINSIC
      *>   (§2002:12.2.7.3 GR13).
      *>   I1 DEBUG, J1 DEBUG, J1 FLOAT DEBUG - the inherited DEBUGGING
      *>   MODE compiles the contained units' fixed D and floating >>D
      *>   debugging lines (§2002:6.1.2: >>D is "a debugging line when
      *>   followed by at least one space character", cite.py OK).
      *> EACH LEG CAN FAIL: an entry not inherited prints 2.5/3.5, PCS
      *> NATIVE, refuses UPPER-CASE, or drops a DEBUG line; an
      *> inheritance that stops at the direct containee fails only the
      *> J1 lines.
      *> The obsolete-element warnings COBOL0903 (debugging lines,
      *> DEBUGGING MODE) do not fail the case.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CFG02.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. G2-COMPUTER WITH DEBUGGING MODE.
       OBJECT-COMPUTER. G2-COMPUTER
           PROGRAM COLLATING SEQUENCE IS REV.
       SPECIAL-NAMES.
           ALPHABET REV IS "B" "A"
           DECIMAL-POINT IS COMMA.
       REPOSITORY.
           FUNCTION ALL INTRINSIC.
       PROCEDURE DIVISION.
           DISPLAY "OUTER"
      D    DISPLAY "OUTER DEBUG"
           CALL "G2CFG02I"
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CFG02I.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N  PIC 9V9 VALUE 2,5.
       01 ED PIC 9,9.
       01 XA PIC X VALUE "A".
       PROCEDURE DIVISION.
           MOVE N TO ED
           DISPLAY "I1 DP " ED
           IF XA > "B"
               DISPLAY "I1 PCS REV"
           ELSE
               DISPLAY "I1 PCS NATIVE"
           END-IF
           DISPLAY "I1 FN " UPPER-CASE("in")
      D    DISPLAY "I1 DEBUG"
           CALL "G2CFG02J"
           GOBACK.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CFG02J.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N  PIC 9V9 VALUE 3,5.
       01 ED PIC 9,9.
       01 XA PIC X VALUE "A".
       PROCEDURE DIVISION.
           MOVE N TO ED
           DISPLAY "J1 DP " ED
           IF XA > "B"
               DISPLAY "J1 PCS REV"
           ELSE
               DISPLAY "J1 PCS NATIVE"
           END-IF
           DISPLAY "J1 FN " UPPER-CASE("jn")
      D    DISPLAY "J1 DEBUG"
           >>D DISPLAY "J1 FLOAT DEBUG"
           GOBACK.
       END PROGRAM G2CFG02J.
       END PROGRAM G2CFG02I.
       END PROGRAM G2CFG02.
