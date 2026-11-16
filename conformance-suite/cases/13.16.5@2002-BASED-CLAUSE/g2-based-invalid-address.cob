      *>--- conformance-case
      *> id: g2-based-invalid-address
      *> rules: [2002-GR-13.16.5.3-4]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.5.3 GR4 - "If the subject of the entry is
      *> referenced while its address is not NULL and not a valid
      *> address of storage, the EC-BOUND-PTR exception condition is
      *> set to exist."
      *> The address: G2BASLS returns ADDRESS OF its LOCAL-STORAGE item
      *> L.  §2002:8.6.3: a local-storage item "persists while that
      *> instance of the runtime element is in active state"; after
      *> GOBACK the instance is no longer active, so the pointer P is
      *> not NULL but no longer the address of storage
      *> (§2002:8.6.4: "The association may cease to exist because the
      *> actual data no longer exists").
      *> Checking is enabled by >>TURN (§2002:7.2.19.3 GR4); Table 14
      *> makes EC-BOUND-PTR Fatal: the statement is interrupted, the
      *> USE declarative runs (§2002:14.5.12.1.2 rule 2), and RESUME AT
      *> NEXT STATEMENT (§2002:14.8.31.3 GR2) continues after it.
      *> Derivation:
      *>  H1: MOVE B TO W4 raises; W4 stays "....".
      *>  H2: SEND OUT-CD FROM B - the subject referenced as the message
      *>      source of a communication statement (§2002:A.2) raises.
      *>  OWN: after SET ADDRESS OF B TO ADDRESS OF OWN (static storage
      *>  that persists to the end of the run unit, §2002:8.6.3) the
      *>  MOVE completes: W4 = "OWNS" and N stays 2.
      *> The H lines fail if the stale address is not checked; the W4
      *> line fails if the interrupted MOVE completed; the OWN line
      *> fails if a valid address raised the condition.
       >>TURN EC-BOUND-PTR CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2BAS02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  P USAGE POINTER.
       01  W4 PIC X(4) VALUE "....".
       01  OWN PIC X(4) VALUE "OWNS".
       01  N PIC 9 VALUE 0.
       01  EXN PIC X(16).
       01  B PIC X(4) BASED.
       COMMUNICATION SECTION.
       CD  OUT-CD FOR OUTPUT
           DESTINATION COUNT IS OUT-COUNT
           TEXT LENGTH IS OUT-LEN
           STATUS KEY IS OUT-ST
           SYMBOLIC DESTINATION IS OUT-DEST.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-BP SECTION.
           USE AFTER EXCEPTION CONDITION EC-BOUND-PTR.
       D-BP-P.
           ADD 1 TO N
           MOVE FUNCTION EXCEPTION-STATUS TO EXN
           DISPLAY "H" N "=[" EXN "]"
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       M-P.
           CALL "G2BASLS" RETURNING P
           SET ADDRESS OF B TO P
           MOVE B TO W4
           DISPLAY "W4=[" W4 "]"
           MOVE 1 TO OUT-COUNT
           MOVE 4 TO OUT-LEN
           MOVE "DEST1" TO OUT-DEST
           SEND OUT-CD FROM B WITH EMI
           SET ADDRESS OF B TO ADDRESS OF OWN
           MOVE B TO W4
           DISPLAY "N=" N " OWN=[" W4 "]"
           STOP RUN.
       END PROGRAM G2BAS02.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2BASLS.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01  L PIC X(4) VALUE "LLLL".
       LINKAGE SECTION.
       01  RP USAGE POINTER.
       PROCEDURE DIVISION RETURNING RP.
           SET RP TO ADDRESS OF L
           GOBACK.
       END PROGRAM G2BASLS.
