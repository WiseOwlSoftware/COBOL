      *>--- conformance-case
      *> id: g2-ws-value-initialization
      *> rules: [2002-GR-13.4.3-3]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.4.3 GR3 - "Data items in the working-storage section
      *> are initialized as indicated in 13.16.61, VALUE clause."
      *> §2002:13.16.61.3 GR4 gives each occasion, and every leg here
      *> is one of them:
      *>  c) ordinary items, "when the object or runtime element is
      *>     placed in initial state": W1=WSV1 W2=042 WG=AB07 on entry;
      *>     "data items of class object and class pointer are
      *>     initialized to null": WP NULL, WO NULL.
      *>  c) "during the execution of an INITIALIZE statement": W1, W2
      *>     overwritten, INITIALIZE ... ALL TO VALUE -> WSV1 042.
      *>  a) "for external items, during the execution of an INITIALIZE
      *>     statement": WEX overwritten to ZZZ, INITIALIZE -> EXV.
      *>  b) "for based items ... during the execution of an ALLOCATE":
      *>     ALLOCATE WB INITIALIZED (§2002:14.8.3.3 GR7, as if INITIALIZE
      *>     ... ALL TO VALUE) -> WBX=BSV, WBN=05.
      *> Initial state (§2002:14.5.2.1.2.1): static data (§2002:13.4.3
      *> GR1) is placed in it the first time the program is activated
      *> and after a CANCEL of it, so the non-initial G2WS01S shows
      *> S1=STV1, then its own last-used CHGD, then STV1 after CANCEL;
      *> initial data (§2002:13.4.3 GR2) is placed in it "every time"
      *> the program is activated, so the INITIAL G2WS01I shows
      *> I1=INI1 on both calls.
      *> Fails if a VALUE is not applied on any occasion, if a pointer
      *> or object reference is not null, if the static item is reset
      *> on the second call or not reset after CANCEL, or if the
      *> initial program keeps CHGD.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2WS01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W1 PIC X(4) VALUE "WSV1".
       01 W2 PIC 9(3) VALUE 42.
       01 WG.
          05 WG1 PIC X(2) VALUE "AB".
          05 WG2 PIC 9(2) VALUE 7.
       01 WP USAGE POINTER.
       01 WO USAGE OBJECT REFERENCE.
       01 WE IS EXTERNAL.
          05 WEX PIC X(3) VALUE "EXV".
       01 WB BASED.
          05 WBX PIC X(3) VALUE "BSV".
          05 WBN PIC 9(2) VALUE 5.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "W1=" W1 " W2=" W2 " WG=" WG
           IF WP = NULL DISPLAY "WP NULL" ELSE DISPLAY "WP NOT NULL"
           END-IF
           IF WO = NULL DISPLAY "WO NULL" ELSE DISPLAY "WO NOT NULL"
           END-IF
           MOVE "QQQQ" TO W1
           MOVE 999 TO W2
           INITIALIZE W1 W2 ALL TO VALUE
           DISPLAY "INIT W1=" W1 " W2=" W2
           MOVE "ZZZ" TO WEX
           INITIALIZE WE ALL TO VALUE
           DISPLAY "WE=" WEX
           ALLOCATE WB INITIALIZED
           DISPLAY "WB=" WBX " WBN=" WBN
           CALL "G2WS01S"
           CALL "G2WS01S"
           CANCEL "G2WS01S"
           CALL "G2WS01S"
           CALL "G2WS01I"
           CALL "G2WS01I"
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2WS01S.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S1 PIC X(4) VALUE "STV1".
       PROCEDURE DIVISION.
       MAIN-S.
           DISPLAY "S1=" S1
           MOVE "CHGD" TO S1
           GOBACK.
       END PROGRAM G2WS01S.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2WS01I IS INITIAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 I1 PIC X(4) VALUE "INI1".
       PROCEDURE DIVISION.
       MAIN-I.
           DISPLAY "I1=" I1
           MOVE "CHGD" TO I1
           GOBACK.
       END PROGRAM G2WS01I.
       END PROGRAM G2WS01.
