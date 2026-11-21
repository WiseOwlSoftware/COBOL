      *>--- conformance-case
      *> id: 1985-GR-VI_1_2-p2
      *> rules: [1985-GR-VI.1.2-p2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.1.2 p2 (INTRODUCTION TO THE NUCLEUS MODULE). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-08.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * 1985-GR-VI.1.2-p2: Nucleus level 2 statements, qualification,
      * a seven-dimensional table. Expect: 007 B 3 AXC 3 AB-CD 7 OK
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NUCL2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T7.
        02 A1 OCCURS 2.
         03 A2 OCCURS 2.
          04 A3 OCCURS 2.
           05 A4 OCCURS 2.
            06 A5 OCCURS 2.
             07 A6 OCCURS 2.
              08 A7 OCCURS 2 PIC 9.
       01 G1.
          02 K PIC 99.
       01 G2.
          02 K PIC 99.
       01 S PIC X(3) VALUE "ABC".
       01 S2 PIC X(5).
       01 P1X PIC X(2).
       01 P2X PIC X(2).
       01 C PIC 9.
       01 TB.
          02 E OCCURS 3 INDEXED BY I PIC X.
       PROCEDURE DIVISION.
       MAIN.
           INITIALIZE T7.
           COMPUTE K OF G1 = 2 + 5.
           MOVE 7 TO A7 (2, 2, 2, 2, 2, 2, 2).
           MOVE A7 (2, 2, 2, 2, 2, 2, 2) TO K IN G2.
           IF K OF G1 = K OF G2 DISPLAY "0" K OF G1 WITH NO ADVANCING.
           EVALUATE TRUE
             WHEN K OF G1 > 5 DISPLAY " B" WITH NO ADVANCING
             WHEN OTHER DISPLAY " WRONG" WITH NO ADVANCING
           END-EVALUATE.
           MOVE "ZBY" TO TB.
           SET I TO 1.
           SEARCH E WHEN E (I) = "Y" SET C TO I DISPLAY " " C
                    WITH NO ADVANCING
           END-SEARCH.
           INSPECT S REPLACING ALL "B" BY "X".
           DISPLAY " " S WITH NO ADVANCING.
           MOVE 0 TO C.
           INSPECT S TALLYING C FOR CHARACTERS.
           DISPLAY " " C WITH NO ADVANCING.
           STRING "AB" DELIMITED BY SIZE "-" DELIMITED BY SIZE
                  "CD" DELIMITED BY SIZE INTO S2.
           DISPLAY " " S2 WITH NO ADVANCING.
           UNSTRING S2 DELIMITED BY "-" INTO P1X P2X.
           IF P1X = "AB" AND P2X = "CD"
              DISPLAY " " A7 (2, 2, 2, 2, 2, 2, 2) " OK"
           ELSE DISPLAY " WRONG".
           STOP RUN.
