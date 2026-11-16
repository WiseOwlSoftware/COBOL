      *>--- conformance-case
      *> id: g85_bf07_10
      *> rules: [1985-GR-XI.2.4.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XI.2.4.4 GR1 - "The SAME RECORD/SORT/SORT-MERGE AREA clause
      *> for the Sort-Merge module is presented on the next page." The
      *> I-O-CONTROL paragraph of the Sort-Merge module takes that
      *> clause (§1985:XI.2.5), each of its three forms:
      *>   SAME RECORD AREA FOR SFILE OFILE     (a sort file, a file)
      *>   SAME SORT AREA FOR SFILE TFILE       (two sort files)
      *>   SAME SORT-MERGE AREA FOR UFILE WFILE (SORT-MERGE = SORT,
      *>                                        §1985:XI.2.5.3 SR3)
      *> keeping §1985:XI.2.5.3 SR4 and SR5 (no file in two SAME RECORD
      *> AREA clauses, no sort file in two SAME SORT AREA clauses, the
      *> plain files in no other SAME clause).
      *> §1985:XI.2.5.4 GR1 - in the SAME RECORD AREA "A logical record
      *> ... is considered as a logical record of each file open in the
      *> output mode": in SFILE's output procedure OFILE is open OUTPUT,
      *> so each record RETURNed from SFILE is OFILE's record and WRITE
      *> OFILE-REC (never moved to) writes it.
      *> O1 - OFILE read back after the sort: keys C A B sorted, ABC.
      *> T1, U1 - the sort files that share a sort area still sort:
      *>   ZXY gives XYZ, 312 gives 123 (§1985:XI.2.5.4 GR2a only
      *>   reuses memory between them; WFILE is never open during a
      *>   SORT, GR2d).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF0710.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SFILE ASSIGN TO "BF0710SW".
           SELECT TFILE ASSIGN TO "BF0710TW".
           SELECT UFILE ASSIGN TO "BF0710UW".
           SELECT OFILE ASSIGN TO "BF0710OF".
           SELECT WFILE ASSIGN TO "BF0710WF".
       I-O-CONTROL.
           SAME RECORD AREA FOR SFILE OFILE
           SAME SORT AREA FOR SFILE TFILE
           SAME SORT-MERGE AREA FOR UFILE WFILE.
       DATA DIVISION.
       FILE SECTION.
       SD  SFILE.
       01  SREC.
           05  SKEY PIC X.
       SD  TFILE.
       01  TREC.
           05  TKEY PIC X.
       SD  UFILE.
       01  UREC.
           05  UKEY PIC 9.
       FD  OFILE.
       01  OFILE-REC PIC X.
       FD  WFILE.
       01  WFILE-REC PIC X.
       WORKING-STORAGE SECTION.
       01  RESULT-LINE PIC X(3) VALUE SPACES.
       01  IX PIC 9 VALUE 0.
       01  EOF-FLAG PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN-SEC SECTION.
       MAIN-PARA.
           SORT SFILE ON ASCENDING KEY SKEY
               INPUT PROCEDURE IS S-IN
               OUTPUT PROCEDURE IS S-OUT.
           MOVE SPACES TO RESULT-LINE.
           MOVE 0 TO IX.
           MOVE "N" TO EOF-FLAG.
           OPEN INPUT OFILE.
           PERFORM READ-O UNTIL EOF-FLAG = "Y".
           CLOSE OFILE.
           DISPLAY "O1=" RESULT-LINE.
           SORT TFILE ON ASCENDING KEY TKEY
               INPUT PROCEDURE IS T-IN
               OUTPUT PROCEDURE IS T-OUT.
           DISPLAY "T1=" RESULT-LINE.
           SORT UFILE ON ASCENDING KEY UKEY
               INPUT PROCEDURE IS U-IN
               OUTPUT PROCEDURE IS U-OUT.
           DISPLAY "U1=" RESULT-LINE.
           STOP RUN.
       READ-O.
           READ OFILE AT END MOVE "Y" TO EOF-FLAG.
           IF EOF-FLAG = "N"
               ADD 1 TO IX
               MOVE OFILE-REC TO RESULT-LINE (IX:1).
       S-IN SECTION.
       S-IN-PARA.
           MOVE "C" TO SKEY.
           RELEASE SREC.
           MOVE "A" TO SKEY.
           RELEASE SREC.
           MOVE "B" TO SKEY.
           RELEASE SREC.
       S-OUT SECTION.
       S-OUT-PARA.
           OPEN OUTPUT OFILE.
           MOVE "N" TO EOF-FLAG.
           PERFORM S-RET UNTIL EOF-FLAG = "Y".
           CLOSE OFILE.
           GO TO S-OUT-EXIT.
       S-RET.
           RETURN SFILE AT END MOVE "Y" TO EOF-FLAG.
           IF EOF-FLAG = "N"
               WRITE OFILE-REC.
       S-OUT-EXIT.
           EXIT.
       T-IN SECTION.
       T-IN-PARA.
           MOVE "Z" TO TKEY.
           RELEASE TREC.
           MOVE "X" TO TKEY.
           RELEASE TREC.
           MOVE "Y" TO TKEY.
           RELEASE TREC.
       T-OUT SECTION.
       T-OUT-PARA.
           MOVE SPACES TO RESULT-LINE.
           MOVE 0 TO IX.
           MOVE "N" TO EOF-FLAG.
           PERFORM T-RET UNTIL EOF-FLAG = "Y".
           GO TO T-OUT-EXIT.
       T-RET.
           RETURN TFILE AT END MOVE "Y" TO EOF-FLAG.
           IF EOF-FLAG = "N"
               ADD 1 TO IX
               MOVE TKEY TO RESULT-LINE (IX:1).
       T-OUT-EXIT.
           EXIT.
       U-IN SECTION.
       U-IN-PARA.
           MOVE 3 TO UKEY.
           RELEASE UREC.
           MOVE 1 TO UKEY.
           RELEASE UREC.
           MOVE 2 TO UKEY.
           RELEASE UREC.
       U-OUT SECTION.
       U-OUT-PARA.
           MOVE SPACES TO RESULT-LINE.
           MOVE 0 TO IX.
           MOVE "N" TO EOF-FLAG.
           PERFORM U-RET UNTIL EOF-FLAG = "Y".
           GO TO U-OUT-EXIT.
       U-RET.
           RETURN UFILE AT END MOVE "Y" TO EOF-FLAG.
           IF EOF-FLAG = "N"
               ADD 1 TO IX
               MOVE UKEY TO RESULT-LINE (IX:1).
       U-OUT-EXIT.
           EXIT.
