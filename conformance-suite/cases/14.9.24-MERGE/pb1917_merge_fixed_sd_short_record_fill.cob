*>--- conformance-case
*> id: pb1917_merge_fixed_sd_short_record_fill
*> rules: [GR-14.9.24.4-2, GR-14.9.24.4-7]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1917 - THE MERGE USING ARM OF THE SHORT-RECORD FILL, INTO A FIXED-LENGTH MERGE FILE.
      *> pb1140_sort_merge_short_record_fill runs the SORT legs of the fill and a MERGE leg whose SD is VARYING, so the
      *> fill there happens on the GIVING side (GR13); this test runs the MERGE USING side, GR2 of 14.9.24.4:
      *>   cite.py --check 14.9.24.4 "If the file referenced by file-name-1 contains only fixed-length records, any
      *>     record in the file referenced by file-name-2 or file-name-3 containing fewer character positions than that
      *>     fixed length is space filled on the right to that fixed length" -> OK §14.9.24.4 2)
      *>   cite.py --check 14.9.24.4 "a) If there is only one record description entry associated with the file
      *>     referenced by file-name-2 or file-name-3 and that record is described as a national data item or as an
      *>     elementary data item of usage national and of category numeric, numeric-edited, or boolean, the record is
      *>     filled with national space characters." -> OK §14.9.24.4 2) a)
      *>   cite.py --check 14.9.24.4 "c) Otherwise, the record is filled with alphanumeric space characters."
      *>     -> OK §14.9.24.4 2) c)
      *> (b) needs SELECT WHEN, which WiseOwl COBOL declines by name - Annex A.4.8 - so it cannot arise.) A national
      *> space is the two bytes 00 20 (UTF-16BE) and an alphanumeric space the one byte 20. Every SD below is FIXED: no
      *> RECORD VARYING clause, so its records are all its description's 6 bytes, and each 4-byte USING record is
      *> shorter than that. The GIVING files are fixed 6 bytes too, so nothing fills on their side, and every merged
      *> record is read back BYTE BY BYTE through an X(6) record (ORD gives byte + 1: 00 -> 001, 20 -> 033).
      *>   N  MERGE USING UN1 (one record N(2): "XX" = 00 58 00 58) and UN2 (one record PIC 9(2) USAGE NATIONAL:
      *>      "12" = 00 31 00 32, an elementary usage-national item of numeric category) GIVING GN. Both files meet
      *>      GR2 a), so the two bytes after each 4-byte record are 00 20. The national key order puts "12" first:
      *>        001 050 001 051 001 033   then   001 089 001 089 001 033
      *>   A  MERGE USING UA1 UA2 (one record X(4) each: "AAAA", "BBBB") into an alphanumeric-key SD: GR2 c), 20 20:
      *>        066 066 066 066 033 033   then   067 067 067 067 033 033
      *>   M  MERGE USING UM1 UM2 (TWO record descriptions each, N(2) and X(4); the program writes the national one
      *>      "YY", "ZZ"): the national record is not the ONLY description, so GR2 a) does not apply and c) does:
      *>        001 090 001 090 033 033   then   001 091 001 091 033 033
      *>   O  the same national files with an OUTPUT PROCEDURE instead of GIVING: the RETURNed record, read in the SD's
      *>      own area, already carries 00 20 in bytes 5 and 6 (the fill is the USING side's, GR2, not the GIVING's):
      *>        OP5=001 OP6=033, twice
      *>   X  EXCEPTION PATH, GR7 a): MERGE USING UD1 MISS GIVING GX, where MISS is a non-optional file that is not
      *>      there. Its implicit OPEN INPUT is I-O status 35 (cite.py --check 9.1.13.6 "I-O status = 35" -> OK
      *>      §9.1.13.6 5)), which is fatal (9.1.13.1), the MERGE statement ends at that OPEN, and the program has a
      *>      FILE STATUS clause for MISS so it continues (docs/CONFORMANCE.md DOC-A.1-103): MISS-ST = 35, the
      *>      GIVING file GX was never opened by the terminated statement (GX-ST still spaces), and OPEN INPUT GX
      *>      afterwards is 35 again - no merged record, filled or not, reached it.
      *> A leg fails if the fill is the alphanumeric space everywhere (N and O wrong), the national space everywhere
      *> (A and M wrong), taken from whether ANY of the connector's records is national (M wrong), or lost on the MERGE
      *> USING arm (the 4-byte record reaches the SD short or filled with zero bytes: bytes 5 and 6 would not be a space).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1917MERGEFILL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT UN1 ASSIGN TO "pb1917un1.dat".
           SELECT UN2 ASSIGN TO "pb1917un2.dat".
           SELECT UA1 ASSIGN TO "pb1917ua1.dat".
           SELECT UA2 ASSIGN TO "pb1917ua2.dat".
           SELECT UM1 ASSIGN TO "pb1917um1.dat".
           SELECT UM2 ASSIGN TO "pb1917um2.dat".
           SELECT UD1 ASSIGN TO "pb1917ud1.dat".
           SELECT UD2 ASSIGN TO "pb1917ud2.dat".
           SELECT MISS ASSIGN TO "pb1917-never-written.dat"
               FILE STATUS IS MISS-ST.
           SELECT GN ASSIGN TO "pb1917gn.dat".
           SELECT GA ASSIGN TO "pb1917ga.dat".
           SELECT GM ASSIGN TO "pb1917gm.dat".
           SELECT GX ASSIGN TO "pb1917gx.dat" FILE STATUS IS GX-ST.
           SELECT BN ASSIGN TO "pb1917gn.dat".
           SELECT BA ASSIGN TO "pb1917ga.dat".
           SELECT BM ASSIGN TO "pb1917gm.dat".
           SELECT BX ASSIGN TO "pb1917gx.dat" FILE STATUS IS BX-ST.
           SELECT SN ASSIGN TO "pb1917sn.tmp".
           SELECT SA ASSIGN TO "pb1917sa.tmp".
       DATA DIVISION.
       FILE SECTION.
       FD UN1.
       01 UN1-REC PIC N(2).
       FD UN2.
       01 UN2-REC PIC 9(2) USAGE NATIONAL.
       FD UA1.
       01 UA1-REC PIC X(4).
       FD UA2.
       01 UA2-REC PIC X(4).
       FD UM1.
       01 UM1-N PIC N(2).
       01 UM1-X PIC X(4).
       FD UM2.
       01 UM2-N PIC N(2).
       01 UM2-X PIC X(4).
       FD UD1.
       01 UD1-REC PIC N(2).
       FD UD2.
       01 UD2-REC PIC N(2).
       FD MISS.
       01 MISS-REC PIC N(2).
       FD GN.
       01 GN-REC PIC X(6).
       FD GA.
       01 GA-REC PIC X(6).
       FD GM.
       01 GM-REC PIC X(6).
       FD GX.
       01 GX-REC PIC X(6).
       FD BN.
       01 BN-REC PIC X(6).
       FD BA.
       01 BA-REC PIC X(6).
       FD BM.
       01 BM-REC PIC X(6).
       FD BX.
       01 BX-REC PIC X(6).
       SD SN.
       01 SN-REC.
          05 SN-K PIC N(2).
          05 SN-T PIC N(1).
       01 SN-X PIC X(6).
       SD SA.
       01 SA-REC.
          05 SA-K PIC X(4).
          05 SA-T PIC X(2).
       WORKING-STORAGE SECTION.
       01 W-I PIC 99.
       01 W-O PIC 9(3).
       01 W-EOF PIC X VALUE "N".
       01 MISS-ST PIC XX VALUE SPACES.
       01 GX-ST PIC XX VALUE SPACES.
       01 BX-ST PIC XX VALUE SPACES.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT UN1.
           MOVE N"XX" TO UN1-REC. WRITE UN1-REC.
           CLOSE UN1.
           OPEN OUTPUT UN2.
           MOVE N"12" TO UN2-REC. WRITE UN2-REC.
           CLOSE UN2.
           MERGE SN ON ASCENDING KEY SN-K USING UN1 UN2 GIVING GN.
           OPEN OUTPUT UA1.
           MOVE "AAAA" TO UA1-REC. WRITE UA1-REC.
           CLOSE UA1.
           OPEN OUTPUT UA2.
           MOVE "BBBB" TO UA2-REC. WRITE UA2-REC.
           CLOSE UA2.
           MERGE SA ON ASCENDING KEY SA-K USING UA1 UA2 GIVING GA.
           OPEN OUTPUT UM1.
           MOVE N"YY" TO UM1-N. WRITE UM1-N.
           CLOSE UM1.
           OPEN OUTPUT UM2.
           MOVE N"ZZ" TO UM2-N. WRITE UM2-N.
           CLOSE UM2.
           MERGE SN ON ASCENDING KEY SN-K USING UM1 UM2 GIVING GM.
           OPEN OUTPUT UD1.
           MOVE N"XX" TO UD1-REC. WRITE UD1-REC.
           CLOSE UD1.
           OPEN OUTPUT UD2.
           MOVE N"YY" TO UD2-REC. WRITE UD2-REC.
           CLOSE UD2.
           MERGE SN ON ASCENDING KEY SN-K USING UD1 UD2
               OUTPUT PROCEDURE SHOW-N.
           MERGE SN ON ASCENDING KEY SN-K USING UD1 MISS GIVING GX.
           DISPLAY "MISS-ST=" MISS-ST.
           DISPLAY "GX-ST=[" GX-ST "]".
           PERFORM DUMP-ALL.
           STOP RUN.
       SHOW-N.
           MOVE "N" TO W-EOF.
           PERFORM UNTIL W-EOF = "Y"
               RETURN SN AT END MOVE "Y" TO W-EOF
               NOT AT END
                   MOVE FUNCTION ORD(SN-X(5:1)) TO W-O
                   DISPLAY "OP5=" W-O
                   MOVE FUNCTION ORD(SN-X(6:1)) TO W-O
                   DISPLAY "OP6=" W-O
               END-RETURN
           END-PERFORM.
       DUMP-ALL.
           OPEN INPUT BN.
           PERFORM 2 TIMES
               READ BN AT END DISPLAY "EOF"
               END-READ
               PERFORM VARYING W-I FROM 1 BY 1 UNTIL W-I > 6
                   MOVE FUNCTION ORD(BN-REC(W-I:1)) TO W-O
                   DISPLAY "N B" W-I "=" W-O
               END-PERFORM
           END-PERFORM.
           CLOSE BN.
           OPEN INPUT BA.
           PERFORM 2 TIMES
               READ BA AT END DISPLAY "EOF"
               END-READ
               PERFORM VARYING W-I FROM 1 BY 1 UNTIL W-I > 6
                   MOVE FUNCTION ORD(BA-REC(W-I:1)) TO W-O
                   DISPLAY "A B" W-I "=" W-O
               END-PERFORM
           END-PERFORM.
           CLOSE BA.
           OPEN INPUT BM.
           PERFORM 2 TIMES
               READ BM AT END DISPLAY "EOF"
               END-READ
               PERFORM VARYING W-I FROM 1 BY 1 UNTIL W-I > 6
                   MOVE FUNCTION ORD(BM-REC(W-I:1)) TO W-O
                   DISPLAY "M B" W-I "=" W-O
               END-PERFORM
           END-PERFORM.
           CLOSE BM.
           OPEN INPUT BX.
           DISPLAY "BX-ST=" BX-ST.
