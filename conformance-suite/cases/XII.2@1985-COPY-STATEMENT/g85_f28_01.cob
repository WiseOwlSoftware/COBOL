      *>--- conformance-case
      *> id: g85_f28_01
      *> rules: [1985-SR-XII.2.3-5]
      *> accept: [85]
      *> files: [G85F28LB.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XII.2.3 SR5 - character-strings within pseudo-text-1 and
      *> pseudo-text-2 of COPY REPLACING are continued: a word, a numeric
      *> literal and a nonnumeric literal, each split by a continuation
      *> line. cite.py --check "§1985:XII.2.3 SR5" "Character-strings
      *> within pseudo-text-1 and pseudo-text-2 may be continued" -> OK.
      *> §1985:IV.7.2.5 "the normal rules for continuation of lines
      *> apply to the formation of text words" -> OK; §1985:IV.7.2.2: a
      *> hyphen joins the first nonblank character in area B to the last
      *> nonblank character of the preceding line, and a continued
      *> nonnumeric literal resumes after the quotation mark that opens
      *> area B (-> OK). Each opening literal fragment ends in column 72
      *> (margin R), so no trailing space enters the literal.
      *> Library text G85F28LB:
      *>   MOVE "ALPHA" TO WS-RESULT.  MOVE 1111 TO WS-NUM.
      *> DERIVED (§1985:XII.2.4 GR3: each matched pseudo-text-1 replaced):
      *>   WS-RES+ULT = WS-RESULT    -> WS-OTH+ER = WS-OTHER
      *>   11+11 = 1111              -> 22+22 = 2222
      *>   "AL"+"PHA" = "ALPHA"      -> "CONTINUED-"+"OK"
      *> so the library text becomes MOVE "CONTINUED-OK" TO WS-OTHER.
      *> MOVE 2222 TO WS-NUM. and the one line is
      *>   [            ][CONTINUED-OK]2222
      *> A pseudo-text not joined fails to match or places other text:
      *> WS-RESULT would show ALPHA, WS-NUM 1111, or WS-OTHER differ.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F2801.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-RESULT PIC X(12) VALUE SPACES.
       01  WS-OTHER  PIC X(12) VALUE SPACES.
       01  WS-NUM    PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       MAIN-P.
           COPY G85F28LB REPLACING
               ==WS-RES
      -        ULT== BY ==WS-OTH
      -        ER==
               ==11
      -        11== BY ==22
      -        22==
               ==                                                    "AL
      -        "PHA"== BY
               ==                                            "CONTINUED-
      -        "OK"==.
           DISPLAY "[" WS-RESULT "][" WS-OTHER "]" WS-NUM.
           STOP RUN.
