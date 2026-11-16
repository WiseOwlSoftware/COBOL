      *>--- conformance-case
      *> id: g85_bf11_01
      *> rules: [1985-SR-VI.5.8.3-13]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.8.3 SR13 - "An INDEXED BY phrase is required if the
      *> subject of this entry, or an entry subordinate to this entry, is
      *> to be referenced by indexing." The program references TA (whose
      *> OCCURS entry names IA), and TB-C, an entry SUBORDINATE to TB
      *> (whose OCCURS entry names IB), by indexing; both forms the rule
      *> grants must be accepted and must reach the right occurrence.
      *> SET IA TO 3 makes IA refer to occurrence 3 (VI.6.23.4 GR3a)
      *> of TA (elements "AA" "BB" "CC" "DD"), so TA (IA) = "CC".
      *> SET IB TO 2: TB-C (IB) is the TB-C of occurrence 2 of TB
      *> (elements "1x" "2y" "3z"), so "y". TA (IA + 1) is relative
      *> indexing (II.4.4.2): occurrence 4 = "DD". A leg fails if
      *> the INDEXED BY entry is refused, or the index addresses another
      *> occurrence or another element size.
      *> The refusal twin is g85_bf11_02.
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. G85BF1101.
000300 DATA DIVISION.
000400 WORKING-STORAGE SECTION.
000500 01  TAV       PIC X(8) VALUE "AABBCCDD".
000600 01  TAR REDEFINES TAV.
000700     05  TA    PIC XX OCCURS 4 TIMES INDEXED BY IA.
000800 01  TBV       PIC X(6) VALUE "1x2y3z".
000900 01  TBR REDEFINES TBV.
001000     05  TB    OCCURS 3 TIMES INDEXED BY IB.
001100         10  TB-N  PIC X.
001200         10  TB-C  PIC X.
001300 PROCEDURE DIVISION.
001400 MAIN-PARA.
001500     SET IA TO 3.
001600     SET IB TO 2.
001700     DISPLAY "TA=" TA (IA).
001800     DISPLAY "TB-C=" TB-C (IB).
001900     DISPLAY "REL=" TA (IA + 1).
002000     STOP RUN.
