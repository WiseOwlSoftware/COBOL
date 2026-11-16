      *>--- conformance-case
      *> id: g85_f06_06
      *> rules: [1985-SR-X.4.3.3-4, 1985-GR-X.4.3.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.4.3.3 SR4 - the GLOBAL clause stands in a data
      *>   description entry whose level-number is 01 (WS-GRP); the
      *>   names subordinate to it are then global (§1985:X.4.6.4
      *>   GR1), so the contained program G85F0606I references WS-G-A,
      *>   WS-G-N and the condition-name G-TWELVE without describing
      *>   them: lines "I GRP=" and "I G-TWELVE".
      *> §1985:X.4.3.4 GR1 - the Inter-Program Communication data
      *>   description entry: the EXTERNAL clause (X-23) makes WS-EXT
      *>   one record for G85F0606 and the separately compiled
      *>   G85F0606X (§1985:X.4.5.4 GR1), the GLOBAL clause (X-24)
      *>   works as above, and every other clause (PICTURE, VALUE,
      *>   level 88) behaves as the Nucleus module states: lines
      *>   "X SEES" and "OUTER EXT=".
      *> A leg fails if either level-01 entry is refused, if the
      *> separately compiled program sees its own copy of WS-EXT, or
      *> if the global group's members are not visible inside.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F0606.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-EXT IS EXTERNAL.
           05  WS-EXT-A PIC X(4).
           05  WS-EXT-N PIC 9(3).
       01  WS-GRP IS GLOBAL.
           05  WS-G-A   PIC X(4) VALUE "GRPA".
           05  WS-G-N   PIC 9(2) VALUE 12.
               88  G-TWELVE VALUE 12.
       PROCEDURE DIVISION.
       P-MAIN.
           MOVE "EXTA" TO WS-EXT-A.
           MOVE 5 TO WS-EXT-N.
           CALL "G85F0606X".
           CALL "G85F0606I".
           DISPLAY "OUTER EXT=" WS-EXT-A " " WS-EXT-N.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F0606I.
       PROCEDURE DIVISION.
       P-INNER.
           DISPLAY "I GRP=" WS-G-A " " WS-G-N.
           IF G-TWELVE
               DISPLAY "I G-TWELVE YES"
           ELSE
               DISPLAY "I G-TWELVE NO".
           EXIT PROGRAM.
       END PROGRAM G85F0606I.
       END PROGRAM G85F0606.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F0606X.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-EXT IS EXTERNAL.
           05  WS-X-A   PIC X(4).
           05  WS-X-N   PIC 9(3).
       PROCEDURE DIVISION.
       P-X.
           DISPLAY "X SEES " WS-X-A " " WS-X-N.
           MOVE "XSET" TO WS-X-A.
           ADD 1 TO WS-X-N.
           EXIT PROGRAM.
       END PROGRAM G85F0606X.
