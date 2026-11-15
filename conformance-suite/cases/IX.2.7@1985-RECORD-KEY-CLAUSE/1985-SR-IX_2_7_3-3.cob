      *>--- conformance-case
      *> id: 1985-SR-IX_2_7_3-3
      *> rules: [1985-SR-IX.2.7.3-3, 2002-SR-12.3.4.12.2-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.2.7.3 SR3 (THE RECORD KEY CLAUSE). A refusal: Data-name-1
      *> must not reference a group item that contains a variable
      *> occurrence data item, and line 9 below names the group IG, which
      *> holds an OCCURS DEPENDING ON table. WiseOwl refuses it at 85
      *> and 2002 with COBOL0863 (kb/Work PB5445); a compiler that accepts
      *> it fails. COBOL-2002 states the same refusal as §2002:12.3.4.12.2 SR3:
      *> a group holding a variable-table OCCURS is a variable-length data
      *> item (§2002:13.16.36.3 GR8). 2014 and 2023 narrow that term, so
      *> the program translates there.
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. RKODO.
000300 ENVIRONMENT DIVISION.
000400 INPUT-OUTPUT SECTION.
000500 FILE-CONTROL.
000600     SELECT IXF ASSIGN TO "RKODO.DAT"
000700         ORGANIZATION IS INDEXED
000800         ACCESS MODE IS DYNAMIC
000900           RECORD KEY IS IG.
001000 DATA DIVISION.
001100 FILE SECTION.
001200 FD  IXF.
001300 01  IXR.
001400     05  IK PIC X(3).
001500     05  IA PIC X(2).
001600     05  IG.
001700         10  IGN PIC 9.
001800         10  IGT PIC X OCCURS 1 TO 5 TIMES DEPENDING ON IGN.
001900 PROCEDURE DIVISION.
002000 MAIN-PARA.
002100     OPEN OUTPUT IXF.
002200     CLOSE IXF.
002300     DISPLAY "DONE".
002400     STOP RUN.
