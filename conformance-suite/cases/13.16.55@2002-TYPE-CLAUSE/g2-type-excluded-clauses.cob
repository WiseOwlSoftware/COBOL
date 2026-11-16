      *>--- conformance-case
      *> id: g2-type-excluded-clauses
      *> rules: [2002-GR-13.16.55.3-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.55.3 GR1 - "The effect of the TYPE clause is as
      *> though the data description identified by type-name-1 had been
      *> coded in place of the TYPE clause, excluding the level-number,
      *> name, alignment, and the GLOBAL, SELECT WHEN, and TYPEDEF
      *> clauses specified for type-name-1".
      *> T is a level-1 type declaration carrying GLOBAL and SELECT WHEN
      *> IS-A, with VALUEs on its subordinates. R-SUB is a level-05 item
      *> of TYPE T and V a level-01 item of TYPE T:
      *>  - level-number and name excluded: R-SUB keeps level 05 and its
      *>    own name, and T's subordinates are reached as OF R-SUB / OF V;
      *>  - GLOBAL and SELECT WHEN excluded: both are permitted only at
      *>    level 1 (§2002:13.16.25.2 SR1 b, "A data description entry
      *>    whose level-number is 1"; §2002:13.16.49.2 SR1, "may be
      *>    specified only at the 01 level of a record description
      *>    entry"), so the level-05 R-SUB is valid only because neither
      *>    is copied; a SELECT WHEN in working-storage has no effect
      *>    (§2002:13.16.49.3 GR2), so V is plain data;
      *>  - TYPEDEF excluded: R-SUB, R-E and V have storage and VALUEs
      *>    ("A type declaration has no storage associated with it",
      *>    §2002:13.16.56.3 GR2, would otherwise apply to them);
      *>  - the rest is copied: PICTUREs, VALUEs and the 88 IS-A.
      *> The non-carriage of GLOBAL to V is also shown by the refusal
      *> g2-type-global-not-carried beside this case. Alignment is not
      *> observable: WiseOwl COBOL treats every byte as a natural
      *> boundary (docs/conformance DOC-A.1-197).
      *> DERIVATION
      *>  REC = R-X "R" + R-SUB (T-TAG "A", T-VAL "TTT") + R-E (PIC 9(3)
      *>  VALUE 42 -> "042")                 -> "REC=RATTT042"
      *>                                     -> "TAG=A VAL=TTT E=042"
      *>  1 + 1 + 3 + 3 = 8 positions, MOVEd to PIC 99 -> "LEN=08"
      *>  MOVE "BXYZ" TO V (group move)      -> "V=BXYZ"
      *>  T-TAG OF V is "B", IS-A is VALUE "A" -> "V NOT IS-A"
      *>  MOVE "QBCDE007" TO REC: R-X=Q T-TAG=B T-VAL=CDE R-E=007
      *>                                     -> "TAG=B VAL=CDE E=007"
      *>  the nested G2TYX01I sees the type-name T, because "The GLOBAL
      *>  clause applies to the scope of the type-name" (§2002:13.16.56.3
      *>  GR3), and W TYPE T takes T's VALUEs    -> "W=ATTT"
      *> A leg fails if a clause excluded by GR1 is copied (the level-05
      *> entry would be refused, or R-SUB would have no storage), or if
      *> a clause GR1 copies (PICTURE, VALUE, the 88) is lost.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2TYX01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T TYPEDEF GLOBAL SELECT WHEN IS-A.
           05  T-TAG PIC X VALUE "A".
               88  IS-A VALUE "A".
           05  T-VAL PIC X(3) VALUE "TTT".
       01  E TYPEDEF PIC 9(3) VALUE 42.
       01  REC.
           05  R-X PIC X VALUE "R".
           05  R-SUB TYPE T.
           05  R-E TYPE E.
       01  V TYPE T.
       01  WS-LEN PIC 99.
       PROCEDURE DIVISION.
           DISPLAY "REC=" REC.
           DISPLAY "TAG=" T-TAG OF R-SUB " VAL=" T-VAL OF R-SUB
                   " E=" R-E.
           MOVE FUNCTION LENGTH(REC) TO WS-LEN.
           DISPLAY "LEN=" WS-LEN.
           MOVE "BXYZ" TO V.
           DISPLAY "V=" V.
           IF IS-A OF V DISPLAY "V IS-A" ELSE DISPLAY "V NOT IS-A".
           MOVE "QBCDE007" TO REC.
           DISPLAY "TAG=" T-TAG OF R-SUB " VAL=" T-VAL OF R-SUB
                   " E=" R-E.
           CALL "G2TYX01I".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2TYX01I.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W TYPE T.
       PROCEDURE DIVISION.
           DISPLAY "W=" W.
           GOBACK.
       END PROGRAM G2TYX01I.
       END PROGRAM G2TYX01.
