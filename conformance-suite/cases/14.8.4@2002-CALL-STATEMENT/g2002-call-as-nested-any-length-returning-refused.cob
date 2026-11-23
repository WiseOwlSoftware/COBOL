      *>--- conformance-case
      *> id: g2002-call-as-nested-any-length-returning-refused
      *> rules: [2002-SR-14.8.4.2-10]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5611. §2002:14.8.4.2 SR10, under ALL FORMATS:
      *> "Identifier-2, identifier-3, and identifier-4 shall not be
      *> described with the ANY LENGTH clause". Identifier-3 is the
      *> RETURNING item: G2ALRA's formal AL is ANY LENGTH, and its
      *> CALL ... AS NESTED (format 3) RETURNING AL is refused at 2002.
      *> From 2014 format 3 bans only identifier-4 (§2014:14.9.4.2 SR18)
      *> and the same CALL is legal
      *> (13.18.2-ANY-LENGTH/pb1167_any_length_returning, from 2014).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2ALRM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC X(5) VALUE "ABCDE".
       PROCEDURE DIVISION.
           CALL "G2ALRA" AS NESTED USING W
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2ALRA.
       DATA DIVISION.
       LINKAGE SECTION.
       01 AL PIC X ANY LENGTH.
       PROCEDURE DIVISION USING AL.
           CALL "G2ALRB" AS NESTED RETURNING AL
           EXIT PROGRAM.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2ALRB.
       DATA DIVISION.
       LINKAGE SECTION.
       01 R PIC X ANY LENGTH.
       PROCEDURE DIVISION RETURNING R.
           MOVE "ZYXWV" TO R
           EXIT PROGRAM.
       END PROGRAM G2ALRB.
       END PROGRAM G2ALRA.
       END PROGRAM G2ALRM.
