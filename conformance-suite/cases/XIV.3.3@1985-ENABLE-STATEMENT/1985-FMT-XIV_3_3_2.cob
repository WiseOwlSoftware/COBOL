      *>--- conformance-case
      *> id: 1985-FMT-XIV_3_3_2
      *> rules: [1985-FMT-XIV.3.3.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.3.3.2 (THE ENABLE STATEMENT). A refusal: the rule
      *> forbids line 13 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL0901 there; a compiler that accepts it fails.
      *> Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. ENABT.
000300 DATA DIVISION.
000400 COMMUNICATION SECTION.
000500 CD  OUT-CD FOR OUTPUT
000600     DESTINATION COUNT IS OUT-COUNT
000700     TEXT LENGTH IS OUT-LEN
000800     STATUS KEY IS OUT-STATUS
000900     ERROR KEY IS OUT-ERR
001000     SYMBOLIC DESTINATION IS OUT-DEST.
001100 PROCEDURE DIVISION.
001200 MAIN-PARA.
001300     ENABLE OUTPUT TERMINAL OUT-CD.
001400     STOP RUN.
