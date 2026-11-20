      *>--- conformance-case
      *> id: pb5973-global-sum-counter-qualified-85
      *> rules: [1985-GR-X.1.3.8.2-1, 1985-GR-X.1.3.8.2-2, 1985-GR-X.1.3.8.2-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5973. §1985:X.1.3.8.2 GR1 (cite.py: OK): over the set
      *> of program B's names and its containers' global names "the
      *> normal rules for qualification and any other rules for
      *> uniqueness of reference are applied until one or more objects
      *> is identified"; GR2 (cite.py: OK): "If only one object is
      *> identified, it is the referenced object."; GR3a (cite.py: OK):
      *> "If the name is declared in program B, the object in program B
      *> is the referenced object." The container's GLOBAL report R1
      *> has a sum counter TOT under CFG; P73SI declares its own data
      *> item TOT (77). An unqualified TOT in P73SI identifies both, and
      *> by GR3a names P73SI's own (77); TOT OF CFG identifies only the
      *> container's counter, which one DETAIL with WS-N 4 has summed to
      *> 4 (GR2). The container then reads its counter: 04. A compiler
      *> that drops the global counter because P73SI declares the
      *> spelling refuses TOT OF CFG at translation.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P73SO.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPF ASSIGN TO "p73s.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPF GLOBAL REPORT IS R1.
       WORKING-STORAGE SECTION.
       01 WS-N PIC 9 VALUE 0 GLOBAL.
       01 WS-T PIC 99 VALUE 0 GLOBAL.
       REPORT SECTION.
       RD R1 GLOBAL CONTROL FINAL.
       01 DET TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC 9 SOURCE WS-N.
       01 CFG TYPE CF FINAL LINE PLUS 1.
          02 TOT COLUMN 1 PIC 99 SUM WS-N.
       PROCEDURE DIVISION.
       P-1.
           OPEN OUTPUT RPF
           INITIATE R1
           MOVE 4 TO WS-N
           GENERATE DET
           CALL "P73SI"
           MOVE TOT TO WS-T
           DISPLAY "O TOT=" WS-T
           TERMINATE R1
           CLOSE RPF
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P73SI.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TOT PIC 99 VALUE 77.
       PROCEDURE DIVISION.
       I-1.
           DISPLAY "I TOT=" TOT
           MOVE TOT OF CFG TO WS-T
           DISPLAY "I TOTCFG=" WS-T
           EXIT PROGRAM.
       END PROGRAM P73SI.
       END PROGRAM P73SO.
