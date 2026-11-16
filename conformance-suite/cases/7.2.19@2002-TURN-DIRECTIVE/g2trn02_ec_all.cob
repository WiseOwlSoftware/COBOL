      *>--- conformance-case
      *> id: g2trn02_ec_all
      *> rules: [2002-GR-7.2.19.3-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.2.19.3 GR2 - "If exception-name-1 EC-ALL is specified,
      *>   the effect is as if the same TURN directive were specified
      *>   containing all exception-names."
      *>   cite.py: OK  §2002:7.2.19.3 GR2
      *> Supporting: §2002:7.2.19.3 GR1 default >>TURN EC-ALL CHECKING
      *>   OFF; §2002:9.1.12 I-O status 1x -> EC-I-O-AT-END "set to
      *>   exist", with an AT END phrase "no USE procedure shall be
      *>   executed"; §2002:14.8.29.3 GR21 (I-O status '10', control to
      *>   the AT END imperative); §2002:8.4.1.2.3 GR2 a subscript above
      *>   the OCCURS maximum: "the EC-BOUND-SUBSCRIPT exception
      *>   condition is set to exist"; §2002:14.5.12.1 raised only when
      *>   enabled, then "the last exception status is set to indicate
      *>   that exception condition"; §2002:14.5.12.1.2 a fatal
      *>   condition with checking enabled and an applicable USE: "the
      *>   associated declarative is executed"; §2002:14.5.12.1.1 a
      *>   STOP statement "executed within the scope of the
      *>   declarative" is not normal completion, so no abnormal
      *>   termination follows; §2002:14.8.35.3 GR25; §2002:15.28.2 RV1.
      *>   Every one cite.py --check OK.
      *> No SIZE ERROR leg: with a SIZE ERROR phrase §2002:14.6.4 never
      *>   sets EC-SIZE-TRUNCATION to exist (only the no-phrase branch
      *>   does; §2002:E.15 "the raising of the EC-SIZE exception
      *>   condition is disabled for that statement"), so such a leg
      *>   cannot discriminate EC-ALL.
      *> DERIVATION. F1 is empty (every READ is at end), I = 5 >
      *> OCCURS 3.
      *>   1 before any TURN (GR1): AT END unchecked -> spaces.
      *>   >>TURN EC-ALL CHECKING ON = ON for every exception-name
      *>   (GR2), so two different level-2 families are enabled:
      *>   2 I-O -> EC-I-O-AT-END;
      *>   3 the MOVE to E(I) raises EC-BOUND-SUBSCRIPT, the EC-BOUND
      *>     declarative runs, shows it and STOPs.
      *> An implementation whose EC-ALL missed a family shows spaces on
      *> 2, or for BOUND never runs the declarative.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2TRN02.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "G2TRN021.DAT".
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 R1 PIC X(4).
       WORKING-STORAGE SECTION.
       01 T.
          05 E PIC X OCCURS 3.
       01 I PIC 9 VALUE 5.
       PROCEDURE DIVISION.
       DECLARATIVES.
       BND SECTION.
           USE AFTER EXCEPTION CONDITION EC-BOUND.
       B1.
           DISPLAY "3 BOUND [" FUNCTION EXCEPTION-STATUS "]".
           STOP RUN.
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           OPEN OUTPUT F1.
           CLOSE F1.
           OPEN INPUT F1.
           READ F1 AT END
               DISPLAY "1 [" FUNCTION EXCEPTION-STATUS "]"
           END-READ.
           CLOSE F1.
       >>TURN EC-ALL CHECKING ON
           OPEN INPUT F1.
           SET LAST EXCEPTION TO OFF.
           READ F1 AT END
               DISPLAY "2 I-O [" FUNCTION EXCEPTION-STATUS "]"
           END-READ.
           CLOSE F1.
           MOVE "Z" TO E(I).
           DISPLAY "NOT REACHED".
           STOP RUN.
