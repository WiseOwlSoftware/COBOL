      *>--- conformance-case
      *> id: g85_f28_02
      *> rules: [1985-SR-XII.3.3-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XII.3.3 SR5 - character-strings within pseudo-text-1 and
      *> pseudo-text-2 of REPLACE are continued: words, a numeric literal
      *> and a nonnumeric literal split by continuation lines.
      *> cite.py --check "§1985:XII.3.3 SR5" "Character-strings within
      *> pseudo-text-1 and pseudo-text-2 may be continued" -> OK.
      *> §1985:IV.7.2.5 and §1985:IV.7.2.2 (each -> OK) form the text
      *> words by the continuation rules; the opening literal fragment
      *> ends in column 72 (margin R), so no trailing space enters it.
      *> DERIVED (§1985:XII.3.4 GR1: each matched pseudo-text-1 replaced):
      *>   SET-RES+ULT = SET-RESULT -> MOVE "CONT"+"INUED-OK" TO
      *>      WS-RES+ULT = MOVE "CONTINUED-OK" TO WS-RESULT
      *>   SET-N+UM = SET-NUM -> MOVE 12+34 TO WS-NUM (1234)
      *> so the one line is [CONTINUED-OK]1234. A pseudo-text-1 not
      *> joined leaves SET-RESULT or SET-NUM in the source and the program
      *> does not compile; a pseudo-text-2 not joined moves other text.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F2802.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-RESULT PIC X(12) VALUE SPACES.
       01  WS-NUM    PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       MAIN-P.
           REPLACE ==SET-RES
      -        ULT== BY
               ==MOVE                                              "CONT
      -        "INUED-OK" TO WS-RES
      -        ULT==
                   ==SET-N
      -        UM== BY ==MOVE 12
      -        34 TO WS-NUM==.
           SET-RESULT.
           SET-NUM.
           DISPLAY "[" WS-RESULT "]" WS-NUM.
           STOP RUN.
