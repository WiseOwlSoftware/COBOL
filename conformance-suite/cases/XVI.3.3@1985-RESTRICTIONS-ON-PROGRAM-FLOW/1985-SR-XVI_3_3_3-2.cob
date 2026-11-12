      *>--- conformance-case
      *> id: 1985-SR-XVI_3_3_3-2
      *> rules: [1985-SR-XVI.3.3.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XVI.3.3.3 SR2 (RESTRICTIONS ON PROGRAM FLOW). A refusal:
      *> the rule forbids line 27 of the program below, and WiseOwl
      *> refuses it at 85 with COBOL6877 there; a compiler that accepts
      *> it fails. Placed from written and run by the 1985 draft triage
      *> (witnesses-85k): a draft that still mismatched in rerun-85g,
      *> corrected from the rule's text and run to MATCH by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. SEGMRG.
000300 ENVIRONMENT DIVISION.
000400 INPUT-OUTPUT SECTION.
000500 FILE-CONTROL.
000600     SELECT IN1 ASSIGN TO "IN1".
000700     SELECT IN2 ASSIGN TO "IN2".
000800     SELECT SD-FILE ASSIGN TO "SDWORK".
000900 DATA DIVISION.
001000 FILE SECTION.
001100 FD  IN1.
001200 01  IN1-REC PIC X(10).
001300 FD  IN2.
001400 01  IN2-REC PIC X(10).
001500 SD  SD-FILE.
001600 01  SD-REC.
001700     05 SD-KEY PIC X(10).
001800 PROCEDURE DIVISION.
001900 P0 SECTION.
002000 P0-A.
002100     PERFORM MRG-SEC.
002200     STOP RUN.
002300 MRG-SEC SECTION 60.
002400 MRG-A.
002500     MERGE SD-FILE ON ASCENDING KEY SD-KEY
002600         USING IN1 IN2
002700         OUTPUT PROCEDURE IS OUT-SEC.
002800 OUT-SEC SECTION 70.
002900 OUT-A.
003000     RETURN SD-FILE AT END GO TO OUT-X.
003100     GO TO OUT-A.
003200 OUT-X.
003300     EXIT.
