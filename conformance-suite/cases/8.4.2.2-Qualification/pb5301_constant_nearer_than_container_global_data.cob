      *>--- conformance-case
      *> id: pb5301_constant_nearer_than_container_global_data
      *> rules: [SR-8.4.2.2.1-1]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5301, review R1-1. §2023:8.4.6.2.1 rule 3 a) (the nearest
      *> declaring source element): the contained program INNERP declares the
      *> constant KK itself, so its reference to KK is to the item INNERP
      *> describes; the container's GLOBAL data item KK is declared farther out
      *> and does not make the name non-unique.
      *> DERIVATION: DISPLAY KK in INNERP shows the constant 5, not OUTERP's 1.
      *> Fails if: the reference is refused as ambiguous, or reads the data item.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5301OUTER.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  KK PIC 9 VALUE 1 GLOBAL.
       PROCEDURE DIVISION.
       P1.
           CALL "PB5301INNER".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5301INNER.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  KK CONSTANT AS 5.
       PROCEDURE DIVISION.
       P2.
           DISPLAY KK.
           EXIT PROGRAM.
       END PROGRAM PB5301INNER.
       END PROGRAM PB5301OUTER.
