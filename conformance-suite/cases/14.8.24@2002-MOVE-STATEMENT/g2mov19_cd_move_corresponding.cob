      *>--- conformance-case
      *> id: g2mov19_cd_move_corresponding
      *> rules: [2002-SR-14.8.24.2-10, 2002-SR-14.8.24.2-11]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> MOVE CORRESPONDING whose identifier-3 or identifier-4 is a
      *> group of an output CD's record (§2002:A.1.2.2 SR8, SR9; its
      *> VALUE clauses take effect, §2002:13.16.61.3 GR4). The facility
      *> draws COBOL0903. Every cite.py --check here is OK.
      *> §2002:14.8.24.2 SR10 - identifier-3 and identifier-4 are group
      *>   items, not reference modified: a CD group as sender (M1), as
      *>   receiver (M2) and both (M3) is accepted. Fails if a CD group
      *>   operand is refused. (The prohibited elementary operand is
      *>   g2mov20.)
      *> §2002:14.8.24.2 SR11 - the corresponding items of
      *>   identifier-3 are sent, those of identifier-4 receive, and
      *>   the pairs follow §2002:14.6.5: F1, F2, F7 pair (rule 1);
      *>   F3 does not (rule 2: alphabetic to numeric is "No" in
      *>   Table 17); F5 does not (rule 4: OCCURS); F6 does not
      *>   (rule 1: qualified by SUB in CG, not in WG); the FILLER and
      *>   F9 have no partner. M1S shows the sender unchanged.
      *>   M1: ABC to X(4) 'ABC ', 012 to 9(5) 00012, '77'; F3 55,
      *>   F5 ##, F6 # untouched. M2: 'hi' to X(5), 34 to 9(4) 0034,
      *>   '78Z' to X(2) '78'. M3: 'ABC  ', 0012, '77'.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2MOV19.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WG.
          05 F2 PIC 9(5) VALUE 99999.
          05 F1 PIC X(4) VALUE "####".
          05 F3 PIC 9(2) VALUE 55.
          05 F5 PIC X(2) VALUE "##".
          05 F6 PIC X VALUE "#".
          05 F7 PIC X(2) VALUE "##".
       01 WH.
          05 F1 PIC X(2) VALUE "hi".
          05 F2 PIC 9(2) VALUE 34.
          05 F7 PIC X(3) VALUE "78Z".
          05 F9 PIC X VALUE "9".
       COMMUNICATION SECTION.
       CD CDO FOR OUTPUT.
       01 CR.
          05 CG.
             10 F1 PIC X(3) VALUE "ABC".
             10 F2 PIC 9(3) VALUE 12.
             10 F3 PIC A(2) VALUE "XY".
             10 FILLER PIC X VALUE "*".
             10 F5 PIC X OCCURS 2.
             10 SUB.
                15 F6 PIC X VALUE "S".
             10 F7 PIC X(2) VALUE "77".
          05 CK.
             10 F1 PIC X(5).
             10 F2 PIC 9(4).
             10 F7 PIC X(2).
       PROCEDURE DIVISION.
       MAIN.
           MOVE "O" TO F5 OF CG (1) F5 OF CG (2)
           MOVE CORRESPONDING CG TO WG
           DISPLAY "M1 [" F1 OF WG "] " F2 OF WG " " F3 OF WG " "
               F5 OF WG " " F6 OF WG " " F7 OF WG
           DISPLAY "M1S " F1 OF CG F2 OF CG F3 OF CG F7 OF CG
           MOVE ALL "=" TO CK
           MOVE CORR WH TO CK
           DISPLAY "M2 [" F1 OF CK "] " F2 OF CK " " F7 OF CK
           MOVE ALL "=" TO CK
           MOVE CORR CG TO CK
           DISPLAY "M3 [" F1 OF CK "] " F2 OF CK " " F7 OF CK
           STOP RUN.
