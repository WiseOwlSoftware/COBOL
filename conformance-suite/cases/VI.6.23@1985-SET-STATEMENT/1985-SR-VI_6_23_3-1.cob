      *>--- conformance-case
      *> id: 1985-SR-VI_6_23_3-1
      *> rules: [1985-SR-VI.6.23.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.23.3 SR1 (THE SET STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. SETREC.
000300 DATA DIVISION.
000400 WORKING-STORAGE SECTION.
000500 01  TBL.
000600     05  ELT PIC X OCCURS 5 TIMES INDEXED BY IX-1 IX-2.
000700 01  N1 PIC 9 VALUE 0.
000800 01  N2 PIC 9 VALUE 0.
000900 PROCEDURE DIVISION.
001000 MAIN-PARA.
001100     SET IX-1 IX-2 TO 3.
001200     SET IX-1 IX-2 UP BY 1.
001300     SET N1 N2 TO IX-2.
001400     DISPLAY N1 " " N2.
001500     SET N1 TO IX-1.
001600     DISPLAY N1.
001700     STOP RUN.
