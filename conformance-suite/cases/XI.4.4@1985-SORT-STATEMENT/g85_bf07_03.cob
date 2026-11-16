      *>--- conformance-case
      *> id: g85_bf07_03
      *> rules: [1985-GR-XI.4.4.4-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XI.4.4.4 GR5 - "The collating sequence that applies to the
      *> comparison of the nonnumeric key data items specified is
      *> determined at the beginning of the execution of the SORT
      *> statement in the following order of precedence: a. First, the
      *> collating sequence established by the COLLATING SEQUENCE
      *> phrase, if specified, in the SORT statement. b. Second, the
      *> collating sequence established as the program collating
      *> sequence."
      *> Program sequence REVSEQ = "C" "B" "A" "2" "1" (§1985:VI.4.4.4
      *> GR5): C < B < A < 2 < 1, every other character after them in
      *> native order (§1985:VI.4.5.4 GR4d). ALT = "B" "C" "A".
      *> T1 - GR5b, no phrase: keys A B C under REVSEQ return CBA
      *>   (native order would give ABC).
      *> T2 - GR5a, COLLATING SEQUENCE IS ALT: B < C < A, so BCA (the
      *>   program sequence would give CBA).
      *> T3 - the rule's word "nonnumeric": a numeric key takes no
      *>   collating sequence; it is ordered "according to the rules
      *>   for comparison of operands in a relation condition"
      *>   (§1985:XI.4.4.4 GR2a), i.e. by algebraic value: 2 1 3 return
      *>   123. Weighed as characters under REVSEQ they would give 213.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF0703.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. X.
       OBJECT-COMPUTER. X
           PROGRAM COLLATING SEQUENCE IS REVSEQ.
       SPECIAL-NAMES.
           ALPHABET REVSEQ IS "C" "B" "A" "2" "1"
           ALPHABET ALT IS "B" "C" "A".
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SFILE ASSIGN TO "BF0703WK".
           SELECT NFILE ASSIGN TO "BF0703NK".
       DATA DIVISION.
       FILE SECTION.
       SD  SFILE.
       01  SREC.
           05  SKEY PIC X.
       SD  NFILE.
       01  NREC.
           05  NKEY PIC 9.
       WORKING-STORAGE SECTION.
       01  RESULT-LINE PIC X(3) VALUE SPACES.
       01  IX PIC 9 VALUE 0.
       01  EOF-FLAG PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN-SEC SECTION.
       MAIN-PARA.
           SORT SFILE ON ASCENDING KEY SKEY
               INPUT PROCEDURE IS IN-PROC
               OUTPUT PROCEDURE IS OUT-PROC.
           DISPLAY "T1=" RESULT-LINE.
           SORT SFILE ON ASCENDING KEY SKEY
               COLLATING SEQUENCE IS ALT
               INPUT PROCEDURE IS IN-PROC
               OUTPUT PROCEDURE IS OUT-PROC.
           DISPLAY "T2=" RESULT-LINE.
           SORT NFILE ON ASCENDING KEY NKEY
               INPUT PROCEDURE IS N-IN-PROC
               OUTPUT PROCEDURE IS N-OUT-PROC.
           DISPLAY "T3=" RESULT-LINE.
           STOP RUN.
       IN-PROC SECTION.
       IN-PARA.
           MOVE "A" TO SKEY.
           RELEASE SREC.
           MOVE "B" TO SKEY.
           RELEASE SREC.
           MOVE "C" TO SKEY.
           RELEASE SREC.
       OUT-PROC SECTION.
       OUT-PARA.
           MOVE SPACES TO RESULT-LINE.
           MOVE 0 TO IX.
           MOVE "N" TO EOF-FLAG.
           PERFORM RET-PARA UNTIL EOF-FLAG = "Y".
           GO TO OUT-EXIT.
       RET-PARA.
           RETURN SFILE AT END MOVE "Y" TO EOF-FLAG.
           IF EOF-FLAG = "N"
               ADD 1 TO IX
               MOVE SKEY TO RESULT-LINE (IX:1).
       OUT-EXIT.
           EXIT.
       N-IN-PROC SECTION.
       N-IN-PARA.
           MOVE 2 TO NKEY.
           RELEASE NREC.
           MOVE 1 TO NKEY.
           RELEASE NREC.
           MOVE 3 TO NKEY.
           RELEASE NREC.
       N-OUT-PROC SECTION.
       N-OUT-PARA.
           MOVE SPACES TO RESULT-LINE.
           MOVE 0 TO IX.
           MOVE "N" TO EOF-FLAG.
           PERFORM N-RET-PARA UNTIL EOF-FLAG = "Y".
           GO TO N-OUT-EXIT.
       N-RET-PARA.
           RETURN NFILE AT END MOVE "Y" TO EOF-FLAG.
           IF EOF-FLAG = "N"
               ADD 1 TO IX
               MOVE NKEY TO RESULT-LINE (IX:1).
       N-OUT-EXIT.
           EXIT.
