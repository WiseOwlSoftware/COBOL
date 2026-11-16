      *>--- conformance-case
      *> id: g2_if_directive_text_replaced
      *> rules: [2002-GR-7.2.13.3-1]
      *> accept: [2002]
      *> files: [g2ifd1a.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.2.13.3 GR1 - "Text-1 and text-2 are not part of the IF
      *>   compiler directive line. Any text words in text-1 or text-2
      *>   that do not form a compiler directive line are subject to the
      *>   matching and replacing rules of the COPY statement and the
      *>   REPLACE statement." The 2002 COPY rules replace
      *>   "pseudo-text-1, text-1, word-1, literal-3, or partial-word-1"
      *>   (§2002:7.1.2.3 GR8), text-1, word-1 and literal-3 matched as
      *>   pseudo-text (GR9), and "Text-words within a debugging line
      *>   participate in the matching" (GR10 c) 6.). A compiler
      *>   directive line "is not affected by the replacing action"
      *>   (§2002:7.2.3 GR1) and matches as one space (GR10 c) 5.).
      *> EXPECTED OUTPUT, DERIVED (why each leg can fail):
      *>   WORD-R    word-1 MSG-A BY MSG-B in text-1 of >>IF G2X = 1.
      *>   LIT-R     literal-3 "LIT-A" BY "LIT-R" in text-1.
      *>   DBG-R     word-1 DBG-A BY DBG-B on a 'D' debugging line of
      *>             text-1 (compiled: WITH DEBUGGING MODE).
      *>   TEXT-R    text-1 TXT-A(1:7) BY TXT-B(3:6) in text-1.
      *>   PSEUDO-R  pseudo-text ==DISPLAY PSE-A== in text-1.
      *>             Unreplaced, each of these prints its NO- value.
      *>             REPLACING G2X BY G2Y must not touch the >>IF lines:
      *>             rewritten, >>IF G2Y = 1 is false and ELSE-ARM
      *>             prints instead of all five.
      *>   T2 LIT-R  literal-3 in text-2 of >>IF G2X = 9 (false).
      *>   R-DONE    the REPLACE statement rewrites text-2 "R-LIT" in
      *>             source text; its ==G2X = 2== cannot reach the
      *>             >>IF line (R-NOT would print).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2IFD1.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. ANY-KIND WITH DEBUGGING MODE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 MSG-A PIC X(8) VALUE "NO-WORD".
       01 MSG-B PIC X(8) VALUE "WORD-R".
       01 DBG-A PIC X(8) VALUE "NO-DBG".
       01 DBG-B PIC X(8) VALUE "DBG-R".
       01 TXT-A PIC X(8) VALUE "NO-TEXT".
       01 TXT-B PIC X(8) VALUE "XXTEXT-R".
       01 PSE-A PIC X(8) VALUE "NO-PSEU".
       01 PSE-B PIC X(8) VALUE "PSEUDO-R".
       PROCEDURE DIVISION.
       >>DEFINE G2X AS 1
       >>DEFINE G2Y AS 2
       MAIN-PARA.
           COPY g2ifd1a REPLACING MSG-A BY MSG-B
                                  "LIT-A" BY "LIT-R"
                                  DBG-A BY DBG-B
                                  TXT-A(1:7) BY TXT-B(3:6)
                                  ==DISPLAY PSE-A== BY ==DISPLAY PSE-B==
                                  G2X BY G2Y.
           REPLACE ==G2X = 2== BY ==G2X = 1==
                   =="R-LIT"== BY =="R-DONE"==.
       >>IF G2X = 2
           DISPLAY "R-NOT"
       >>ELSE
           DISPLAY "R-LIT"
       >>END-IF
           REPLACE OFF.
           STOP RUN.
