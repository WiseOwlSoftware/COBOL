      *>--- conformance-case
      *> id: g2-based-null-reference
      *> rules: [2002-GR-13.16.5.3-3]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.5.3 GR3 - "If the subject of the entry or any item
      *> subordinate to it is referenced directly or indirectly while
      *> its address is NULL, the EC-DATA-PTR-NULL exception condition
      *> is set to exist."  §2002:13.16.5.3 GR2: the implicit
      *> data-address pointer "has an initial value of NULL".
      *> Checking is enabled by >>TURN (§2002:7.2.19.3 GR4); Table 14
      *> makes EC-DATA-PTR-NULL Fatal, so the statement is interrupted
      *> and the USE declarative runs (§2002:14.5.12.1.2 rule 2); the
      *> RESUME AT NEXT STATEMENT (§2002:14.8.31.3 GR2) keeps the run
      *> unit alive and continues after the interrupted statement.
      *> Derivation, one line per leg:
      *>  H1: MOVE B TO W4 - the subject referenced while NULL; the
      *>      MOVE is interrupted, so W4 stays "....".
      *>  H2: MOVE B1 TO W4 - a subordinate item referenced (sending).
      *>  H3: MOVE "ZZ" TO B2 - a subordinate item referenced
      *>      (receiving).
      *>  H4: SEND OUT-CD FROM B - the subject referenced as the message
      *>      source of a communication statement (§2002:A.2).
      *>  After SET ADDRESS OF B TO ADDRESS OF W4 the address is not
      *>  NULL: MOVE B1 TO W2 raises nothing, so N stays 4 and
      *>  W2 = "..".  Each H line fails if the reference is not checked;
      *>  the W4 lines fail if the interrupted MOVE completed; the last
      *>  line fails if a valid address raised the condition.
      *> EXCEPTION-STATUS is 31 characters, left-justified
      *> (§2002:15.28.2 RV1); it is moved to a 16-character item so
      *> the line carries no trailing spaces.
       >>TURN EC-DATA-PTR-NULL CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2BAS01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W4 PIC X(4) VALUE "....".
       01  W2 PIC X(2) VALUE "--".
       01  N PIC 9 VALUE 0.
       01  EXN PIC X(16).
       01  B BASED.
           05 B1 PIC X(2).
           05 B2 PIC X(2).
       COMMUNICATION SECTION.
       CD  OUT-CD FOR OUTPUT
           DESTINATION COUNT IS OUT-COUNT
           TEXT LENGTH IS OUT-LEN
           STATUS KEY IS OUT-ST
           SYMBOLIC DESTINATION IS OUT-DEST.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-DATA-PTR-NULL.
       H-P.
           ADD 1 TO N
           MOVE FUNCTION EXCEPTION-STATUS TO EXN
           DISPLAY "H" N "=[" EXN "]"
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           MOVE B TO W4
           DISPLAY "W4=[" W4 "]"
           MOVE B1 TO W4
           DISPLAY "W4=[" W4 "]"
           MOVE "ZZ" TO B2
           MOVE 1 TO OUT-COUNT
           MOVE 4 TO OUT-LEN
           MOVE "DEST1" TO OUT-DEST
           SEND OUT-CD FROM B WITH EMI
           SET ADDRESS OF B TO ADDRESS OF W4
           MOVE B1 TO W2
           DISPLAY "N=" N " W2=[" W2 "]"
           STOP RUN.
