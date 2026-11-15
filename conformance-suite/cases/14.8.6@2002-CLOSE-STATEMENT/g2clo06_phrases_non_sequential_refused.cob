      *>--- conformance-case
      *> id: g2clo06_phrases_non_sequential_refused
      *> rules: [2002-SR-14.8.6.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.6.2 SR1 - "The NO REWIND, REEL, and UNIT phrases
      *> may be used only with files that are of sequential
      *> organization". RLF is relative and XF indexed, the other two
      *> 2002 organizations: CLOSE RLF REEL, CLOSE XF WITH NO REWIND and
      *> CLOSE RLF UNIT are each refused at their line. (CLOSE XF WITH
      *> LOCK is outside the rule and is the accepted control.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CLS06.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RLF ASSIGN TO "g2clo06r.dat" ORGANIZATION RELATIVE.
           SELECT XF ASSIGN TO "g2clo06x.dat" ORGANIZATION INDEXED
               RECORD KEY IS XK.
       DATA DIVISION.
       FILE SECTION.
       FD RLF.
       01 RR PIC X(4).
       FD XF.
       01 XR.
          02 XK PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           CLOSE RLF REEL
           CLOSE XF WITH NO REWIND
           CLOSE RLF UNIT
           CLOSE XF WITH LOCK
           STOP RUN.
