      *>--- conformance-case
      *> id: pb5301_global_constant_nearer_than_farther_global_data
      *> rules: [SR-8.4.2.2.1-1]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5301, review R1-1 of the w20262006b finisher: the INHERITED
      *> arm of pb5301_constant_nearer_than_container_global_data.
      *> §2023:8.4.6.2.1 GR3b.1 (cite.py --check OK): "The item in source
      *> element A if the name is declared in source element A." INNERP
      *> declares no KK; its direct container MIDP declares the GLOBAL constant
      *> KK, and MIDP's container OUTERP the GLOBAL data item KK, farther out.
      *> The nearer declaration, the constant, is the referenced item, and the
      *> data item does not make the name non-unique.
      *> DERIVATION: DISPLAY KK in INNERP shows the constant 5, not OUTERP's 1.
      *> Fails if: the reference is refused as ambiguous, or reads the data item.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5301GOUTER.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  KK PIC 9 VALUE 1 GLOBAL.
       PROCEDURE DIVISION.
       P1.
           CALL "PB5301GMID".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5301GMID.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  KK CONSTANT IS GLOBAL AS 5.
       PROCEDURE DIVISION.
       P2.
           CALL "PB5301GINNER".
           EXIT PROGRAM.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5301GINNER.
       PROCEDURE DIVISION.
       P3.
           DISPLAY KK.
           EXIT PROGRAM.
       END PROGRAM PB5301GINNER.
       END PROGRAM PB5301GMID.
       END PROGRAM PB5301GOUTER.
