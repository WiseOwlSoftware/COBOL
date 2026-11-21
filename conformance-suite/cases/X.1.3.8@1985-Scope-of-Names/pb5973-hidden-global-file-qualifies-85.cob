      *>--- conformance-case
      *> id: pb5973-hidden-global-file-qualifies-85
      *> rules: [1985-GR-X.1.3.8.2-1, 1985-GR-X.1.3.8.2-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5973. §1985:X.1.3.8.2 GR1 (cite.py: OK): the set of
      *> names is every name of program B and every global name of
      *> its containers, and "the normal rules for qualification and
      *> any other rules for uniqueness of reference are applied
      *> until one or more objects is identified"; GR2 (cite.py: OK):
      *> "If only one object is identified, it is the referenced
      *> object." P73FI declares its own file FA, which hides the
      *> container's GLOBAL FA as a file reference, but qualification
      *> runs over the whole set first: FX OF FREC OF FA reaches only
      *> the container's FX (P73FI's FA holds BREC), so it names that
      *> item, and MOVE "QQQ" changes the container's record. G and
      *> CV are declared by both: an unqualified G is P73FI's own (LOC),
      *> X OF G only the container's subordinate (OUX), and C OF CV only
      *> the container's level-88 (CV is 1). After the CALL the
      *> container shows its record: QQQ. A compiler that resolves the
      *> file qualifier only among the files a reference to FA could
      *> name refuses FX OF FREC OF FA at translation.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P73FO.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FA ASSIGN TO "p73fa.dat".
       DATA DIVISION.
       FILE SECTION.
       FD FA GLOBAL.
       01 FREC.
          05 FX PIC X(3).
       WORKING-STORAGE SECTION.
       01 G GLOBAL.
          05 X PIC X(3) VALUE "OUX".
       01 CV PIC 9 VALUE 1 GLOBAL.
          88 C VALUE 1.
       PROCEDURE DIVISION.
       P-1.
           OPEN OUTPUT FA
           MOVE "OLD" TO FX
           CALL "P73FI"
           DISPLAY "O FREC=" FREC
           CLOSE FA
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P73FI.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FA ASSIGN TO "p73fb.dat".
       DATA DIVISION.
       FILE SECTION.
       FD FA.
       01 BREC PIC X(3).
       WORKING-STORAGE SECTION.
       01 G PIC X(3) VALUE "LOC".
       01 CV PIC X VALUE "Z".
       PROCEDURE DIVISION.
       I-1.
           DISPLAY "I G=" G
           DISPLAY "I XG=" X OF G
           IF C OF CV DISPLAY "I C TRUE" ELSE DISPLAY "I C FALSE".
           MOVE "QQQ" TO FX OF FREC OF FA
           EXIT PROGRAM.
       END PROGRAM P73FI.
       END PROGRAM P73FO.
