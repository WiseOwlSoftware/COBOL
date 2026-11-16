      *>--- conformance-case
      *> id: g2obj01_object_computer_contained
      *> rules: [2002-GR-12.2.5.3-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:12.2.5.3 GR1 - "All clauses of the OBJECT-COMPUTER
      *> paragraph apply to the source unit in which they are
      *> explicitly or implicitly specified and to any source unit
      *> contained within that source unit."
      *>   cite.py --check "§2002:12.2.5.3 GR1" "All clauses of the
      *>   OBJECT-COMPUTER paragraph apply to the source unit in which
      *>   they are explicitly or implicitly specified and to any
      *>   source unit contained within that source unit" -> OK
      *> G2OBJ01 specifies both optional clauses: PROGRAM COLLATING
      *> SEQUENCE IS REV, ALPHABET REV IS "B" "A", and CHARACTER
      *> CLASSIFICATION IS TR, LOCALE TR IS "tr-TR". G2OBJ1A is
      *> contained in it and G2OBJ1B in G2OBJ1A; neither may have a
      *> configuration section (§2002:12.2.2 SR1), so each has the
      *> clauses only through GR1. Each unit runs the same three legs.
      *> REL - "A" > "B" under REV: §2002:12.2.6.3 GR7k puts "B" at
      *>   position 1 and "A" at 2 ("in ascending sequence"), every
      *>   other character after them; §2002:12.2.5.3 GR11 a) uses
      *>   the program collating sequence for the relation, so the
      *>   condition is true (native order would make it false).
      *> 88  - C = "B" against 88 VALUE "A" THRU "C": under REV "B"
      *>   (1) is below "A" (2), so outside the range (GR11 b); the
      *>   condition is false (native order makes it true).
      *> UP  - FUNCTION UPPER-CASE("i") under the tr-TR LC_CTYPE
      *>   (§2002:12.2.5.3 GR7 a) is the dotted capital I, not "I";
      *>   with no classification in effect it would be "I".
      *> Derived output:
      *>   L0-REL=REV L0-88=REV L0-UP=TR  (G2OBJ01, own clauses)
      *>   L1-REL=REV L1-88=REV L1-UP=TR   (directly contained, GR1)
      *>   L2-REL=REV L2-88=REV L2-UP=TR   (indirectly contained, GR1)
      *> one value per line. A compiler that applied either clause to
      *> its own unit only would print NAT on the L1/L2 lines.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2OBJ01.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. X
           CHARACTER CLASSIFICATION IS TR
           PROGRAM COLLATING SEQUENCE IS REV.
       SPECIAL-NAMES.
           ALPHABET REV IS "B" "A"
           LOCALE TR IS "tr-TR".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X VALUE "A".
       01 C PIC X VALUE "B".
          88 IN-AC VALUE "A" THRU "C".
       01 S PIC X(4).
       PROCEDURE DIVISION.
           IF X > "B" DISPLAY "L0-REL=REV"
           ELSE DISPLAY "L0-REL=NAT".
           IF IN-AC DISPLAY "L0-88=NAT"
           ELSE DISPLAY "L0-88=REV".
           MOVE FUNCTION UPPER-CASE("i") TO S
           IF S(1:1) = "I" DISPLAY "L0-UP=NAT"
           ELSE DISPLAY "L0-UP=TR".
           CALL "G2OBJ1A".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2OBJ1A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X VALUE "A".
       01 C PIC X VALUE "B".
          88 IN-AC VALUE "A" THRU "C".
       01 S PIC X(4).
       PROCEDURE DIVISION.
           IF X > "B" DISPLAY "L1-REL=REV"
           ELSE DISPLAY "L1-REL=NAT".
           IF IN-AC DISPLAY "L1-88=NAT"
           ELSE DISPLAY "L1-88=REV".
           MOVE FUNCTION UPPER-CASE("i") TO S
           IF S(1:1) = "I" DISPLAY "L1-UP=NAT"
           ELSE DISPLAY "L1-UP=TR".
           CALL "G2OBJ1B".
           GOBACK.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2OBJ1B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X VALUE "A".
       01 C PIC X VALUE "B".
          88 IN-AC VALUE "A" THRU "C".
       01 S PIC X(4).
       PROCEDURE DIVISION.
           IF X > "B" DISPLAY "L2-REL=REV"
           ELSE DISPLAY "L2-REL=NAT".
           IF IN-AC DISPLAY "L2-88=NAT"
           ELSE DISPLAY "L2-88=REV".
           MOVE FUNCTION UPPER-CASE("i") TO S
           IF S(1:1) = "I" DISPLAY "L2-UP=NAT"
           ELSE DISPLAY "L2-UP=TR".
           GOBACK.
       END PROGRAM G2OBJ1B.
       END PROGRAM G2OBJ1A.
       END PROGRAM G2OBJ01.
