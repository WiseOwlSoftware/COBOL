      *>--- conformance-case
      *> id: pb5301-global-constant-and-data-same-container
      *> rules: [SR-8.4.2.2.1-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5301, review R1-1 of the w20262006b finisher: the refusal
      *> that bounds pb5301_global_constant_nearer_than_farther_global_data.
      *> The container PB5301SMID declares BOTH a GLOBAL constant KK and a
      *> GLOBAL data item KK, so in the contained PB5301SINNER the two are
      *> declared in one source element and neither is nearer (§2023:8.4.6.2.1
      *> GR3b.1 selects the source element, not an item inside it). A
      *> constant-name cannot be qualified, so the bare KK does not uniquely
      *> identify its item: §2023:8.4.2.2.3 SR1, COBOL1639.
      *> Fails if: the program compiles.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5301SOUTER.
       PROCEDURE DIVISION.
       P1.
           CALL "PB5301SMID".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5301SMID.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  KK CONSTANT IS GLOBAL AS 5.
       01  G GLOBAL.
           05  KK PIC 9 VALUE 2.
       PROCEDURE DIVISION.
       P2.
           CALL "PB5301SINNER".
           EXIT PROGRAM.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5301SINNER.
       PROCEDURE DIVISION.
       P3.
           DISPLAY KK.
           EXIT PROGRAM.
       END PROGRAM PB5301SINNER.
       END PROGRAM PB5301SMID.
       END PROGRAM PB5301SOUTER.
