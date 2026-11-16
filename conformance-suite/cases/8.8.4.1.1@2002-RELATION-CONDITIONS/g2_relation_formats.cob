      *>--- conformance-case
      *> id: g2_relation_formats
      *> rules: [2002-FMT-8.8.4.1.1.1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.8.4.1.1.1 - the three relation condition formats.
      *> Format 1: every operator the 2002 format prints, IS [NOT]
      *> GREATER THAN, >, LESS THAN, <, EQUAL TO, =, and the extended
      *> GREATER/LESS THAN OR EQUAL TO, >=, <= (R01-R16); operands
      *> identifier, literal-1/literal-2, arithmetic-expression-1/-2
      *> (R17-R21) and index-name-1/-2 (R22-R27). Format 2 (boolean):
      *> boolean-expression IS [NOT] EQUAL TO | = (R28-R33). Format 3
      *> (pointer-or-object-reference): identifier-3 IS [NOT] EQUAL TO
      *> | = identifier-4 over data-pointers (R34-R37) and object
      *> references (R38-R41).
      *> cite.py OK: §2002:8.8.4.1.1.1 "Format 3
      *> (pointer-or-object-reference)"; §2002:8.7.5.2 SR1 "When
      *> specified, NOT creates one relational operator";
      *> §2002:8.8.4.1.1.3 "a comparison is made with respect to the
      *> algebraic value of the operands"; §2002:8.8.4.1.1.10 "The
      *> result is the same as if the corresponding occurrence numbers
      *> were compared" (item 2: index-name with a numeric data item or
      *> literal compares the occurrence number); §2002:8.8.4.1.1.12
      *> "The operands are equal if they reference the same address";
      *> §2002:8.8.4.1.1.11 "has a true value if the object referenced
      *> by identifier-3 is the same object that is referenced by
      *> identifier-4".
      *> A=5 B=7 D=5 K=3; I1 occurrence 3, I2 occurrence 4;
      *> BB=BC=B"1010", BD=B"0101"; P1, P2 = ADDRESS OF A, P3 =
      *> ADDRESS OF B; O1 and O2 the same new object, O3 another.
      *> Expected: R01 5>7 F, R02 T, R03 F, R04 T, R05 5<7 T, R06 F,
      *> R07 T, R08 F, R09 5=5 T, R10 F, R11 5=7 F, R12 T, R13 5>=5 T,
      *> R14 5>=7 F, R15 5<=5 T, R16 7<=5 F, R17 5=A T, R18 5<6 T,
      *> R19 7=7 T, R20 6>6 F, R21 14=14 T, R22 3=4 F, R23 3<4 T,
      *> R24 3=3 T, R25 3=K T, R26 4=4 T, R27 4 NOT>3 F,
      *> R28 1010=1010 T, R29 1010=0101 F, R30 T, R31 F,
      *> R32 1010 B-AND 1000 = 1000 T, R33 B-NOT 1010 = 0101 T,
      *> R34 same address T, R35 F, R36 T, R37 F, R38 same object T,
      *> R39 different objects F, R40 T, R41 F.
      *> A leg fails if an operator spelling is refused or mis-mapped,
      *> NOT does not reverse its one operator, an index-name is
      *> compared by displacement rather than occurrence number, or a
      *> pointer/object relation compares anything but identity.
       IDENTIFICATION DIVISION.
       CLASS-ID. G2RELK INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       END CLASS G2RELK.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2REL05.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2RELK.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A  PIC 99 VALUE 5.
       01  B  PIC 99 VALUE 7.
       01  D  PIC 99 VALUE 5.
       01  K  PIC 9 VALUE 3.
       01  TB.
           05  TE PIC X OCCURS 5 INDEXED BY I1 I2.
       01  BB PIC 1(4) VALUE B"1010".
       01  BC PIC 1(4) VALUE B"1010".
       01  BD PIC 1(4) VALUE B"0101".
       01  P1 USAGE POINTER.
       01  P2 USAGE POINTER.
       01  P3 USAGE POINTER.
       01  O1 USAGE OBJECT REFERENCE G2RELK.
       01  O2 USAGE OBJECT REFERENCE G2RELK.
       01  O3 USAGE OBJECT REFERENCE G2RELK.
       PROCEDURE DIVISION.
       MAIN-PARA.
      *> Format 1, every operator of the 2002 format; A=5 B=7 D=5.
           IF A IS GREATER THAN B DISPLAY "R01 T" ELSE DISPLAY "R01 F".
           IF A IS NOT GREATER THAN B DISPLAY "R02 T"
           ELSE DISPLAY "R02 F".
           IF A > B DISPLAY "R03 T" ELSE DISPLAY "R03 F".
           IF A NOT > B DISPLAY "R04 T" ELSE DISPLAY "R04 F".
           IF A IS LESS THAN B DISPLAY "R05 T" ELSE DISPLAY "R05 F".
           IF A IS NOT LESS THAN B DISPLAY "R06 T"
           ELSE DISPLAY "R06 F".
           IF A < B DISPLAY "R07 T" ELSE DISPLAY "R07 F".
           IF A IS NOT < B DISPLAY "R08 T" ELSE DISPLAY "R08 F".
           IF A IS EQUAL TO D DISPLAY "R09 T" ELSE DISPLAY "R09 F".
           IF A IS NOT EQUAL TO D DISPLAY "R10 T"
           ELSE DISPLAY "R10 F".
           IF A = B DISPLAY "R11 T" ELSE DISPLAY "R11 F".
           IF A NOT = B DISPLAY "R12 T" ELSE DISPLAY "R12 F".
           IF A IS GREATER THAN OR EQUAL TO D DISPLAY "R13 T"
           ELSE DISPLAY "R13 F".
           IF A >= B DISPLAY "R14 T" ELSE DISPLAY "R14 F".
           IF A IS LESS THAN OR EQUAL TO D DISPLAY "R15 T"
           ELSE DISPLAY "R15 F".
           IF B IS <= A DISPLAY "R16 T" ELSE DISPLAY "R16 F".
      *> Format 1 operand kinds: literal, arithmetic expression.
           IF 5 = A DISPLAY "R17 T" ELSE DISPLAY "R17 F".
           IF A < 6 DISPLAY "R18 T" ELSE DISPLAY "R18 F".
           IF A + 2 = B DISPLAY "R19 T" ELSE DISPLAY "R19 F".
           IF (B - A) * 3 > A + 1 DISPLAY "R20 T" ELSE DISPLAY "R20 F".
           IF 14 = B * 2 DISPLAY "R21 T" ELSE DISPLAY "R21 F".
      *> Format 1 index-name operands; I1 at occurrence 3, I2 at 4.
           SET I1 TO 3. SET I2 TO 4.
           IF I1 = I2 DISPLAY "R22 T" ELSE DISPLAY "R22 F".
           IF I1 < I2 DISPLAY "R23 T" ELSE DISPLAY "R23 F".
           IF I1 = 3 DISPLAY "R24 T" ELSE DISPLAY "R24 F".
           IF I1 = K DISPLAY "R25 T" ELSE DISPLAY "R25 F".
           IF 4 = I2 DISPLAY "R26 T" ELSE DISPLAY "R26 F".
           IF I2 NOT > K DISPLAY "R27 T" ELSE DISPLAY "R27 F".
      *> Format 2 (boolean).
           IF BB IS EQUAL TO BC DISPLAY "R28 T" ELSE DISPLAY "R28 F".
           IF BB = BD DISPLAY "R29 T" ELSE DISPLAY "R29 F".
           IF BB IS NOT EQUAL TO BD DISPLAY "R30 T"
           ELSE DISPLAY "R30 F".
           IF BB NOT = BC DISPLAY "R31 T" ELSE DISPLAY "R31 F".
           IF BB B-AND B"1000" = B"1000" DISPLAY "R32 T"
           ELSE DISPLAY "R32 F".
           IF B-NOT BB IS EQUAL TO BD DISPLAY "R33 T"
           ELSE DISPLAY "R33 F".
      *> Format 3 (pointer).
           SET P1 TO ADDRESS OF A.
           SET P2 TO ADDRESS OF A.
           SET P3 TO ADDRESS OF B.
           IF P1 = P2 DISPLAY "R34 T" ELSE DISPLAY "R34 F".
           IF P1 IS EQUAL TO P3 DISPLAY "R35 T" ELSE DISPLAY "R35 F".
           IF P1 IS NOT EQUAL TO P3 DISPLAY "R36 T"
           ELSE DISPLAY "R36 F".
           IF P1 NOT = P2 DISPLAY "R37 T" ELSE DISPLAY "R37 F".
      *> Format 3 (object reference): O1 and O2 the same object.
           INVOKE G2RELK "NEW" RETURNING O1.
           INVOKE G2RELK "NEW" RETURNING O3.
           SET O2 TO O1.
           IF O1 = O2 DISPLAY "R38 T" ELSE DISPLAY "R38 F".
           IF O1 IS EQUAL TO O3 DISPLAY "R39 T" ELSE DISPLAY "R39 F".
           IF O1 IS NOT EQUAL TO O3 DISPLAY "R40 T"
           ELSE DISPLAY "R40 F".
           IF O1 IS NOT = O2 DISPLAY "R41 T" ELSE DISPLAY "R41 F".
           STOP RUN.
       END PROGRAM G2REL05.
