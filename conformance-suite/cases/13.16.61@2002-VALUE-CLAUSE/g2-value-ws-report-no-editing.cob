      *>--- conformance-case
      *> id: g2-value-ws-report-no-editing
      *> rules: [2002-GR-13.16.61.3-7, 2002-GR-13.16.61.3-8, 2002-SR-13.16.61.2-8]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> Format 1 and 2 VALUE clauses in working storage and format 4
      *> VALUE clauses in a report group, on JUSTIFIED, BLANK WHEN ZERO
      *> and edited items.
      *> WJ1 WJ   §2002:13.16.61.3 GR7 - "initialization is not affected
      *>          by a JUSTIFIED clause": X(4) JUSTIFIED RIGHT VALUE "AB"
      *>          is "AB  " (§2002:14.5.8 rule 5, left aligned), for the
      *>          elementary item (format 1) and for each occurrence of
      *>          the format 2 table. Fails if the value is right aligned.
      *> WN       §2002:13.16.61.3 GR7 - 1.5 FROM (1) in 9V99 OCCURS 2 is
      *>          aligned by decimal point (§2002:14.5.8 rule 1a): each
      *>          occurrence times 100 is 150.
      *> WB WZ WB1  §2002:13.16.61.3 GR8 - BLANK WHEN ZERO does not
      *>          blank the initial value: "000" stays 000 and ZERO fills
      *>          ZZ9 with 000 (§2002:8.3.1.2.5.3 GR2b), for format 1 and
      *>          format 2. Fails if the item shows spaces.
      *> WE WE1   §2002:13.16.61.2 SR8 - XX/XX is five positions and
      *>          takes "ABCD" unedited ("ABCD "), elementary and in a
      *>          table; WS1 PIC XX/XX VALUE "AB/CD" with its condition-
      *>          name WS1-OK VALUE "AB/CD" (format 3) is accepted - the
      *>          '/' counts in the size (SR4 size limit not exceeded) -
      *>          and WS1-OK is true for the initial value.
      *> RPT      §2002:13.16.61.3 GR7 and GR8, format 4 - the report
      *>          line holds X(5) JUSTIFIED RIGHT VALUE "AB" left aligned
      *>          in columns 1-5, ZZZ9 BLANK WHEN ZERO VALUE "0000" as
      *>          0000 in columns 8-11, XXBXX VALUE "ABCDE" unedited in
      *>          columns 14-18 and 999 VALUE 7 as 007 in columns 21-23
      *>          (§2002:13.16.61.3 GR18 - the literal is the content of
      *>          the printable item; SR2 and SR4 accepted format 4 legs).
      *>          The line is read back byte by byte so the record length
      *>          of the report file does not matter.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2VQ02.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "g2vq02.txt".
           SELECT CHK ASSIGN TO "g2vq02.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R1.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF PIC X VALUE "N".
       01  WS-I   PIC 99 VALUE 0.
       01  WS-LINE PIC X(40) VALUE SPACES.
       01  W4 PIC 9(4).
       01  WJ1 PIC X(4) JUSTIFIED RIGHT VALUE "AB".
       01  WB1 PIC ZZ9 BLANK WHEN ZERO VALUE ZERO.
       01  WE1 PIC XX/XX VALUE "ABCD".
       01  WS1 PIC XX/XX VALUE "AB/CD".
           88 WS1-OK VALUE "AB/CD".
       01  WT.
           05 WB PIC ZZ9 BLANK WHEN ZERO OCCURS 2
                 VALUE "000" FROM (1) TO (2).
           05 WZ PIC ZZ9 BLANK WHEN ZERO OCCURS 2
                 VALUE ZERO FROM (1).
           05 WJ PIC X(4) JUSTIFIED RIGHT OCCURS 2
                 VALUE "AB" FROM (1).
           05 WE PIC XX/XX OCCURS 2 VALUE "ABCD" FROM (1).
           05 WN PIC 9V99 OCCURS 2 VALUE 1.5 FROM (1).
       REPORT SECTION.
       RD  R1 PAGE LIMIT 20 LINES.
       01  DET TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC X(5) JUSTIFIED RIGHT VALUE "AB".
           03  COLUMN 8 PIC ZZZ9 BLANK WHEN ZERO VALUE "0000".
           03  COLUMN 14 PIC XXBXX VALUE "ABCDE".
           03  COLUMN 21 PIC 999 VALUE 7.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "WJ1=[" WJ1 "] WB1=[" WB1 "] WE1=[" WE1 "]"
           IF WS1-OK DISPLAY "WS1-OK TRUE" ELSE DISPLAY "WS1-OK FALSE"
           END-IF
           DISPLAY "WB=[" WB (1) WB (2) "] WZ=[" WZ (1) WZ (2) "]"
           DISPLAY "WJ=[" WJ (1) WJ (2) "] WE=[" WE (1) WE (2) "]"
           COMPUTE W4 = WN (1) * 100
           DISPLAY "WN1=" W4
           COMPUTE W4 = WN (2) * 100
           DISPLAY "WN2=" W4
           OPEN OUTPUT PRT
           INITIATE R1
           GENERATE DET
           TERMINATE R1
           CLOSE PRT
           OPEN INPUT CHK
           PERFORM UNTIL WS-EOF = "Y"
               READ CHK
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END PERFORM TAKE-BYTE
               END-READ
           END-PERFORM
           CLOSE CHK
           PERFORM SHOW-LINE
           STOP RUN.
       TAKE-BYTE.
           IF CHK-REC = X"0A"
               PERFORM SHOW-LINE
           ELSE
               IF CHK-REC NOT = X"0D" AND CHK-REC NOT = X"0C"
                   ADD 1 TO WS-I
                   MOVE CHK-REC TO WS-LINE (WS-I:1)
               END-IF
           END-IF.
       SHOW-LINE.
           IF WS-I > 0
               IF WS-LINE NOT = SPACES
                   DISPLAY "RPT=[" WS-LINE (1:WS-I) "]"
               END-IF
               MOVE SPACES TO WS-LINE
               MOVE 0 TO WS-I
           END-IF.
