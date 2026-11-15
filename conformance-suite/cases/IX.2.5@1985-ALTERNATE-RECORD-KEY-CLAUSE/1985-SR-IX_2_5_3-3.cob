      *>--- conformance-case
      *> id: 1985-SR-IX_2_5_3-3
      *> rules: [1985-SR-IX.2.5.3-3, 2002-SR-12.3.4.5.2-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.2.5.3 SR3 (THE ALTERNATE RECORD KEY CLAUSE). A refusal: Data-name-1
      *> must not reference a group item that contains a variable
      *> occurrence data item, and line 10 below names the group IG, which
      *> holds an OCCURS DEPENDING ON table. WiseOwl refuses it at 85
      *> and 2002 with COBOL0863 (kb/Work PB5445); a compiler that accepts
      *> it fails. COBOL-2002 states the same refusal as §2002:12.3.4.5.2 SR3:
      *> a group holding a variable-table OCCURS is a variable-length data
      *> item (§2002:13.16.36.3 GR8). 2014 and 2023 narrow that term, so
      *> the program translates there.
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. AKODO.
000300 ENVIRONMENT DIVISION.
000400 INPUT-OUTPUT SECTION.
000500 FILE-CONTROL.
000600     SELECT IXF ASSIGN TO "AKODO.DAT"
000700         ORGANIZATION IS INDEXED
000800         ACCESS MODE IS DYNAMIC
000900           RECORD KEY IS IK
001000           ALTERNATE RECORD KEY IS IG.
001100 DATA DIVISION.
001200 FILE SECTION.
001300 FD  IXF.
001400 01  IXR.
001500     05  IK PIC X(3).
001600     05  IA PIC X(2).
001700     05  IG.
001800         10  IGN PIC 9.
001900         10  IGT PIC X OCCURS 1 TO 5 TIMES DEPENDING ON IGN.
002000 PROCEDURE DIVISION.
002100 MAIN-PARA.
002200     OPEN OUTPUT IXF.
002300     CLOSE IXF.
002400     DISPLAY "DONE".
002500     STOP RUN.
