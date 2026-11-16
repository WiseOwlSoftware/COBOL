      *>--- conformance-case
      *> id: g85_f06_05
      *> rules: [1985-GR-X.2.3.3-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.2.3.3 GR2 - the contained program G85F0605I is a
      *>   separate program: its own WS-LOCAL and its own paragraph
      *>   P-MAIN are distinct from the container's items of the same
      *>   names, yet it additionally references the container's
      *>   GLOBAL item WS-SHARED (§1985:X.4.6.4 GR2).
      *> A leg fails if the contained program sees the container's
      *> WS-LOCAL (INNER LOCAL=OUT-L), if its WS-LOCAL overwrites the
      *> container's (OUTER LOCAL=INN-L), or if WS-SHARED is not the
      *> one item both programs see (OUTER SHARED=OUTER).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F0605.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-SHARED GLOBAL PIC X(5) VALUE "OUTER".
       01  WS-LOCAL  PIC X(5) VALUE "OUT-L".
       PROCEDURE DIVISION.
       P-MAIN.
           CALL "G85F0605I".
           DISPLAY "OUTER SHARED=" WS-SHARED.
           DISPLAY "OUTER LOCAL=" WS-LOCAL.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F0605I.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-LOCAL  PIC X(5) VALUE "INN-L".
       PROCEDURE DIVISION.
       P-MAIN.
           DISPLAY "INNER SHARED=" WS-SHARED.
           DISPLAY "INNER LOCAL=" WS-LOCAL.
           MOVE "INNER" TO WS-SHARED.
           MOVE "CHG-L" TO WS-LOCAL.
           EXIT PROGRAM.
       END PROGRAM G85F0605I.
       END PROGRAM G85F0605.
