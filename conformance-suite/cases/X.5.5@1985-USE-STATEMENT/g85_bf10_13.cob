      *>--- conformance-case
      *> id: g85_bf10_13
      *> rules: [1985-GR-X.5.5.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.5.5.4 GR1 - "only the first qualifying declarative will be
      *> selected for execution", in the order a. the program containing the
      *> statement, b. a GLOBAL declarative in the directly containing program,
      *> c. rule b applied outward to the outermost program.
      *> G85BF1013 (outer) owns the GLOBAL files GF and GG and has GLOBAL
      *> declaratives for both; G85BF1013M (in it) has a NON-global declarative
      *> for GF and a GLOBAL one for GG; G85BF1013N (in M) has none. Each READ
      *> past the one record is the at end condition, I-O status 10.
      *>   MID-L-GF 10   : M's own statement - rule a, M's own declarative.
      *>   OUTER-G-GF 10 : N's statement on GF - N has none, M's is not GLOBAL
      *>                   (rule b fails at M), rule c reaches the outer GLOBAL.
      *>   MID-G-GG 10   : N's statement on GG - rule b selects M's GLOBAL one,
      *>                   and only it (the outer GLOBAL GG declarative is not).
      *>   OUTER-G-GG 10 : the outer program's own statement - rule a.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF1013.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT GF ASSIGN TO "bf1013f.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS GF-ST.
           SELECT GG ASSIGN TO "bf1013g.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS GG-ST.
       DATA DIVISION.
       FILE SECTION.
       FD  GF GLOBAL
           LABEL RECORDS ARE STANDARD.
       01  GF-REC                  PIC X(4).
       FD  GG GLOBAL
           LABEL RECORDS ARE STANDARD.
       01  GG-REC                  PIC X(4).
       WORKING-STORAGE SECTION.
       01  GF-ST                   PIC XX GLOBAL.
       01  GG-ST                   PIC XX GLOBAL.
       PROCEDURE DIVISION.
       DECLARATIVES.
       O-GF SECTION.
           USE GLOBAL AFTER STANDARD ERROR PROCEDURE ON GF.
       O-GF-P.
           DISPLAY "OUTER-G-GF " GF-ST.
       O-GG SECTION.
           USE GLOBAL AFTER STANDARD ERROR PROCEDURE ON GG.
       O-GG-P.
           DISPLAY "OUTER-G-GG " GG-ST.
       END DECLARATIVES.
       O-MAIN SECTION.
       O-1.
           OPEN OUTPUT GF.
           MOVE "F001" TO GF-REC.
           WRITE GF-REC.
           CLOSE GF.
           OPEN OUTPUT GG.
           MOVE "G001" TO GG-REC.
           WRITE GG-REC.
           CLOSE GG.
           CALL "G85BF1013M".
           OPEN INPUT GG.
           READ GG.
           READ GG.
           CLOSE GG.
           DISPLAY "OUTER END".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF1013M.
       PROCEDURE DIVISION.
       DECLARATIVES.
       M-GF SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON GF.
       M-GF-P.
           DISPLAY "MID-L-GF " GF-ST.
       M-GG SECTION.
           USE GLOBAL AFTER STANDARD ERROR PROCEDURE ON GG.
       M-GG-P.
           DISPLAY "MID-G-GG " GG-ST.
       END DECLARATIVES.
       M-MAIN SECTION.
       M-1.
           OPEN INPUT GF.
           READ GF.
           READ GF.
           CLOSE GF.
           CALL "G85BF1013N".
           EXIT PROGRAM.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF1013N.
       PROCEDURE DIVISION.
       N-1.
           OPEN INPUT GF.
           READ GF.
           READ GF.
           CLOSE GF.
           OPEN INPUT GG.
           READ GG.
           READ GG.
           CLOSE GG.
           EXIT PROGRAM.
       END PROGRAM G85BF1013N.
       END PROGRAM G85BF1013M.
       END PROGRAM G85BF1013.
