      *>--- conformance-case
      *> id: pb5915_line_delimited_rewrite_size_85
      *> rules: [1985-GR-VII.4.5.4-10, 1985-GR-VII.2.8.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5915 - the REWRITE of a file whose records the line
      *> delimiter frames (RECORD DELIMITER IS LINE-SEQUENTIAL) is the
      *> sequential REWRITE at COBOL-85: equal size or '44', the file
      *> unchanged. The rules, checked with cite.py:
      *> - §1985:VII.4.5.4 GR10: "If the number of character positions
      *>   specified in the record referenced by record-name-1 is not
      *>   equal to the number of character positions in the record
      *>   being replaced, the execution of the REWRITE statement is
      *>   unsuccessful, the updating operation does not take place".
      *> - §1985:VII.1.3.5: I-O status 44, "the record is not the same
      *>   size as the record being replaced".
      *> - §1985:VII.2.8.4 GR1: the delimiter method "will not be
      *>   reflected in the record area or the record size used within
      *>   the program", so no line sequential rule (2023's GR17 c)
      *>   pads a shorter record and succeeds) reaches this file.
      *> The file holds ALPHA (5), BRAVO (5), FIFTEENCHARSXYZ (15) and
      *> DELTA (5). Through F (1 TO 20): a 3-character REWRITE of ALPHA
      *> is 44, a 7-character one of BRAVO is 44, a 15-character one of
      *> the third record is 00 and replaces it. Through G (1 TO 10),
      *> whose third READ truncates the 15-character line (04,
      *> §1985:VII.4.4.4 GR13), a 10-character REWRITE is 44: the record
      *> being replaced has 15 character positions. EXPECTED: the four
      *> statuses 44 44 00 44, then ALPHA, BRAVO, the replacement and
      *> DELTA read back with 00 and their sizes 5, 5, 15, 5.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5915A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "P5915A.DAT"
               ORGANIZATION IS SEQUENTIAL
               RECORD DELIMITER IS LINE-SEQUENTIAL
               FILE STATUS IS FS.
           SELECT G ASSIGN TO "P5915A.DAT"
               ORGANIZATION IS SEQUENTIAL
               RECORD DELIMITER IS LINE-SEQUENTIAL
               FILE STATUS IS GS.
       DATA DIVISION.
       FILE SECTION.
       FD  F
           RECORD IS VARYING IN SIZE FROM 1 TO 20 CHARACTERS
               DEPENDING ON RL.
       01  R           PIC X(20).
       FD  G
           RECORD IS VARYING IN SIZE FROM 1 TO 10 CHARACTERS
               DEPENDING ON GL.
       01  GR          PIC X(10).
       WORKING-STORAGE SECTION.
       01  FS          PIC XX.
       01  GS          PIC XX.
       01  RL          PIC 99.
       01  GL          PIC 99.
       01  EOFF        PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT F.
           MOVE "ALPHA" TO R. MOVE 5 TO RL. WRITE R.
           MOVE "BRAVO" TO R. MOVE 5 TO RL. WRITE R.
           MOVE "FIFTEENCHARSXYZ" TO R. MOVE 15 TO RL. WRITE R.
           MOVE "DELTA" TO R. MOVE 5 TO RL. WRITE R.
           CLOSE F.
           OPEN I-O F.
           READ F AT END DISPLAY "EOF" END-READ.
           MOVE "XYZ" TO R. MOVE 3 TO RL.
           REWRITE R.
           DISPLAY "SHORTER " FS.
           READ F AT END DISPLAY "EOF" END-READ.
           MOVE "LONGERX" TO R. MOVE 7 TO RL.
           REWRITE R.
           DISPLAY "LONGER " FS.
           READ F AT END DISPLAY "EOF" END-READ.
           MOVE "SAMESIZEREPLACE" TO R. MOVE 15 TO RL.
           REWRITE R.
           DISPLAY "EQUAL " FS.
           CLOSE F.
           OPEN I-O G.
           READ G AT END DISPLAY "EOF" END-READ.
           READ G AT END DISPLAY "EOF" END-READ.
           READ G AT END DISPLAY "EOF" END-READ.
           DISPLAY "TRUNCATED READ " GS " " GL.
           MOVE "TENCHARSOK" TO GR. MOVE 10 TO GL.
           REWRITE GR.
           DISPLAY "AFTER TRUNCATION " GS.
           CLOSE G.
           OPEN INPUT F.
           PERFORM UNTIL EOFF = "Y"
               MOVE SPACES TO R
               READ F AT END MOVE "Y" TO EOFF
               NOT AT END DISPLAY FS " " RL " [" R "]"
               END-READ
           END-PERFORM.
           CLOSE F.
           STOP RUN.
