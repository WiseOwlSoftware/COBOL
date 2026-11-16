      *>--- conformance-case
      *> id: g85_f02_04
      *> rules: [1985-GR-X.5.2.4-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.5.2.4 GR4 - two programs named G85F02DP and two named
      *> G85F02CM. Mechanism a: G85F0204 calls its directly contained
      *> G85F02DP and G85F02CM (NESTED DP, COMMON CM FROM MAIN).
      *> Mechanism b: contained G85F02P2 calls the common G85F02CM of
      *> its container (COMMON CM FROM P2). Mechanism c: G85F02P2 calls
      *> G85F02DP, which is neither directly contained in P2 nor common,
      *> so the separately compiled one runs (SEPARATE DP), and that one
      *> calls G85F02CM, which only the separate CM answers. A wrong
      *> pick or a refusal of the duplicate names fails a leg.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F0204.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WHO PIC X(4) VALUE "MAIN".
       PROCEDURE DIVISION.
       P-MAIN.
           CALL "G85F02DP".
           CALL "G85F02CM" USING WHO.
           CALL "G85F02P2".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F02DP.
       PROCEDURE DIVISION.
       P-DP.
           DISPLAY "NESTED DP".
           EXIT PROGRAM.
       END PROGRAM G85F02DP.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F02CM IS COMMON PROGRAM.
       DATA DIVISION.
       LINKAGE SECTION.
       01  CALLER PIC X(4).
       PROCEDURE DIVISION USING CALLER.
       P-CM.
           DISPLAY "COMMON CM FROM " CALLER.
           EXIT PROGRAM.
       END PROGRAM G85F02CM.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F02P2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WHO PIC X(4) VALUE "P2".
       PROCEDURE DIVISION.
       P-P2.
           CALL "G85F02CM" USING WHO.
           CALL "G85F02DP".
           EXIT PROGRAM.
       END PROGRAM G85F02P2.
       END PROGRAM G85F0204.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F02DP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WHO PIC X(4) VALUE "SDP".
       PROCEDURE DIVISION.
       P-SDP.
           DISPLAY "SEPARATE DP".
           CALL "G85F02CM" USING WHO.
           EXIT PROGRAM.
       END PROGRAM G85F02DP.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F02CM.
       DATA DIVISION.
       LINKAGE SECTION.
       01  CALLER PIC X(4).
       PROCEDURE DIVISION USING CALLER.
       P-SCM.
           DISPLAY "SEPARATE CM FROM " CALLER.
           EXIT PROGRAM.
       END PROGRAM G85F02CM.
