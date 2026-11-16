      *>--- conformance-case
      *> id: g85_f02_07
      *> rules: [1985-GR-X.5.3.4-8]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> After CANCEL "G85F02SK" it and its contained G85F02KA and
      *> G85F02KB restart (K1 A1 B1; §1985:X.5.2.4 GR5, context only:
      *> g85_f02_10 witnesses X.5.3.4 GR4). §1985:X.5.3.4 GR8 -
      *> CANCEL of the never-called G85F02NV and a second CANCEL of
      *> the cancelled G85F02SK take no
      *> action; control reaches the next statement (AFTER lines); the
      *> later first call of G85F02NV finds its VALUE (NV 1).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F0207.
       PROCEDURE DIVISION.
       P-MAIN.
           CANCEL "G85F02NV".
           DISPLAY "AFTER CANCEL NV".
           CALL "G85F02SK".
           CALL "G85F02SK".
           CANCEL "G85F02SK".
           DISPLAY "AFTER CANCEL SK".
           CANCEL "G85F02SK".
           DISPLAY "AFTER CANCEL SK AGAIN".
           CALL "G85F02SK".
           CALL "G85F02NV".
           STOP RUN.
       END PROGRAM G85F0207.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F02SK.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N   PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       P-SK.
           ADD 1 TO N.
           DISPLAY "K" N.
           CALL "G85F02KA".
           CALL "G85F02KB".
           EXIT PROGRAM.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F02KA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N   PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       P-KA.
           ADD 1 TO N.
           DISPLAY "A" N.
           EXIT PROGRAM.
       END PROGRAM G85F02KA.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F02KB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N   PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       P-KB.
           ADD 1 TO N.
           DISPLAY "B" N.
           EXIT PROGRAM.
       END PROGRAM G85F02KB.
       END PROGRAM G85F02SK.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F02NV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N   PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       P-NV.
           ADD 1 TO N.
           DISPLAY "NV " N.
           EXIT PROGRAM.
       END PROGRAM G85F02NV.
