      *>--- conformance-case
      *> id: 1985-SR-VI_6_12_3-1
      *> rules: [1985-SR-VI.6.12.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.12.3 SR1 (THE ENTER STATEMENT): "Language-name-1 is
      *> specified by the implementor". A refusal: WiseOwl specifies no
      *> language, so the ENTER of the program below, which names one, is
      *> refused at 85 with COBOL7240 rather than compiled to nothing, which
      *> ran the program as if the other-language routine had run (kb/Work
      *> PB5308).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. ENTSR1.
000300 PROCEDURE DIVISION.
000400 MAIN-PARA.
000500     ENTER FORTRAN SUBRTN.
000600     DISPLAY "DONE".
000700     STOP RUN.
