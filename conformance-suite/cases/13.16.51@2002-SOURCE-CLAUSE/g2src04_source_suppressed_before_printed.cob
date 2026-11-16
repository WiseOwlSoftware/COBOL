      *>--- conformance-case
      *> id: g2src04_source_suppressed_before_printed
      *> rules: [2002-GR-13.16.51.3-4]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.51.3 GR4: "SOURCE operands are nevertheless assigned
      *> to them, even though they are not printed".  A multiple LINE
      *> entry (two lines) over a COLUMN entry OCCURS 1 TO 2 DEPENDING ON
      *> WS-N: four repeating printable items L1C1 L1C2 L2C1 L2C2, taken
      *> horizontally then vertically, get CA CB CC CE.  With WS-N = 1,
      *> L1C2 and L2C2 are suppressed but still take CB and CE, so the
      *> lines are [AAA] and [CCC] (skipping suppressed items: [AAA] [BBB]).
      *> Legal at 2002: §2002:13.16.36.2 SR9 restricts only an OCCURS
      *> clause subordinate to an entry "that contains another OCCURS
      *> clause"; a multiple LINE entry contains none, and §2002:13.13.3
      *> GR3 names the two forms apart ("an OCCURS clause or a LINE or
      *> COLUMN clause with more than one operand"). kb/Work PB5651: the
      *> binder counted the multiple LINE's repetitions as an OCCURS clause
      *> and refused the entry COBOL2021.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SRC04.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-X0A SYM-X0D
               ARE 11 14.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "g2src04.txt".
           SELECT CHK ASSIGN TO "g2src04.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R1.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-I    PIC 99    VALUE 0.
       01  WS-N    PIC 9     VALUE 1.
       01  WS-LINE PIC X(40) VALUE SPACES.
       01  CA      PIC X(3)  VALUE "AAA".
       01  CB      PIC X(3)  VALUE "BBB".
       01  CC      PIC X(3)  VALUE "CCC".
       01  CE      PIC X(3)  VALUE "DDD".
       REPORT SECTION.
       RD  R1.
       01  DMULTI TYPE DE.
           02  LINE PLUS 1 PLUS 1.
               03  COLUMN 1 PIC X(3) OCCURS 1 TO 2 TIMES
                   DEPENDING ON WS-N STEP 4
                   SOURCES ARE CA CB CC CE.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT PRT.
           INITIATE R1.
           GENERATE DMULTI.
           TERMINATE R1.
           CLOSE PRT.
           OPEN INPUT CHK.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHK
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHK.
           PERFORM SHOW-LINE.
           STOP RUN.
       TAKE-BYTE.
           IF CHK-REC = SYM-X0A
               PERFORM SHOW-LINE
           ELSE
               IF CHK-REC NOT = SYM-X0D
                   ADD 1 TO WS-I
                   MOVE CHK-REC TO WS-LINE(WS-I:1)
               END-IF
           END-IF.
       SHOW-LINE.
           IF WS-I > 0
               DISPLAY "[" WS-LINE(1:WS-I) "]"
               MOVE SPACES TO WS-LINE
               MOVE 0 TO WS-I
           END-IF.
