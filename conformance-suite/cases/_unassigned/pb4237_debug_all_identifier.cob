      *>--- conformance-case
      *> id: pb4237_debug_all_identifier
      *> rules: []
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> X3.23-1985 USE FOR DEBUGGING ON [ALL REFERENCES OF] identifier-1 (VCR Table 7 row 7.17; kb/Work
      *> PB4237). Only ALL is a keyword in that bracket: REFERENCES and OF are optional words, so ALL W-B,
      *> ALL OF W-B, ALL REFERENCES W-B and ALL REFERENCES OF W-A all write the one operand. ISO/IEC
      *> 1989:2023 prints no debug format, so under CLAUDE.md rule 1 GnuCOBOL decides: cobc -std=cobol85
      *> accepts all four (docs/CONFORMANCE.md D-DEBUG-USE). DBG-1 and DBG-2 are CCVS DB201A's two
      *> USE statements, each with a second operand that omits REFERENCES OF.
      *> Why it can fail: a grammar that makes REFERENCES mandatory refuses ALL ID-2, ALL AB2 OF A2,
      *> ALL OF W-B and ALL W-B (the pre-fix COBOL5506).
      *> Expected output: SOURCE-COMPUTER carries no WITH DEBUGGING MODE, so each debugging section is
      *> compiled as if it were comment lines and never runs (VCR row 7.17's '85 posture): HITS stays 0
      *> and the main body's three MOVEs are all that change the data.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4237DBG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-A   PIC 9(3) VALUE 0.
       01  W-B   PIC 9(3) VALUE 0.
       01  A1.
           02  AB2.
               03  ABC1 PIC X VALUE "X".
       01  A2.
           02  AB2  PIC X VALUE "Y".
       01  ID-2  PIC X VALUE "Z".
       01  HITS  PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       DBG-1 SECTION.
           USE FOR DEBUGGING ON  ALL REFERENCES OF W-A
                                 ALL               ID-2.
       DBG-1-PARA.
           ADD 1 TO HITS.
       DBG-2 SECTION.
           USE FOR DEBUGGING ON ALL REFERENCES OF ABC1 OF AB2 OF A1
                                ALL                       AB2 OF A2.
       DBG-2-PARA.
           ADD 1 TO HITS.
       DBG-3 SECTION.
           USE FOR DEBUGGING ON ALL OF W-B  ALL REFERENCES W-B
                                ALL W-B.
       DBG-3-PARA.
           ADD 1 TO HITS.
       END DECLARATIVES.
       MAIN SECTION.
       P0.
           MOVE 7 TO W-A.
           MOVE 8 TO W-B.
           MOVE "Q" TO ID-2.
           DISPLAY "W-A=" W-A " W-B=" W-B " ID-2=" ID-2 " HITS=" HITS.
           STOP RUN.
