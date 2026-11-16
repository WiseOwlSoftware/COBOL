      *>--- conformance-case
      *> id: g85_f04_02
      *> rules: [1985-GR-VI.4.4.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.4.4.4 GR3 - "When the OBJECT-COMPUTER paragraph is not
      *> specified and the program is not contained within a program
      *> including an OBJECT-COMPUTER paragraph, the object computer is
      *> defined by the implementor." WiseOwl defines one object computer,
      *> the .NET runtime the program is compiled for (docs/conformance
      *> DOC-A.1-127). The obligation a compiler can fail: a program with
      *> no OBJECT-COMPUTER paragraph (here no Environment Division at
      *> all), and a program contained in it that has none either, both
      *> translate and run as written. A compiler that demanded the
      *> paragraph, or failed to run the contained program, fails.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F0402.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-A PIC 99 VALUE 7.
       PROCEDURE DIVISION.
       MAIN-PARA.
           ADD 5 TO W-A.
           DISPLAY "OUTER " W-A.
           CALL "G85F0402I".
           DISPLAY "BACK".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F0402I.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-B PIC 99 VALUE 30.
       PROCEDURE DIVISION.
       INNER-PARA.
           SUBTRACT 4 FROM W-B.
           DISPLAY "INNER " W-B.
           EXIT PROGRAM.
       END PROGRAM G85F0402I.
       END PROGRAM G85F0402.
