      *>--- conformance-case
      *> id: g2-ls-value-every-activation
      *> rules: [2002-GR-13.5.3-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.5.3 GR2 - "Data items in the local-storage section are
      *> initialized as indicated in 13.16.61, VALUE clause."
      *> §2002:13.5.3 GR1 makes them automatic data, and
      *> §2002:14.5.2.1.2.1 places automatic data in the initial state
      *> "every time the function, method, or program in which it is
      *> described is activated"; §2002:13.16.61.3 GR4 c) applies the
      *> VALUE "when the object or runtime element is placed in initial
      *> state" and initializes class pointer items to null.
      *> Each of the three source elements that may hold the section
      *> (§2002:13.5.2 SR1) changes its local item and is activated
      *> twice, so each second activation shows the VALUE again:
      *>   function G2LS01F: CALL1=LSV1NULL, CALL2=LSV1NULL (LP was SET
      *>     to an address in the first activation, NULL again);
      *>   factory method M1 of G2LS01C: M1 L1=MLS1 twice;
      *>   program G2LS01P (not INITIAL): P L1=PLS1 twice, beside its
      *>     static working-storage W1 that keeps the last-used CHGD
      *>     (the contrast that makes the reset a local-storage effect).
      *> Fails if any activation sees CHGD/SET in a local item.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. G2LS01F.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 L1 PIC X(4) VALUE "LSV1".
       01 LP USAGE POINTER.
       LINKAGE SECTION.
       01 RES PIC X(8).
       PROCEDURE DIVISION RETURNING RES.
       MAIN-F.
           MOVE L1 TO RES(1:4)
           IF LP = NULL MOVE "NULL" TO RES(5:4)
           ELSE MOVE "SET " TO RES(5:4) END-IF
           MOVE "CHGD" TO L1
           SET LP TO ADDRESS OF L1
           GOBACK.
       END FUNCTION G2LS01F.
       IDENTIFICATION DIVISION.
       CLASS-ID. G2LS01C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY. CLASS BASE.
       IDENTIFICATION DIVISION.
       FACTORY.
       PROCEDURE DIVISION.
       IDENTIFICATION DIVISION.
       METHOD-ID. M1.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 L1 PIC X(4) VALUE "MLS1".
       PROCEDURE DIVISION.
       MAIN-M.
           DISPLAY "M1 L1=" L1
           MOVE "CHGD" TO L1
           GOBACK.
       END METHOD M1.
       END FACTORY.
       END CLASS G2LS01C.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2LS02.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION G2LS01F
           CLASS G2LS01C.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "CALL1=" FUNCTION G2LS01F
           DISPLAY "CALL2=" FUNCTION G2LS01F
           INVOKE G2LS01C "M1"
           INVOKE G2LS01C "M1"
           CALL "G2LS01P"
           CALL "G2LS01P"
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2LS01P.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W1 PIC X(4) VALUE "WSV1".
       LOCAL-STORAGE SECTION.
       01 L1 PIC X(4) VALUE "PLS1".
       PROCEDURE DIVISION.
       MAIN-S.
           DISPLAY "P L1=" L1 " W1=" W1
           MOVE "CHGD" TO L1
           MOVE "CHGD" TO W1
           GOBACK.
       END PROGRAM G2LS01P.
       END PROGRAM G2LS02.
