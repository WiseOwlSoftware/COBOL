      *>--- conformance-case
      *> id: g85_bf11_04
      *> rules: [1985-SR-VI.5.9.3-6]
      *> accept: [85]
      *> source-format: fixed
      *>---
      *> §1985:VI.5.9.3 SR6 - "PIC is an abbreviation for PICTURE." The
      *> abbreviation is the same clause, so PIC and PICTURE (each with
      *> and without IS) describe identical items. WA and WB are X(4):
      *> "ABCDEFG" moved in is truncated on the right to 4 characters
      *> [ABCD] (VI.6.19.4 GR4a). WC and WD are ZZ9.99: 12.34 aligns as
      *> 012.34 and the leading zero is suppressed to a space, [ 12.34]
      *> (VI.5.9.5 GR1a). An abbreviation refused,
      *> or bound to a different character-string, changes one side of
      *> a pair.
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. G85BF1104.
000300 DATA DIVISION.
000400 WORKING-STORAGE SECTION.
000500 01  WA    PIC X(4).
000600 01  WB    PICTURE X(4).
000700 01  WC    PIC IS ZZ9.99.
000800 01  WD    PICTURE IS ZZ9.99.
000900 PROCEDURE DIVISION.
001000 MAIN-PARA.
001100     MOVE "ABCDEFG" TO WA.
001200     MOVE "ABCDEFG" TO WB.
001300     MOVE 12.34 TO WC.
001400     MOVE 12.34 TO WD.
001500     DISPLAY "A=[" WA "]".
001600     DISPLAY "B=[" WB "]".
001700     DISPLAY "C=[" WC "]".
001800     DISPLAY "D=[" WD "]".
001900     STOP RUN.
