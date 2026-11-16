      *>--- conformance-case
      *> id: g2inv42-cd-operand-reach
      *> rules: [2002-SR-14.8.22.2-1, 2002-SR-14.8.22.2-4,
      *>    2002-SR-14.8.22.2-5, 2002-SR-14.8.22.2-9,
      *>    2002-SR-14.8.22.2-11, 2002-SR-14.8.22.2-15,
      *>    2002-GR-14.8.22.3-2, 2002-GR-14.8.22.3-6]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> INVOKE operands that are communication-section items at 2002:
      *> the record CR under CD CDOUT, the level-1 object reference and
      *> pointer records FO, CO and CP, each alone under its own CD
      *> CDOUT3, CDOUT4 and CDOUT5 (§2002:13.16.58.2 SR13 puts USAGE
      *> OBJECT REFERENCE at level 1; kb/Work PB5604). Each has its own
      *> CD because records under one output CD implicitly redefine one
      *> record area (§2002:A.1.2.2 SR9), so they would overlap RV, BR,
      *> BV and MN and the results could not be derived. And
      *> the TEXT LENGTH item OTL of CD CDOUT2 (§2002:A.1.2.3 GR16 b:
      *> "an integer of 4 digits, without an operational sign").
      *> The CD facility is obsolete at 2002 (COBOL0903 warning; the
      *> case is accepted).
      *> §2002:14.8.22.2 SR1 "Identifier-1 shall be an object
      *>   reference": the CD object references FO and CO are
      *>   identifier-1 (every INVOKE but leg 7 and the one in TRYA2).
      *> SR4 a) "literal-1 shall be the name of a method contained in
      *>   the factory interface of that class-name": FO is FACTORY OF
      *>   G2INV42C and NEW is a factory method (its result CO serves
      *>   every later leg). SR4 b) CO is described with G2INV42C, so
      *>   GET7, SET42, TWICE, ISSELF, SHOWP, SHOWR and TRYA2 are names
      *>   in its instance interface.
      *> SR5 a) BY CONTENT / BY REFERENCE arguments meet a BY REFERENCE
      *>   formal (legs 3, 4, 10, 12); b) BY VALUE meets a BY VALUE
      *>   formal (5, 8, 9); c) 14.7 conformance: PICTURE 9(4) both
      *>   sides, pointer to pointer, object reference to universal.
      *> SR9 "Identifier-3 shall be an address-identifier or shall
      *>   reference a data item defined in the ... communication
      *>   section": BR (legs 2, 4, 7), OTL (12), and the
      *>   address-identifier ADDRESS OF WX (10, 11; WX is in
      *>   working-storage because §2002:8.4.2.11.2 SR1 does not admit
      *>   a communication-section operand of ADDRESS OF).
      *> SR11 "Identifier-4 shall reference a data item defined in the
      *>   ... communication section": CO (NEW), RV (1, 5, 6, 14) and
      *>   OTL (13).
      *> SR15 "identifier-5 shall be of class numeric, object or
      *>   pointer": BV numeric (5, 6), CO object (8), CP pointer (9).
      *> §2002:14.8.22.3 GR2: FO identifies the factory object and CO
      *>   an instance object; the content of identifier-2, the CD item
      *>   MN ("SET42"), names the method (leg 7).
      *> GR6 a) 1. keywordless BR meets SR9 and SR10: BY REFERENCE
      *>   (leg 2: SET42's MOVE is seen). a) 2. keywordless OW, in the
      *>   instance object's working-storage, does not meet SR10: BY
      *>   CONTENT (leg 14: OW keeps 0001). b) keywordless BV with a
      *>   BY VALUE formal: BY VALUE (leg 6: TWICE's MOVE 0 not seen).
      *> EXPECTED (by reference the formal shares the argument's
      *> storage, by content or value it is a copy: §2002:14.1.3 GR8,
      *> GR9):
      *>   1 RV=0007        2 BR=0042        3 BR=0001 (BY CONTENT)
      *>   4 BR=0042        5 RV=0010 BV=0005
      *>   6 RV=0010 BV=0005 (keywordless, BY VALUE assumed)
      *>   7 BR=0042        8 SELF=Y         9 P=WXYZ
      *>   10 R=WXYZ        11 R=WXYZ        12 OTL=0042
      *>   13 OTL=0007      14 RV=0001
      *> Why each leg can fail: a CD item refused in any position, a
      *> wrong passing manner (BR, BV or the RV of leg 14 differ), or a
      *> wrong method resolved through FO, CO or MN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2INV42.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2INV42C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 U  USAGE OBJECT REFERENCE.
       01 FL PIC X VALUE "?".
       01 WR PIC X(4) VALUE SPACES.
       01 WX PIC X(4) VALUE "WXYZ".
       COMMUNICATION SECTION.
       CD CDOUT FOR OUTPUT.
       01 CR.
          05 RV PIC 9(4) VALUE 0.
          05 BR PIC 9(4) VALUE 1.
          05 BV PIC 9(4) VALUE 5.
          05 MN PIC X(5) VALUE "SET42".
       CD CDOUT3 FOR OUTPUT.
       01 FO USAGE OBJECT REFERENCE FACTORY OF G2INV42C.
       CD CDOUT4 FOR OUTPUT.
       01 CO USAGE OBJECT REFERENCE G2INV42C.
       CD CDOUT5 FOR OUTPUT.
       01 CP USAGE POINTER.
       CD CDOUT2 FOR OUTPUT
           TEXT LENGTH IS OTL.
       PROCEDURE DIVISION.
       MAIN.
           SET FO TO G2INV42C.
           INVOKE FO "NEW" RETURNING CO.
           INVOKE CO "GET7" RETURNING RV.
           DISPLAY "1 RV=" RV.
           INVOKE CO "SET42" USING BR.
           DISPLAY "2 BR=" BR.
           MOVE 1 TO BR.
           INVOKE CO "SET42" USING BY CONTENT BR.
           DISPLAY "3 BR=" BR.
           INVOKE CO "SET42" USING BY REFERENCE BR.
           DISPLAY "4 BR=" BR.
           INVOKE CO "TWICE" USING BY VALUE BV RETURNING RV.
           DISPLAY "5 RV=" RV " BV=" BV.
           MOVE 0 TO RV.
           INVOKE CO "TWICE" USING BV RETURNING RV.
           DISPLAY "6 RV=" RV " BV=" BV.
           SET U TO CO.
           MOVE 1 TO BR.
           INVOKE U MN USING BR.
           DISPLAY "7 BR=" BR.
           INVOKE CO "ISSELF" USING BY VALUE CO RETURNING FL.
           DISPLAY "8 SELF=" FL.
           SET CP TO ADDRESS OF WX.
           INVOKE CO "SHOWP" USING BY VALUE CP RETURNING WR.
           DISPLAY "9 P=" WR.
           MOVE SPACES TO WR.
           INVOKE CO "SHOWR" USING BY CONTENT ADDRESS OF WX
               RETURNING WR.
           DISPLAY "10 R=" WR.
           MOVE SPACES TO WR.
           INVOKE CO "SHOWR" USING ADDRESS OF WX RETURNING WR.
           DISPLAY "11 R=" WR.
           MOVE 0 TO OTL.
           INVOKE CO "SET42" USING BY REFERENCE OTL.
           DISPLAY "12 OTL=" OTL.
           MOVE 0 TO OTL.
           INVOKE CO "GET7" RETURNING OTL.
           DISPLAY "13 OTL=" OTL.
           MOVE 0 TO RV.
           INVOKE CO "TRYA2" RETURNING RV.
           DISPLAY "14 RV=" RV.
           STOP RUN.
       END PROGRAM G2INV42.

       IDENTIFICATION DIVISION.
       CLASS-ID. G2INV42C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OW PIC 9(4) VALUE 1.
       PROCEDURE DIVISION.
       METHOD-ID. GET7.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LR PIC 9(4).
       PROCEDURE DIVISION RETURNING LR.
           MOVE 7 TO LR.
       END METHOD GET7.
       METHOD-ID. SET42.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LP PIC 9(4).
       PROCEDURE DIVISION USING LP.
           MOVE 42 TO LP.
       END METHOD SET42.
       METHOD-ID. TWICE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LV PIC 9(4).
       01 LR PIC 9(4).
       PROCEDURE DIVISION USING BY VALUE LV RETURNING LR.
           COMPUTE LR = LV * 2.
           MOVE 0 TO LV.
       END METHOD TWICE.
       METHOD-ID. ISSELF.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LO USAGE OBJECT REFERENCE.
       01 LF PIC X.
       PROCEDURE DIVISION USING BY VALUE LO RETURNING LF.
           IF LO = SELF
               MOVE "Y" TO LF
           ELSE
               MOVE "N" TO LF
           END-IF.
       END METHOD ISSELF.
       METHOD-ID. SHOWP.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 LB PIC X(4) BASED.
       LINKAGE SECTION.
       01 LQ USAGE POINTER.
       01 LR PIC X(4).
       PROCEDURE DIVISION USING BY VALUE LQ RETURNING LR.
           SET ADDRESS OF LB TO LQ.
           MOVE LB TO LR.
       END METHOD SHOWP.
       METHOD-ID. SHOWR.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 LB PIC X(4) BASED.
       LINKAGE SECTION.
       01 LQ USAGE POINTER.
       01 LR PIC X(4).
       PROCEDURE DIVISION USING LQ RETURNING LR.
           SET ADDRESS OF LB TO LQ.
           MOVE LB TO LR.
       END METHOD SHOWR.
       METHOD-ID. TRYA2.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LR PIC 9(4).
       PROCEDURE DIVISION RETURNING LR.
           INVOKE SELF "SET42" USING OW.
           MOVE OW TO LR.
       END METHOD TRYA2.
       END OBJECT.
       END CLASS G2INV42C.
