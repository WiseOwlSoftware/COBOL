      *>--- conformance-case
      *> id: pb5617-cd-implicit-field-by-reference-and-returning
      *> rules: [2002-SR-14.8.22.2-14, 2002-GR-14.8.22.3-3, 2002-GR-14.8.22.3-8]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5617 - an output CD's TEXT LENGTH and STATUS KEY are
      *> implicitly described as "02 data-name PICTURE 9(4)" and
      *> "PICTURE XX" (2002 A.1.2.3 GR16), so they carry the same
      *> PICTURE, USAGE and SIGN as a PIC 9(4) and a PIC XX formal
      *> parameter (14.7.1.2.1): they may be an INVOKE's RETURNING item
      *> and an argument of INVOKE (BY REFERENCE implied) and of CALL.
      *> GET42 returns 42; BUMP and PB5617S add 1 to their first
      *> parameter, PB5617S also moving NO to its second.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5617A.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB5617C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O  USAGE OBJECT REFERENCE PB5617C.
       COMMUNICATION SECTION.
       CD CDOUT FOR OUTPUT
           TEXT LENGTH IS OTL
           STATUS KEY IS OST.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB5617C "NEW" RETURNING O.
           INVOKE O "GET42" RETURNING OTL.
           DISPLAY "TL=" OTL.
           INVOKE O "BUMP" USING OTL.
           DISPLAY "TL=" OTL.
           MOVE "OK" TO OST.
           CALL "PB5617S" USING BY REFERENCE OTL OST.
           DISPLAY "TL=" OTL " ST=" OST.
           STOP RUN.
       END PROGRAM PB5617A.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5617S.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L1 PIC 9(4).
       01 L2 PIC XX.
       PROCEDURE DIVISION USING L1 L2.
       MAIN.
           ADD 1 TO L1.
           MOVE "NO" TO L2.
           EXIT PROGRAM.
       END PROGRAM PB5617S.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB5617C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. GET42.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LR PIC 9(4).
       PROCEDURE DIVISION RETURNING LR.
       MAIN.
           MOVE 42 TO LR.
       END METHOD GET42.
       METHOD-ID. BUMP.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LA PIC 9(4).
       PROCEDURE DIVISION USING LA.
       MAIN.
           ADD 1 TO LA.
       END METHOD BUMP.
       END OBJECT.
       END CLASS PB5617C.
