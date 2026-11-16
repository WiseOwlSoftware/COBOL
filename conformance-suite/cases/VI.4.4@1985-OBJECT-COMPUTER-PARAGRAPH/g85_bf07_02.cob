      *>--- conformance-case
      *> id: g85_bf07_02
      *> rules: [1985-GR-VI.4.4.4-8]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.4.4.4 GR8 - "The program collating sequence established
      *> in the OBJECT-COMPUTER paragraph is applied to any nonnumeric
      *> merge or sort keys unless the COLLATING SEQUENCE phrase is
      *> specified in the respective SORT or MERGE statement."
      *> The program sequence is REVSEQ (§1985:VI.4.4.4 GR5), the
      *> alphabet "C" "B" "A": C < B < A, every other character after
      *> them (§1985:VI.4.5.4 GR4d). NAT is the native sequence
      *> (code-unit order, docs/CONFORMANCE.md DOC-A.1-8): A < B < C.
      *> S1 - SORT, no phrase: program sequence, keys A B C return CBA.
      *> S2 - SORT with COLLATING SEQUENCE IS NAT: the phrase wins, ABC.
      *> M1 - MERGE, no phrase, of BF0702A (C A) and BF0702B (B), each
      *>   in REVSEQ order: program sequence, CBA. Merged under native
      *>   order the heads C/B give B first: BCA, so the leg can fail.
      *> M2 - MERGE with COLLATING SEQUENCE IS NAT of BF0702C (A C) and
      *>   BF0702D (B), each in native order: ABC (under REVSEQ the
      *>   heads A/B give B first: BAC). §1985:XI.4.1.4 GR6 makes a
      *>   merge of unordered input undefined, so every input file is
      *>   written in the order its MERGE statement names.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF0702.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. X.
       OBJECT-COMPUTER. X
           PROGRAM COLLATING SEQUENCE IS REVSEQ.
       SPECIAL-NAMES.
           ALPHABET REVSEQ IS "C" "B" "A"
           ALPHABET NAT IS NATIVE.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SFILE ASSIGN TO "BF0702WK".
           SELECT IN-A ASSIGN TO "BF0702A".
           SELECT IN-B ASSIGN TO "BF0702B".
           SELECT IN-C ASSIGN TO "BF0702C".
           SELECT IN-D ASSIGN TO "BF0702D".
       DATA DIVISION.
       FILE SECTION.
       SD  SFILE.
       01  SREC.
           05  SKEY PIC X.
       FD  IN-A.
       01  A-REC PIC X.
       FD  IN-B.
       01  B-REC PIC X.
       FD  IN-C.
       01  C-REC PIC X.
       FD  IN-D.
       01  D-REC PIC X.
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
           DISPLAY "S1=" RESULT-LINE.
           SORT SFILE ON ASCENDING KEY SKEY
               COLLATING SEQUENCE IS NAT
               INPUT PROCEDURE IS IN-PROC
               OUTPUT PROCEDURE IS OUT-PROC.
           DISPLAY "S2=" RESULT-LINE.
           OPEN OUTPUT IN-A IN-B IN-C IN-D.
           MOVE "C" TO A-REC.
           WRITE A-REC.
           MOVE "A" TO A-REC.
           WRITE A-REC.
           MOVE "B" TO B-REC.
           WRITE B-REC.
           MOVE "A" TO C-REC.
           WRITE C-REC.
           MOVE "C" TO C-REC.
           WRITE C-REC.
           MOVE "B" TO D-REC.
           WRITE D-REC.
           CLOSE IN-A IN-B IN-C IN-D.
           MERGE SFILE ON ASCENDING KEY SKEY
               USING IN-A IN-B
               OUTPUT PROCEDURE IS OUT-PROC.
           DISPLAY "M1=" RESULT-LINE.
           MERGE SFILE ON ASCENDING KEY SKEY
               COLLATING SEQUENCE IS NAT
               USING IN-C IN-D
               OUTPUT PROCEDURE IS OUT-PROC.
           DISPLAY "M2=" RESULT-LINE.
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
