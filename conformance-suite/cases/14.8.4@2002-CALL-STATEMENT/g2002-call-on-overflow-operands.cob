      *>--- conformance-case
      *> id: g2002-call-on-overflow-operands
      *> rules: [2002-SR-14.8.4.2-1, 2002-SR-14.8.4.2-4, 2002-SR-14.8.4.2-5,
      *>    2002-SR-14.8.4.2-9]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.4.2 SR1 - identifier-1 "shall be defined as an
      *> alphanumeric, national, or program-pointer data item"; SR4 - BY
      *> REFERENCE not specified or implied, or an address-identifier:
      *> "identifier-2 is a sending operand"; SR5 - BY REFERENCE specified or
      *> implied and not an address-identifier: "it is a receiving operand";
      *> SR9 - "Identifier-3 is a receiving operand". These sit under ALL
      *> FORMATS, so they govern 2002 Format 1 (on-overflow), the form 2023
      *> removed; every CALL here is Format 1 (§2002:14.8.4.1). SR3 and SR7
      *> admit the communication section, so leg 5 passes CD fields.
      *> EXPECTED VALUES, DERIVED FROM THE 2002 RULES:
      *>  1. alphanumeric identifier-1 (SR1): its content is the
      *>     program-name (§2002:14.8.4.3 GR3). A BY REFERENCE is receiving
      *>     (SR5): the callee's XXXX reaches it. B BY CONTENT is sending
      *>     (SR4): BBBB stays. RV is receiving (SR9) and gets the result
      *>     (GR4): 0042. The call succeeds, so ON OVERFLOW is ignored
      *>     (GR3h): no "1 OVERFLOW" line.
      *>  2. national identifier-1 (SR1); A has no phrase before it, so
      *>     BY REFERENCE is assumed (GR5): XXXX BBBB 0042.
      *>  3. program-pointer identifier-1 (SR1), set by Format 8 SET from
      *>     a program-address-identifier (§2002:14.8.35.2 SR20;
      *>     §2002:8.4.2.12.3 GR1): it contains the location of the
      *>     program (GR3). A and B are both implied BY REFERENCE
      *>     (GR5), both receiving (SR5): XXXX XXXX 0042.
      *>  4. a name no program has: EC-PROGRAM-NOT-FOUND (GR3) and ON
      *>     OVERFLOW gets control (GR3g): "4 OVERFLOW".
      *>  5. CD fields: OTL BY CONTENT is sending (SR4) and stays 0005; OST
      *>     BY REFERENCE is receiving (SR5) and becomes NO; ODC as the
      *>     RETURNING item is receiving (SR9) and gets 0077.
      *>  6-7. smoke check, not an SR4 witness: ADDRESS OF A is an
      *>     address-identifier, and each evaluation creates a new unique
      *>     data item (§2002:8.4.2.11.3 GR1), so the callee nulling its
      *>     formal cannot be observed in the caller under either mode.
      *>     The callee reads AAAA through it and stores QQQQ through it;
      *>     the second call still reaches A (LX=QQQQ) and A is QQQQ.
      *> Each leg fails if a BY CONTENT store leaks back, a BY REFERENCE
      *> store is lost, the RETURNING item is not stored, a permitted
      *> identifier-1 category is refused or misresolved, or ON OVERFLOW
      *> fires on a successful call or not on a missing program.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CAL01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PN  PIC X(8) VALUE "G2CAL01S".
       01 PNN PIC N(8) VALUE N"G2CAL01S".
       01 PP  USAGE PROGRAM-POINTER.
       01 PX  PIC X(8) VALUE "G2CAL0NO".
       01 A   PIC X(4) VALUE "AAAA".
       01 B   PIC X(4) VALUE "BBBB".
       01 RV  PIC 9(4) VALUE 0.
       COMMUNICATION SECTION.
       CD CDOUT FOR OUTPUT
           DESTINATION COUNT IS ODC
           TEXT LENGTH IS OTL
           STATUS KEY IS OST.
       PROCEDURE DIVISION.
       MAIN.
           CALL PN USING BY REFERENCE A BY CONTENT B
               RETURNING RV
               ON OVERFLOW DISPLAY "1 OVERFLOW"
           END-CALL
           DISPLAY "1 " A " " B " " RV
           MOVE "AAAA" TO A MOVE "BBBB" TO B MOVE 0 TO RV
           CALL PNN USING A BY CONTENT B RETURNING RV
               ON OVERFLOW DISPLAY "2 OVERFLOW"
           END-CALL
           DISPLAY "2 " A " " B " " RV
           MOVE "AAAA" TO A MOVE "BBBB" TO B MOVE 0 TO RV
           SET PP TO ADDRESS OF PROGRAM "G2CAL01S"
           CALL PP USING A B RETURNING RV
               ON OVERFLOW DISPLAY "3 OVERFLOW"
           END-CALL
           DISPLAY "3 " A " " B " " RV
           CALL PX USING A ON OVERFLOW DISPLAY "4 OVERFLOW" END-CALL
           MOVE 5 TO OTL MOVE "OK" TO OST MOVE 0 TO ODC
           CALL "G2CAL01T" USING BY CONTENT OTL BY REFERENCE OST
               RETURNING ODC
               ON OVERFLOW DISPLAY "5 OVERFLOW"
           END-CALL
           DISPLAY "5 " OTL " " OST " " ODC
           MOVE "AAAA" TO A
           CALL "G2CAL01U" USING BY CONTENT ADDRESS OF A
               ON OVERFLOW DISPLAY "6 OVERFLOW"
           END-CALL
           CALL "G2CAL01U" USING BY CONTENT ADDRESS OF A
               ON OVERFLOW DISPLAY "7 OVERFLOW"
           END-CALL
           DISPLAY "7 " A
           STOP RUN.
       END PROGRAM G2CAL01.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CAL01S.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LA PIC X(4).
       01 LB PIC X(4).
       01 LR PIC 9(4).
       PROCEDURE DIVISION USING LA LB RETURNING LR.
       MAIN.
           MOVE "XXXX" TO LA LB
           MOVE 42 TO LR
           GOBACK.
       END PROGRAM G2CAL01S.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CAL01T.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L1 PIC 9(4).
       01 L2 PIC XX.
       01 LR PIC 9(4).
       PROCEDURE DIVISION USING L1 L2 RETURNING LR.
       MAIN.
           MOVE 9 TO L1
           MOVE "NO" TO L2
           MOVE 77 TO LR
           GOBACK.
       END PROGRAM G2CAL01T.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CAL01U.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LP USAGE POINTER.
       01 LX PIC X(4) BASED.
       PROCEDURE DIVISION USING LP.
       MAIN.
           IF LP = NULL
               DISPLAY "LP=NULL"
           ELSE
               SET ADDRESS OF LX TO LP
               DISPLAY "LX=" LX
               MOVE "QQQQ" TO LX
           END-IF
           SET LP TO NULL
           GOBACK.
       END PROGRAM G2CAL01U.
