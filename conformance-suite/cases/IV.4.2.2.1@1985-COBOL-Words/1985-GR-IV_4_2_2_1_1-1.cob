      *>--- conformance-case
      *> id: 1985-GR-IV_4_2_2_1_1-1
      *> rules: [1985-GR-IV.4.2.2.1.1-1, 1985-GR-IV.4.2.2.1.1-10, 1985-GR-IV.4.2.2.1.1-11, 1985-GR-IV.4.2.2.1.1-12, 1985-GR-IV.4.2.2.1.1-13, 1985-GR-IV.4.2.2.1.1-16, 1985-GR-IV.4.2.2.1.1-17, 1985-GR-IV.4.2.2.1.1-18, 1985-GR-IV.4.2.2.1.1-3, 1985-GR-IV.4.2.2.1.1-4, 1985-GR-IV.4.2.2.1.1-5, 1985-GR-IV.4.2.2.1.1-6, 1985-GR-IV.4.2.2.1.1-7, 1985-GR-IV.4.2.2.1.1-8]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.2.2.1.1 GR1; §1985:IV.4.2.2.1.1 GR10;
      *> §1985:IV.4.2.2.1.1 GR11; §1985:IV.4.2.2.1.1 GR12;
      *> §1985:IV.4.2.2.1.1 GR13; §1985:IV.4.2.2.1.1 GR16;
      *> §1985:IV.4.2.2.1.1 GR17; §1985:IV.4.2.2.1.1 GR18;
      *> §1985:IV.4.2.2.1.1 GR3; §1985:IV.4.2.2.1.1 GR4;
      *> §1985:IV.4.2.2.1.1 GR5; §1985:IV.4.2.2.1.1 GR6;
      *> §1985:IV.4.2.2.1.1 GR7; §1985:IV.4.2.2.1.1 GR8 (COBOL Words).
      *> An output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-02.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * s-02 probe: 1985-GR-IV.4.2.2.1.1-1..19 each type of user-
      * defined word, spelled with digits and hyphens as permitted by
      * §1985:IV.4.2.2.1.1 p1, is accepted where its format needs it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. UDW-TYPES-1.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SWITCH-1 IS SW-1 ON STATUS IS SW-1-ON
           ALPHABET ALPHA-2 IS NATIVE
           SYMBOLIC CHARACTERS SYM-3 IS 66
           CLASS CLS-4 IS "A" THRU "C".
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FILE-5 ASSIGN TO "UDWT5.DAT".
       DATA DIVISION.
       FILE SECTION.
       FD  FILE-5.
       01  REC-6                PIC X(4).
       WORKING-STORAGE SECTION.
       01  DATA-7               PIC X(4) VALUE "ABCA".
           88  COND-8           VALUE "ABCA".
       01  TAB-9.
           05  ELEM-10 PIC X OCCURS 3 TIMES INDEXED BY IDX-11.
       PROCEDURE DIVISION.
       SEC-12 SECTION.
       PARA-13.
           IF COND-8 DISPLAY "COND-8".
           IF DATA-7 IS CLS-4 DISPLAY "CLS-4".
           DISPLAY SYM-3.
           SET IDX-11 TO 2.
           MOVE "X" TO ELEM-10 (IDX-11).
           DISPLAY TAB-9.
           IF SW-1-ON DISPLAY "SW-ON" ELSE DISPLAY "SW-OFF".
           PERFORM 0014 OF 0015.
           STOP RUN.
       0015 SECTION 60.
       0014.
           DISPLAY "SEG-60".
