      *>--- conformance-case
      *> id: pb1563_line_sequential_dynamic_members
      *> rules: [DOC-A.1-63, GR-12.4.5.10.3-2, GR-14.9.30.4-2,
      *>    GR-14.9.30.4-15, GR-14.9.30.4-21, GR-14.9.51.4-21,
      *>    GR-14.9.51.4-23, GR-14.9.51.4-3, GR-14.9.35.4-17]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB1563 - A LINE SEQUENTIAL RECORD THAT HOLDS TWO OR
      *> MORE DYNAMIC-LENGTH MEMBERS (docs/CONFORMANCE.md section 3,
      *> determination D-FRA (x)).
      *>
      *> A line is plain text.  12.4.5.10.3 GR2: "Each record in a
      *> line sequential file is terminated by an implementor-defined
      *> line delimiter."  9.1.7.2: "In line sequential files the
      *> length of each record is determined by the number of
      *> characters between the preceding line delimiter and the
      *> following line delimiter".  So every character between the
      *> delimiters is the record, and the file cannot carry the
      *> table of member lengths that a framed record carries beside
      *> it (D-FRA (v)); a second program that reads the file as
      *> PIC X(n) must see exactly the characters written.
      *>
      *> What the standard DOES require is the record AREA after a
      *> READ: 14.9.30.4 GR13 c) makes "the record" available in the
      *> record area, and 8.5.1.11.2 says a variable-length item
      *> "behaves in all respects as though it were in fact
      *> contiguous with its neighbors whenever a procedural
      *> operation is applied to a group containing it".  The group's
      *> contiguous image (DISPLAY LR, FUNCTION LENGTH(LR), 15.50.4
      *> rule 7) is therefore EXACTLY the line, padded on the right
      *> by GR15 up to the record description's minimum size (every
      *> dynamic member at zero length), truncated at its maximum.
      *> No rule says where inside that image one dynamic member ends
      *> and the next begins: that is the implementor's (the A= KY=
      *> C= columns below are D-FRA (x)'s take step, the earlier
      *> member taking the excess up to its maximum size).  Neither
      *> GnuCOBOL, IBM nor Micro Focus admits a dynamic-length item in
      *> the file section, so WiseOwl COBOL's own determination
      *> stands.
      *>
      *> Paths walked (the success paths and the first exceptions;
      *> READ '09', REWRITE '43' '49' '71', record locks and the
      *> abnormal SORT paths are the companion golden
      *> pb1563_line_sequential_dynamic_members_exceptions):
      *>   A. WRITE of three records (14.9.51.4 GR21 drops the spaces
      *>      right of the rightmost non-space character), READ of
      *>      them back, '10' at the end of the file.
      *>   B. READ '47' on a file open for output (14.9.30.4 GR2) and
      *>      WRITE '48' on one open for input (14.9.51.4 GR3), both
      *>      through the USE procedure of the file.
      *>   C. WRITE '71' of a record whose dynamic member holds a
      *>      character outside the line sequential character set
      *>      (14.9.51.4 GR23): nothing reaches the file.
      *>   D. REWRITE of a record in place (14.9.35.4 GR17), then READ
      *>      of it; and the REWRITE of a record longer than the line
      *>      it replaces, '44' (GR17 b) - the record that the READ
      *>      delivered in full is NOT a partial transfer (GR17 a),
      *>      which a variable-length group's fixed run once made it.
      *>   E. A 25-character line (a seed written through a record
      *>      sequential print stream, which has no GR23) read into
      *>      the 20-character maximum: '06' and the truncated record
      *>      (GR15), the remainder by the next READ, an empty line
      *>      padded to the minimum (GR15), '10' (AT END) and '46'
      *>      (GR21) - on a file with a FILE STATUS and NO USE
      *>      procedure, which continues.
      *>   F. REWRITE after that '06' READ, which transferred only
      *>      PART of the line: '44' (14.9.35.4 GR17 a).
      *>   G. SORT ... USING / GIVING line sequential files.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1563LSDYN.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT LF ASSIGN TO "pb1563.dat"
              ORGANIZATION IS LINE SEQUENTIAL
              FILE STATUS IS LF-ST.
           SELECT SEEDF ASSIGN TO "pb1563s.dat"
              ORGANIZATION IS SEQUENTIAL
              FILE STATUS IS SEED-ST.
           SELECT RDF ASSIGN TO "pb1563s.dat"
              ORGANIZATION IS LINE SEQUENTIAL
              FILE STATUS IS RD-ST.
           SELECT SIN ASSIGN TO "pb1563i.dat"
              ORGANIZATION IS LINE SEQUENTIAL.
           SELECT SOUT ASSIGN TO "pb1563o.dat"
              ORGANIZATION IS LINE SEQUENTIAL.
           SELECT SRTF ASSIGN TO "pb1563.srt".
       DATA DIVISION.
       FILE SECTION.
       FD LF.
       01 LR.
          05 A PIC X DYNAMIC LENGTH LIMIT 9.
          05 KY PIC X(3).
          05 C PIC X DYNAMIC LENGTH LIMIT 8.
       FD SEEDF.
       01 S-REC PIC X(25).
       FD RDF.
       01 RR.
          05 RA PIC X DYNAMIC LENGTH LIMIT 9.
          05 RKY PIC X(3).
          05 RC PIC X DYNAMIC LENGTH LIMIT 8.
       FD SIN.
       01 IR.
          05 IK PIC X(2).
          05 IA PIC X DYNAMIC LENGTH LIMIT 9.
          05 IM PIC X(3).
          05 IC PIC X DYNAMIC LENGTH LIMIT 8.
       FD SOUT.
       01 OR1.
          05 OK PIC X(2).
          05 OA PIC X DYNAMIC LENGTH LIMIT 9.
          05 OM PIC X(3).
          05 OC PIC X DYNAMIC LENGTH LIMIT 8.
       SD SRTF.
       01 SR.
          05 SK PIC X(2).
          05 SA PIC X DYNAMIC LENGTH LIMIT 9.
          05 SM PIC X(3).
          05 SC PIC X DYNAMIC LENGTH LIMIT 8.
       WORKING-STORAGE SECTION.
       01 LF-ST PIC XX.
       01 SEED-ST PIC XX.
       01 RD-ST PIC XX.
       01 W-LEN PIC 99.
       PROCEDURE DIVISION.
       DECLARATIVES.
       LF-ERR SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON LF.
       LF-ERR-1.
           DISPLAY "  USE LF=" LF-ST.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-1.
      *> ---- A. write three records, read them back.
           OPEN OUTPUT LF.
           MOVE "AA" TO A.
           MOVE "KEY" TO KY.
           MOVE "CCCC" TO C.
           WRITE LR.
           DISPLAY "W1=" LF-ST.
           MOVE "HI" TO A.
           MOVE "LOW" TO KY.
           MOVE "  " TO C.
           WRITE LR.
           DISPLAY "W2=" LF-ST.
           MOVE "ABCDEFGHI" TO A.
           MOVE "XYZ" TO KY.
           MOVE "12345678" TO C.
           WRITE LR.
           DISPLAY "W3=" LF-ST.
      *> ---- B1. READ while open for output: 14.9.30.4 GR2, '47'.
           READ LF AT END DISPLAY "EOF-B1".
           DISPLAY "B1=" LF-ST.
      *> ---- C. a control character in a dynamic member: '71'.
           MOVE X"01" TO A.
           WRITE LR.
           DISPLAY "C1=" LF-ST.
           CLOSE LF.
           OPEN INPUT LF.
           PERFORM 3 TIMES
              READ LF AT END DISPLAY "EOF-A"
              END-READ
              MOVE FUNCTION LENGTH(LR) TO W-LEN
              DISPLAY "R=" LF-ST " [" LR "] LEN=" W-LEN
                 " A=[" A "] KY=[" KY "] C=[" C "]"
           END-PERFORM.
           READ LF AT END DISPLAY "A4=" LF-ST.
      *> ---- B2. WRITE while open for input: 14.9.51.4 GR3, '48'.
           WRITE LR.
           DISPLAY "B2=" LF-ST.
           CLOSE LF.
      *> ---- D. REWRITE in place (same 9 characters), read again.
           OPEN I-O LF.
           READ LF AT END DISPLAY "EOF-D".
           MOVE "12" TO A.
           MOVE "789" TO KY.
           MOVE "abcd" TO C.
           REWRITE LR.
           DISPLAY "D1=" LF-ST.
      *> ---- D3. the second line is 5 characters: a 9-character
      *>         record cannot replace it, 14.9.35.4 GR17 b), '44'.
           READ LF AT END DISPLAY "EOF-D3".
           MOVE "12" TO A.
           MOVE "789" TO KY.
           MOVE "abcd" TO C.
           REWRITE LR.
           DISPLAY "D3=" LF-ST.
           CLOSE LF.
           OPEN INPUT LF.
           PERFORM 2 TIMES
              READ LF AT END DISPLAY "EOF-D2"
              END-READ
              MOVE FUNCTION LENGTH(LR) TO W-LEN
              DISPLAY "D2=" LF-ST " [" LR "] LEN=" W-LEN
                 " A=[" A "] KY=[" KY "] C=[" C "]"
           END-PERFORM.
           CLOSE LF.
      *> ---- E. a 25-character line and an empty line, no USE.
           OPEN OUTPUT SEEDF.
           MOVE "ABCDEFGHIJKLMNOPQRSTUVWXY" TO S-REC.
           WRITE S-REC BEFORE ADVANCING 1 LINE.
           MOVE SPACES TO S-REC.
           WRITE S-REC BEFORE ADVANCING 1 LINE.
           CLOSE SEEDF.
           OPEN INPUT RDF.
           PERFORM 3 TIMES
              READ RDF AT END DISPLAY "EOF-E"
              END-READ
              MOVE FUNCTION LENGTH(RR) TO W-LEN
              DISPLAY "E=" RD-ST " [" RR "] LEN=" W-LEN
                 " A=[" RA "] KY=[" RKY "] C=[" RC "]"
           END-PERFORM.
           READ RDF AT END DISPLAY "E4=" RD-ST.
           READ RDF AT END DISPLAY "EOF-E5".
           DISPLAY "E5=" RD-ST.
           CLOSE RDF.
      *> ---- F. REWRITE after a READ that transferred only PART of
      *>         the line: 14.9.35.4 GR17 a), '44' (the replaced
      *>         record is the whole 25-character line, so only the
      *>         truncated READ is refused - the 9-character record
      *>         of D1 above was not part of a longer line).
           OPEN I-O RDF.
           READ RDF AT END DISPLAY "EOF-F".
           DISPLAY "F1=" RD-ST " [" RR "]".
           REWRITE RR.
           DISPLAY "F2=" RD-ST.
           CLOSE RDF.
      *> ---- G. SORT ... USING / GIVING line sequential files:
      *>         the sort orders the records by the leading fixed
      *>         key; each record's contiguous image reaches the
      *>         GIVING file unchanged (the lines are exactly the
      *>         images), and READ gives it back by the take step.
           OPEN OUTPUT SIN.
           MOVE "B2" TO IK.
           MOVE "AA" TO IA.
           MOVE "KEY" TO IM.
           MOVE "CCCC" TO IC.
           WRITE IR.
           MOVE "A1" TO IK.
           MOVE "x" TO IA.
           MOVE "mid" TO IM.
           MOVE "yz" TO IC.
           WRITE IR.
           CLOSE SIN.
           SORT SRTF ON ASCENDING KEY SK USING SIN GIVING SOUT.
           OPEN INPUT SOUT.
           PERFORM 2 TIMES
              READ SOUT AT END DISPLAY "EOF-G"
              END-READ
              DISPLAY "G=[" OR1 "] K=[" OK "] A=[" OA
                 "] M=[" OM "] C=[" OC "]"
           END-PERFORM.
           CLOSE SOUT.
           STOP RUN.
