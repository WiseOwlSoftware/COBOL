      *>--- conformance-case
      *> id: g2con01_cd_constant_entries
      *> rules: [2002-SR-13.9.2-1, 2002-SR-13.9.2-4, 2002-SR-13.9.2-5]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> Constant entries written in the COMMUNICATION SECTION, after a CD
      *> entry (the 2002 data division format: communication-description-
      *> entry followed by constant-entry | record-description-entry).
      *> §2002:13.9.2 SR1 - "If the operand of the constant entry consists
      *>   of a single numeric literal, that operand is treated as a
      *>   literal, not as an arithmetic-expression." KC AS 2.5 is the
      *>   literal 2.5 (§2002:13.9.3 GR1: as if literal-1 were written);
      *>   KE AS 2.5 + 0 is an arithmetic expression, truncated to the
      *>   integer 2 (§2002:13.9.3 GR4, §2002:7.2.5.2 GR2).
      *>   Lines KC= and K2= pin SR1; KE= is its contrast.
      *> §2002:13.9.2 SR5 (positive half) - KA AS KB + 1 depends on KB,
      *>   not on KA: legal, value 3 + 1 = 4 (line KA=), and KA is an
      *>   integer so it sizes CR-A as X(4) (§2002:13.9.2 SR2).
      *> §2002:13.9.2 SR4 (positive half) - KL AS LENGTH OF CR-B and KR
      *>   AS LENGTH OF CDREC name CD record items whose lengths do not
      *>   depend on KL or KR: legal, values 5 and 4 + 5 = 9 (§2002:13.9.3
      *>   GR6, LENGTH of an alphanumeric item = its character positions).
      *>   Negatives: g2con02-cd-constant-length-cycle (SR4),
      *>   g2con03-cd-constant-value-cycle (SR5).
      *> DERIVATION: receivers are numeric-edited PIC 9.9 (the decimal
      *>   point is an insertion character, so the shown text is fixed by
      *>   the editing rules, not by DISPLAY's implementor conversion):
      *>   MOVE KC -> 2.5; MOVE KE -> 2.0; COMPUTE KC * 2 = 5.0 -> 5.0
      *>   (4.0 if KC were the integer 2); KA 04, KL 05, KR 09 in PIC 99.
      *> Fails if: the single literal is evaluated as an expression and
      *>   truncated (KC=2.0, K2=4.0), a constant under a CD is refused, or a
      *>   legal dependency is reported as a cycle.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CON01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-E     PIC 9.9.
       01  W-L     PIC 99.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  KC CONSTANT AS 2.5.
       01  KE CONSTANT AS 2.5 + 0.
       01  KB CONSTANT AS 3.
       01  KA CONSTANT AS KB + 1.
       01  CDREC.
           05  CR-A PIC X(KA).
           05  CR-B PIC X(5).
       01  KL CONSTANT AS LENGTH OF CR-B.
       01  KR CONSTANT AS LENGTH OF CDREC.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE KC TO W-E.
           DISPLAY "KC=" W-E.
           MOVE KE TO W-E.
           DISPLAY "KE=" W-E.
           COMPUTE W-E = KC * 2.
           DISPLAY "K2=" W-E.
           MOVE KA TO W-L.
           DISPLAY "KA=" W-L.
           MOVE KL TO W-L.
           DISPLAY "KL=" W-L.
           MOVE KR TO W-L.
           DISPLAY "KR=" W-L.
           STOP RUN.
