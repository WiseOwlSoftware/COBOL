      *>--- conformance-case
      *> id: pb5915_line_delimited_rewrite_medium_85
      *> rules: [1985-GR-VII.4.5.4-10, 1985-GR-VII.2.8.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5915 - the two refusals of the COBOL-85 REWRITE of a
      *> file whose records the line delimiter frames (RECORD DELIMITER
      *> IS LINE-SEQUENTIAL) beyond GR10's character positions, and
      *> their order. Both are the determination of Annex A.1 item 150
      *> (docs/conformance/7/DOC-A.1-150.md); cite.py-checked rules:
      *> - §1985:VII.4.5.4 GR10: "the execution of the REWRITE statement
      *>   is unsuccessful, the updating operation does not take place".
      *> - §1985:VII.1.3.5: 44, "the record is not the same size as the
      *>   record being replaced"; a 9x "must not duplicate any
      *>   condition specified by the I-O status values 00 through 49".
      *> A record whose character positions equal the line's but whose
      *> characters take another number of bytes on the medium (U+00E9
      *> is two UTF-8 bytes, Annex A.1 item 115) cannot replace the
      *> line in place: '44', larger or smaller. A record holding the
      *> line delimiter is outside the line character set: '71' at
      *> 2023, reported at 85 as its implementor-defined 9x, '99'. The
      *> size check comes first, so a record that both changes the byte
      *> size and holds a delimiter is '44'. Every refusal leaves the
      *> file unchanged. The file holds ALPHA, BRAVO, CHARL+U+00E9 and
      *> DELTA; the read-back shows U+00E9 as "?". EXPECTED: WRITE 00,
      *> then 44 99 44 44, then the four records unchanged, size 5.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5915B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "P5915B.DAT"
               ORGANIZATION IS SEQUENTIAL
               RECORD DELIMITER IS LINE-SEQUENTIAL
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  F
           RECORD IS VARYING IN SIZE FROM 1 TO 20 CHARACTERS
               DEPENDING ON RL.
       01  R           PIC X(20).
       WORKING-STORAGE SECTION.
       01  FS          PIC XX.
       01  RL          PIC 99.
       01  N           PIC 9 VALUE 0.
       01  EOFF        PIC X VALUE "N".
       01  BE          PIC 999 BINARY VALUE 233.
       01  BEX REDEFINES BE.
           05 FILLER   PIC X.
           05 HE       PIC X.
       01  BL          PIC 999 BINARY VALUE 10.
       01  BLX REDEFINES BL.
           05 FILLER   PIC X.
           05 HL       PIC X.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT F.
           MOVE "ALPHA" TO R. MOVE 5 TO RL. WRITE R.
           MOVE "BRAVO" TO R. MOVE 5 TO RL. WRITE R.
           MOVE "CHARL" TO R. MOVE HE TO R(5:1). MOVE 5 TO RL.
           WRITE R.
           DISPLAY "WRITE-WIDE " FS.
           MOVE "DELTA" TO R. MOVE 5 TO RL. WRITE R.
           CLOSE F.
           OPEN I-O F.
           READ F AT END DISPLAY "EOF" END-READ.
           MOVE "ALPH" TO R. MOVE HE TO R(5:1). MOVE 5 TO RL.
           REWRITE R.
           DISPLAY "SAME-CHARS-MORE-BYTES " FS.
           READ F AT END DISPLAY "EOF" END-READ.
           MOVE "BRA" TO R. MOVE HL TO R(4:1). MOVE "O" TO R(5:1).
           MOVE 5 TO RL.
           REWRITE R.
           DISPLAY "LF-IN-RECORD " FS.
           READ F AT END DISPLAY "EOF" END-READ.
           MOVE "CHARL" TO R. MOVE 5 TO RL.
           REWRITE R.
           DISPLAY "SAME-CHARS-FEWER-BYTES " FS.
           READ F AT END DISPLAY "EOF" END-READ.
           MOVE "DELT" TO R. MOVE HE TO R(5:1). MOVE HL TO R(3:1).
           MOVE 5 TO RL.
           REWRITE R.
           DISPLAY "MORE-BYTES-AND-LF " FS.
           CLOSE F.
           OPEN INPUT F.
           PERFORM UNTIL EOFF = "Y"
               MOVE SPACES TO R
               READ F AT END MOVE "Y" TO EOFF
               NOT AT END
                   ADD 1 TO N
                   IF R(5:1) = HE
                       MOVE "?" TO R(5:1)
                   END-IF
                   DISPLAY N " " FS " " RL " [" R(1:5) "]"
               END-READ
           END-PERFORM.
           CLOSE F.
           STOP RUN.
