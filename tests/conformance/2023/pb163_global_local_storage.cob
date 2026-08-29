      *> kb/Work PB163 - ISO 13.18.27.3 SR1b: GLOBAL may be specified on a level-1 entry in the file,
      *> working-storage, LOCAL-STORAGE, or linkage section. The registration pass scanned WS only, so a
      *> GLOBAL LOCAL-STORAGE item was UNDEFINED in every containee (COBOLNET1639 on conforming source).
      *> Derived: the containee reads the container's per-activation LS through the containment bridge
      *> (13.18.27 GR2) - SEES=0007.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. GLSP.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 LS-X GLOBAL PIC 9(4) VALUE 7.
       PROCEDURE DIVISION.
       MAIN.
           CALL "GLSC"
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. GLSC.
       PROCEDURE DIVISION.
       P.
           DISPLAY "SEES=" LS-X
           GOBACK.
       END PROGRAM GLSC.
       END PROGRAM GLSP.
