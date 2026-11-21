      *>--- conformance-case
      *> id: pb5924-reference-format-85
      *> rules: [1985-SR-IV.7.1-p3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:IV.7.1 (cite.py: OK): "Each division must be written according to the rules for the reference format"
      *> Every division in reference format: sequence numbers in 1-6, program-identification text in
      *> 73-80, comment and page-eject lines, and a nonnumeric literal continued with a hyphen.
000100 IDENTIFICATION DIVISION.                                         REFFMT01
A#Z9 x PROGRAM-ID. REFFMT.                                              REFFMT02
000300* A COMMENT LINE: MOVE 1 TO NOWHERE.                              REFFMT03
000400 ENVIRONMENT DIVISION.                                            REFFMT04
000500 CONFIGURATION SECTION.                                           REFFMT05
000600 SOURCE-COMPUTER. ANY-COMPUTER.                                   REFFMT06
000700 DATA DIVISION.                                                   REFFMT07
000800 WORKING-STORAGE SECTION.                                         REFFMT08
000900 01  W  PIC X(60) VALUE "CONTINUED-LITERAL-PART-ONE-              IDCOLS73
001000-    "AND-TWO".                                                   IDCOLS74
001100 01  N  PIC 9(3)                                                  XXXXXXXX
001200         VALUE 123.                                               99999999
001300/PAGE EJECT COMMENT LINE                                          REFFMT13
001400 PROCEDURE DIVISION.                                              REFFMT14
001500 P1.                                                              REFFMT15
001600     DISPLAY W.                                                   DISPLAY 
001700     DISPLAY N.                                                   STOP RUN
001800     STOP RUN.                                                    REFFMT18
