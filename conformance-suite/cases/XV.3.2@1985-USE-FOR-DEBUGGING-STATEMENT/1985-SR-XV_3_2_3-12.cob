      *>--- conformance-case
      *> id: 1985-SR-XV_3_2_3-12
      *> rules: [1985-SR-XV.3.2.3-12]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XV.3.2.3 SR12 (THE USE FOR DEBUGGING STATEMENT). A
      *> refusal: the rule forbids line 12 of the program below, and
      *> WiseOwl refuses it at 85 with COBOL6334 there; a compiler that
      *> accepts it fails. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000010 IDENTIFICATION DIVISION.
000020 PROGRAM-ID. DBGRM.
000030 ENVIRONMENT DIVISION.
000040 CONFIGURATION SECTION.
000050 SOURCE-COMPUTER. XYZ WITH DEBUGGING MODE.
000060 DATA DIVISION.
000070 WORKING-STORAGE SECTION.
000080 01  WS-A PIC X(4) VALUE "ABCD".
000090 PROCEDURE DIVISION.
000100 DECLARATIVES.
000110 DBG SECTION.
000120     USE FOR DEBUGGING ON WS-A (1:2).
000130 DBG-PARA.
000140     DISPLAY "DEBUG".
000150 END DECLARATIVES.
000160 MAIN-SECT SECTION.
000170 MAIN-PARA.
000180     MOVE "WXYZ" TO WS-A.
000190     STOP RUN.
