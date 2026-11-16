      *>--- conformance-case
      *> id: g85_bf32_04
      *> rules: [1985-GR-X.5.3.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.5.3.4 GR3 - a program named in a CANCEL statement in
      *> another program must be callable by that program. BF32C04 is
      *> COMMON and directly contained in G85BF3204, so by
      *> §1985:X.1.3.8.1 SR2 both the container and its other contained
      *> program BF32B04 may name it: each CANCELs it, and by
      *> §1985:X.5.3.4 GR2 the next CALL finds it in its initial state
      *> (CNT VALUE 0). Lines 1-3 come from BF32B04 (call, call,
      *> cancel, call: 1 2 1); lines 4-5 from the container (call: 2,
      *> cancel, call: 1). A line fails if the CANCEL is refused, is
      *> not applied to the program the name locates, or resets it
      *> when it should not.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF3204.
       PROCEDURE DIVISION.
       P1.
           CALL "BF32B04".
           CALL "BF32C04".
           CANCEL "BF32C04".
           CALL "BF32C04".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. BF32B04.
       PROCEDURE DIVISION.
       B1.
           CALL "BF32C04".
           CALL "BF32C04".
           CANCEL "BF32C04".
           CALL "BF32C04".
           EXIT PROGRAM.
       END PROGRAM BF32B04.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. BF32C04 IS COMMON PROGRAM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  CNT PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       C1.
           ADD 1 TO CNT.
           DISPLAY "C CNT=" CNT.
           EXIT PROGRAM.
       END PROGRAM BF32C04.
       END PROGRAM G85BF3204.
