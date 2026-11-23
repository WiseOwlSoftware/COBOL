      *>--- conformance-case
      *> id: g2002-call-as-nested-any-length-using-refused
      *> rules: [2002-SR-14.8.4.2-10]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5611. §2002:14.8.4.2 SR10, under ALL FORMATS:
      *> "Identifier-2, identifier-3, and identifier-4 shall not be
      *> described with the ANY LENGTH clause". G2ALUA's formal LA is
      *> ANY LENGTH (a contained program may describe one), and its
      *> CALL ... AS NESTED (format 3) passes LA on as identifier-2
      *> (BY REFERENCE, assumed from the formal by §2002:14.8.4.3 GR9a1),
      *> so the CALL is refused at 2002. From 2014 the rule is split by
      *> format (§2014:14.9.4.2 SR11, SR18) and the same CALL is legal
      *> (14.9.4-CALL/pb240_call_operand_screens, accepted from 2014).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2ALUM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC X(6) VALUE "ABCDEF".
       PROCEDURE DIVISION.
           CALL "G2ALUA" AS NESTED USING W
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2ALUA.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LA PIC X ANY LENGTH.
       PROCEDURE DIVISION USING LA.
           CALL "G2ALUB" AS NESTED USING LA
           EXIT PROGRAM.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2ALUB.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LB PIC X ANY LENGTH.
       PROCEDURE DIVISION USING LB.
           EXIT PROGRAM.
       END PROGRAM G2ALUB.
       END PROGRAM G2ALUA.
       END PROGRAM G2ALUM.
