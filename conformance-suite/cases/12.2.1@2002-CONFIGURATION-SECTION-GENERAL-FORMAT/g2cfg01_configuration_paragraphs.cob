      *>--- conformance-case
      *> id: g2cfg01_configuration_paragraphs
      *> rules: [2002-FMT-12.2.1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:12.2.1 - CONFIGURATION SECTION. "[ source-computer-
      *> paragraph ] [ object-computer-paragraph ] [ special-names-
      *> paragraph ] [ repository-paragraph ]": four optional
      *> paragraphs in that order.  cite.py --check -> OK
      *> THE ACCEPTED FORMS, one per outermost program (§2002:12.2.2
      *> SR1 forbids a configuration section in a contained program, so
      *> each is a separate outermost program):
      *>   G2CFG03 - all four paragraphs in the printed order, each
      *>   taking effect;
      *>   G2CFG01B - CONFIGURATION SECTION with no paragraph (all
      *>   brackets omitted);
      *>   G2CFG01C - SOURCE-COMPUTER then REPOSITORY (the middle two
      *>   omitted).
      *> DERIVATION OF THE EXPECTED OUTPUT:
      *>   A1: §2002:12.2.6.3 GR14a/b (cite.py OK): with DECIMAL-POINT
      *>   IS COMMA, VALUE 1,5 is one and five tenths and PIC 9,9
      *>   inserts a comma: "A1 1,5".
      *>   A2: §2002:12.2.5.3 GR9 (cite.py OK) the program collating
      *>   sequence is alphabet REV; §2002:12.2.6.3 GR7k.2 (cite.py OK)
      *>   the literals give ascending order, so "B" < "A" and "A" >
      *>   "B" is true: "A2 REV".
      *>   A3: §2002:12.2.7.3 GR13/GR14 (cite.py OK): FUNCTION ALL
      *>   INTRINSIC lets UPPER-CASE be written without FUNCTION: "A3
      *>   OK".
      *>   B1: G2CFG01B has no SPECIAL-NAMES, so the period is the
      *>   decimal point: "B1 1.5".
      *>   C1: REPOSITORY after SOURCE-COMPUTER alone: "C1 OK".
      *> EACH LEG CAN FAIL: any form refused fails the compile; a
      *> paragraph accepted but ignored prints 1.5, A2 NATIVE or a
      *> refused UPPER-CASE reference; a DECIMAL-POINT leak from
      *> G2CFG03 prints B1 1,5.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CFG03.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. G2-COMPUTER.
       OBJECT-COMPUTER. G2-COMPUTER
           PROGRAM COLLATING SEQUENCE IS REV.
       SPECIAL-NAMES.
           ALPHABET REV IS "B" "A"
           DECIMAL-POINT IS COMMA.
       REPOSITORY.
           FUNCTION ALL INTRINSIC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N  PIC 9V9 VALUE 1,5.
       01 ED PIC 9,9.
       01 XA PIC X VALUE "A".
       PROCEDURE DIVISION.
           MOVE N TO ED
           DISPLAY "A1 " ED
           IF XA > "B"
               DISPLAY "A2 REV"
           ELSE
               DISPLAY "A2 NATIVE"
           END-IF
           DISPLAY "A3 " UPPER-CASE("ok")
           CALL "G2CFG01B"
           CALL "G2CFG01C"
           STOP RUN.
       END PROGRAM G2CFG03.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CFG01B.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N  PIC 9V9 VALUE 1.5.
       01 ED PIC 9.9.
       PROCEDURE DIVISION.
           MOVE N TO ED
           DISPLAY "B1 " ED
           GOBACK.
       END PROGRAM G2CFG01B.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CFG01C.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. G2-COMPUTER.
       REPOSITORY.
           FUNCTION ALL INTRINSIC.
       PROCEDURE DIVISION.
           DISPLAY "C1 " UPPER-CASE("ok")
           GOBACK.
       END PROGRAM G2CFG01C.
