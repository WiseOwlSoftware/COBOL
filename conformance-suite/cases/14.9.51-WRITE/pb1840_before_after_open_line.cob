      *>--- conformance-case
      *> id: pb1840_before_after_open_line
      *> rules: [GR-14.9.51.4-25]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB1840 -- a line presented while an earlier write's
      *> line is still open is presented at the SAME vertical position
      *> as that line: an overprint from column 1, never a continuation
      *> of its text.
      *>   python scripts/spec/cite.py --check 14.9.51.4 "If the BEFORE
      *>   phrase is used, the line is presented before the
      *>   representation of the printed page is advanced"
      *>                                      -> OK  14.9.51.4 25) e)
      *>   python scripts/spec/cite.py --check 14.9.51.4 "If the AFTER
      *>   phrase is used and the BEFORE phrase is not used, the line
      *>   is presented after the representation of the printed page
      *>   is advanced"                          -> OK  14.9.51.4 25) f)
      *>   python scripts/spec/cite.py --check 14.9.51.4 "If integer-1
      *>   or the value of the data item referenced by identifier-2 is
      *>   zero, no repositioning of the representation of the printed
      *>   page is performed"                    -> OK  14.9.51.4 25) c)
      *>   python scripts/spec/cite.py --check 14.9.51.4 "the record is
      *>   presented on the logical page before or after (depending on
      *>   the phrase used) the device is repositioned to the next
      *>   logical page"                         -> OK  14.9.51.4 25) g)
      *>   python scripts/spec/cite.py --check 14.9.51.4 "the record is
      *>   presented on the physical page before or after (depending
      *>   on the phrase used) the device is repositioned to the next
      *>   physical page"                        -> OK  14.9.51.4 25) h)
      *> DERIVATION. An AFTER write (or a plain one, which GR25 makes
      *> an AFTER ADVANCING 1 LINE) advances and THEN presents (GR25
      *> f)), so the device stands on its line. A BEFORE write presents
      *> first (GR25 e)): no advance lies between the two lines, so the
      *> second is at the same vertical position as the first, exactly
      *> as an advance of zero puts it (GR25 c)). The device
      *> representation is the implementor's; WiseOwl COBOL writes the
      *> bare carriage return of ADVANCING 0 (docs/CONFORMANCE.md
      *> "ADVANCING 0", owner decision R54). PAGE changes only the
      *> advance, not the presentation's place (GR25 g) and h)). A
      *> device already returned to the line start by an earlier zero
      *> advance writes no second return.
      *> The bytes are read back through a one-character record
      *> sequential FD over the same file. CR shows as "<", LF as "/"
      *> and FF as "^". A record sequential print stream without a
      *> LINAGE clause ends its lines with a line feed on every host,
      *> so its bytes are read as they are; the line end of a LINAGE
      *> file is the HOST newline (CR LF on Windows, LF elsewhere), so
      *> those are read with WS-FOLD = "Y", which folds a CR followed
      *> by LF into the one "/".
      *>   A  AFTER 1 "ABC"            LF, ABC               line open
      *>      BEFORE 1 "XYZ"           CR, XYZ, LF
      *>      plain "PQR"              LF, PQR  (AFTER 1)
      *>      BEFORE 1 "STU"           CR, STU, LF
      *>      => /ABC<XYZ//PQR<STU/
      *>   B  AFTER 1 "AAA"            LF, AAA
      *>      BEFORE PAGE "BBB"        CR, BBB, FF
      *>      => /AAA<BBB^
      *>   C  AFTER 1 "AAA"            LF, AAA
      *>      BEFORE 0 "BBB"           CR, BBB, CR (the zero advance)
      *>      BEFORE 1 "CCC"           CCC, LF: the device is already
      *>                               at the line start, no 2nd CR
      *>      => /AAA<BBB<CCC/
      *>   D  AFTER 1 "AAA"            LF, AAA
      *>      AFTER ZN (ZN = 0) "BBB"  CR, BBB  (identifier-2)
      *>      BEFORE 1 "CCC"           CR, CCC, LF
      *>      => /AAA<BBB<CCC/
      *>   E  nothing is open, so nothing overprints (the controls):
      *>      BEFORE 1 "AAA", BEFORE 1 "BBB", AFTER 1 "CCC"
      *>      => AAA/BBB//CCC/
      *>   F  LINAGE IS 5 LINES: OPEN sets LINAGE-COUNTER to 1
      *>      (13.18.34.4 GR7); AFTER 1 "AAA" -> counter 2 (line 2);
      *>      BEFORE 1 "BBB" presents on line 2 over AAA, then travels
      *>      one line -> counter 3
      *>      => /AAA<BBB/ and F1 = 3
      *>   G  LINAGE IS 5 LINES: AFTER 1 "AAA" (line 2), BEFORE PAGE
      *>      "BBB": BBB over AAA on line 2, then the device goes to the
      *>      first line of the next logical page, line 6: four line
      *>      ends, and the counter is 1 there (13.18.34.4 GR7 c) 1)
      *>      => /AAA<BBB//// and G1 = 1
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1840OL.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-CR SYM-LF SYM-FF
               ARE 14 11 13.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT P ASSIGN TO "pb1840o-p.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT PL ASSIGN TO "pb1840o-l.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT Q ASSIGN TO "pb1840o-p.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT QL ASSIGN TO "pb1840o-l.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD P.
       01 P-REC PIC X(3).
       FD PL LINAGE IS 5 LINES.
       01 PL-REC PIC X(3).
       FD Q.
       01 Q-REC PIC X.
       FD QL.
       01 QL-REC PIC X.
       WORKING-STORAGE SECTION.
       01 WS-EOF   PIC X.
       01 WS-LINE  PIC X(40).
       01 WS-PTR   PIC 99.
       01 WS-BYTE  PIC X.
       01 WS-CH    PIC X.
       01 WS-CR    PIC X.
       01 WS-FOLD  PIC X VALUE "N".
       01 ZN       PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT P
           MOVE "ABC" TO P-REC
           WRITE P-REC AFTER ADVANCING 1 LINE
           MOVE "XYZ" TO P-REC
           WRITE P-REC BEFORE ADVANCING 1 LINE
           MOVE "PQR" TO P-REC
           WRITE P-REC
           MOVE "STU" TO P-REC
           WRITE P-REC BEFORE ADVANCING 1 LINE
           CLOSE P
           PERFORM SHOW-P
           DISPLAY "A=" WS-LINE
           OPEN OUTPUT P
           MOVE "AAA" TO P-REC
           WRITE P-REC AFTER ADVANCING 1 LINE
           MOVE "BBB" TO P-REC
           WRITE P-REC BEFORE ADVANCING PAGE
           CLOSE P
           PERFORM SHOW-P
           DISPLAY "B=" WS-LINE
           OPEN OUTPUT P
           MOVE "AAA" TO P-REC
           WRITE P-REC AFTER ADVANCING 1 LINE
           MOVE "BBB" TO P-REC
           WRITE P-REC BEFORE ADVANCING 0 LINES
           MOVE "CCC" TO P-REC
           WRITE P-REC BEFORE ADVANCING 1 LINE
           CLOSE P
           PERFORM SHOW-P
           DISPLAY "C=" WS-LINE
           OPEN OUTPUT P
           MOVE "AAA" TO P-REC
           WRITE P-REC AFTER ADVANCING 1 LINE
           MOVE "BBB" TO P-REC
           WRITE P-REC AFTER ADVANCING ZN LINES
           MOVE "CCC" TO P-REC
           WRITE P-REC BEFORE ADVANCING 1 LINE
           CLOSE P
           PERFORM SHOW-P
           DISPLAY "D=" WS-LINE
           OPEN OUTPUT P
           MOVE "AAA" TO P-REC
           WRITE P-REC BEFORE ADVANCING 1 LINE
           MOVE "BBB" TO P-REC
           WRITE P-REC BEFORE ADVANCING 1 LINE
           MOVE "CCC" TO P-REC
           WRITE P-REC AFTER ADVANCING 1 LINE
           CLOSE P
           PERFORM SHOW-P
           DISPLAY "E=" WS-LINE
           MOVE "Y" TO WS-FOLD
           OPEN OUTPUT PL
           MOVE "AAA" TO PL-REC
           WRITE PL-REC AFTER ADVANCING 1 LINE
           MOVE "BBB" TO PL-REC
           WRITE PL-REC BEFORE ADVANCING 1 LINE
           DISPLAY "F1=" LINAGE-COUNTER OF PL
           CLOSE PL
           PERFORM SHOW-L
           DISPLAY "F=" WS-LINE
           OPEN OUTPUT PL
           MOVE "AAA" TO PL-REC
           WRITE PL-REC AFTER ADVANCING 1 LINE
           MOVE "BBB" TO PL-REC
           WRITE PL-REC BEFORE ADVANCING PAGE
           DISPLAY "G1=" LINAGE-COUNTER OF PL
           CLOSE PL
           PERFORM SHOW-L
           DISPLAY "G=" WS-LINE
           STOP RUN.
       SHOW-P.
           PERFORM START-LINE
           OPEN INPUT Q
           PERFORM UNTIL WS-EOF = "Y"
               READ Q
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END
                       MOVE Q-REC TO WS-BYTE
                       PERFORM SHOW-BYTE
               END-READ
           END-PERFORM
           CLOSE Q
           PERFORM END-LINE.
       SHOW-L.
           PERFORM START-LINE
           OPEN INPUT QL
           PERFORM UNTIL WS-EOF = "Y"
               READ QL
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END
                       MOVE QL-REC TO WS-BYTE
                       PERFORM SHOW-BYTE
               END-READ
           END-PERFORM
           CLOSE QL
           PERFORM END-LINE.
       START-LINE.
           MOVE SPACES TO WS-LINE
           MOVE 1 TO WS-PTR
           MOVE "N" TO WS-EOF
           MOVE "N" TO WS-CR.
       END-LINE.
           IF WS-CR = "Y"
               MOVE "<" TO WS-CH
               PERFORM APPEND-CH
           END-IF.
       SHOW-BYTE.
           IF WS-CR = "Y"
               MOVE "N" TO WS-CR
               IF WS-BYTE NOT = SYM-LF
                   MOVE "<" TO WS-CH
                   PERFORM APPEND-CH
               END-IF
           END-IF
           EVALUATE WS-BYTE
               WHEN SYM-CR
                   IF WS-FOLD = "Y"
                       MOVE "Y" TO WS-CR
                   ELSE
                       MOVE "<" TO WS-CH
                       PERFORM APPEND-CH
                   END-IF
               WHEN SYM-LF MOVE "/" TO WS-CH
                           PERFORM APPEND-CH
               WHEN SYM-FF MOVE "^" TO WS-CH
                           PERFORM APPEND-CH
               WHEN OTHER MOVE WS-BYTE TO WS-CH
                          PERFORM APPEND-CH
           END-EVALUATE.
       APPEND-CH.
           STRING WS-CH DELIMITED BY SIZE INTO WS-LINE
               WITH POINTER WS-PTR
           END-STRING.
