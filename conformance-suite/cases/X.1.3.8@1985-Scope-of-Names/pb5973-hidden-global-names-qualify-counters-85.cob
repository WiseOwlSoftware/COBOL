      *>--- conformance-case
      *> id: pb5973-hidden-global-names-qualify-counters-85
      *> rules: [1985-GR-X.1.3.8.2-1, 1985-GR-X.1.3.8.2-2, 1985-GR-X.1.3.8.2-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5973 review R1-1. §1985:X.1.3.8.2 GR1 (cite.py: OK):
      *> over program B's names and its containers' global names "the
      *> normal rules for qualification and any other rules for
      *> uniqueness of reference are applied until one or more objects
      *> is identified"; GR2 (cite.py: OK): "If only one object is
      *> identified, it is the referenced object." The container's RD
      *> R1 is GLOBAL, so its LINE-COUNTER and PAGE-COUNTER are global
      *> names (§1985:X.4.4.4 GR1, cite.py: OK); its FD LF is GLOBAL
      *> with a LINAGE clause, so its LINAGE-COUNTER is a global name
      *> (§1985:X.4.2.4 GR1, cite.py: OK). P73QI declares its own data
      *> items R1 and LF, which an unqualified R1 or LF names there
      *> (GR3a, cite.py: OK: "If the name is declared in program B, the
      *> object in program B is the referenced object"), but only the
      *> container's report R1 and file LF have counters, so
      *> PAGE-COUNTER OF R1, LINE-COUNTER OF R1 and LINAGE-COUNTER OF LF
      *> each identify one object (GR2). After INITIATE R1 "LINE-COUNTER
      *> is set to zero" and "PAGE-COUNTER is set to one"
      *> (§1985:XIII.4.4.4 GR1, cite.py: OK); after OPEN OUTPUT LF "The
      *> value of LINAGE-COUNTER is automatically set to one"
      *> (§1985:VII.3.7.4 GR9, cite.py: OK). A compiler that resolves a
      *> counter's qualifier only among the names a reference to it
      *> could name refuses the three qualified counters.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P73QO.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPF ASSIGN TO "p73qr.rpt".
           SELECT LF ASSIGN TO "p73ql.dat".
       DATA DIVISION.
       FILE SECTION.
       FD RPF GLOBAL REPORT IS R1.
       FD LF GLOBAL LINAGE IS 10 LINES.
       01 LREC PIC X(4).
       WORKING-STORAGE SECTION.
       01 WS-N PIC 9 VALUE 0 GLOBAL.
       REPORT SECTION.
       RD R1 GLOBAL PAGE LIMIT 20.
       01 DET TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC 9 SOURCE WS-N.
       PROCEDURE DIVISION.
       P-1.
           OPEN OUTPUT RPF LF
           INITIATE R1
           CALL "P73QI"
           TERMINATE R1
           CLOSE RPF LF
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P73QI.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R1 PIC X VALUE "L".
       01 LF PIC X VALUE "M".
       PROCEDURE DIVISION.
       I-1.
           IF PAGE-COUNTER OF R1 = 1
               DISPLAY "I PC=1" ELSE DISPLAY "I PC WRONG".
           IF LINE-COUNTER OF R1 = 0
               DISPLAY "I LC=0" ELSE DISPLAY "I LC WRONG".
           IF LINAGE-COUNTER OF LF = 1
               DISPLAY "I LGC=1" ELSE DISPLAY "I LGC WRONG".
           DISPLAY "I R1=" R1
           DISPLAY "I LF=" LF
           EXIT PROGRAM.
       END PROGRAM P73QI.
       END PROGRAM P73QO.
