      *>--- conformance-case
      *> id: g85_bf11_02
      *> rules: [1985-SR-VI.5.8.3-13]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.8.3 SR13 - "An INDEXED BY phrase is required if the
      *> subject of this entry, or an entry subordinate to this entry, is
      *> to be referenced by indexing." TN's OCCURS entry has no INDEXED
      *> BY phrase, yet TN is referenced by indexing (with IA, the
      *> index-name of the other table TA). The rule is violated, so a
      *> 1985 compiler must refuse the reference at line 001300.
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. G85BF1102.
000300 DATA DIVISION.
000400 WORKING-STORAGE SECTION.
000500 01  TAR.
000600     05  TA    PIC X OCCURS 4 TIMES INDEXED BY IA.
000700 01  TNR.
000800     05  TN    PIC X OCCURS 4 TIMES.
000900 PROCEDURE DIVISION.
001000 MAIN-PARA.
001100     SET IA TO 2.
001200     MOVE "A" TO TA (IA).
001300     MOVE "B" TO TN (IA).
001400     DISPLAY TN (2).
001500     STOP RUN.
