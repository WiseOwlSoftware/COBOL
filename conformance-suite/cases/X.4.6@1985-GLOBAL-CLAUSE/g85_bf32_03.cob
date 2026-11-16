      *>--- conformance-case
      *> id: g85_bf32_03
      *> rules: [1985-FMT-X.4.6.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.4.6.2 - the GLOBAL clause general format, IS GLOBAL,
      *> with the optional IS written (G1) and omitted (G2), placed
      *> before and after the PICTURE clause, on level-01 entries of
      *> the Working-Storage Section (§1985:X.4.6.3 SR1). The program
      *> must translate at 85 (a refusal of either spelling rejects
      *> legal source). §1985:X.4.6.4 GR2: the contained program
      *> references both names without describing them again; line N
      *> fails if it does not see the container's values, line O if
      *> its stores did not reach the container's items.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF3203.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G1  IS GLOBAL PIC 9(3) VALUE 7.
       01  G2  PIC X(3) VALUE "ABC" GLOBAL.
       PROCEDURE DIVISION.
       P1.
           CALL "BF32N03".
           DISPLAY "O G1=" G1 " G2=" G2.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. BF32N03.
       PROCEDURE DIVISION.
       N1.
           DISPLAY "N G1=" G1 " G2=" G2.
           ADD 1 TO G1.
           MOVE "XYZ" TO G2.
           EXIT PROGRAM.
       END PROGRAM BF32N03.
       END PROGRAM G85BF3203.
