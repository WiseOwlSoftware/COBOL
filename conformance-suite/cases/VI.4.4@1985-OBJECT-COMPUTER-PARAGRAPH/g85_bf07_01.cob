      *>--- conformance-case
      *> id: g85_bf07_01
      *> rules: [1985-GR-VI.4.4.4-6]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.4.4.4 GR6 - no PROGRAM COLLATING SEQUENCE clause, so
      *> the program collating sequence is the native one: "If the
      *> PROGRAM COLLATING SEQUENCE clause is not specified, the program
      *> collating sequence is the native collating sequence."
      *> The OBJECT-COMPUTER paragraph is written, with no clause.
      *> The native sequence is the implementor's: WiseOwl documents it
      *> as code-unit order (docs/CONFORMANCE.md DOC-A.1-8).
      *> REL legs (§1985:VI.4.4.4 GR7a, the program sequence decides a
      *> nonnumeric relation condition): "a"(X61) > "B"(X42) and
      *> "0"(X30) < "A"(X41) are TRUE in code-unit order (both FALSE in
      *> EBCDIC, so a non-native default fails them).
      *> SORT legs (§1985:VI.4.4.4 GR8, §1985:XI.4.4.4 GR5b): SORT with
      *> no COLLATING SEQUENCE phrase takes the program sequence, here
      *> native: keys "a" "B" "0" return 0 B a. The second SORT names
      *> ALPHABET NAT IS NATIVE (§1985:VI.4.5.4 GR4b) in its phrase
      *> (§1985:XI.4.4.4 GR5a); the two orders must agree whatever the
      *> native sequence is, so S1 = S2 fails if the default is not
      *> the native sequence.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF0701.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. X.
       OBJECT-COMPUTER. X.
       SPECIAL-NAMES.
           ALPHABET NAT IS NATIVE.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SFILE ASSIGN TO "BF0701WK".
       DATA DIVISION.
       FILE SECTION.
       SD  SFILE.
       01  SREC.
           05  SKEY PIC X.
       WORKING-STORAGE SECTION.
       01  RESULT-LINE PIC X(3) VALUE SPACES.
       01  IX PIC 9 VALUE 0.
       01  EOF-FLAG PIC X VALUE "N".
       01  C-LOW-A PIC X VALUE "a".
       01  C-UP-A PIC X VALUE "A".
       01  C-UP-B PIC X VALUE "B".
       01  C-ZERO PIC X VALUE "0".
       PROCEDURE DIVISION.
       MAIN-SEC SECTION.
       MAIN-PARA.
           IF C-LOW-A > C-UP-B
               DISPLAY "REL1=T"
           ELSE
               DISPLAY "REL1=F".
           IF C-ZERO < C-UP-A
               DISPLAY "REL2=T"
           ELSE
               DISPLAY "REL2=F".
           SORT SFILE ON ASCENDING KEY SKEY
               INPUT PROCEDURE IS IN-PROC
               OUTPUT PROCEDURE IS OUT-PROC.
           DISPLAY "S1=" RESULT-LINE.
           SORT SFILE ON ASCENDING KEY SKEY
               COLLATING SEQUENCE IS NAT
               INPUT PROCEDURE IS IN-PROC
               OUTPUT PROCEDURE IS OUT-PROC.
           DISPLAY "S2=" RESULT-LINE.
           STOP RUN.
       IN-PROC SECTION.
       IN-PARA.
           MOVE "a" TO SKEY.
           RELEASE SREC.
           MOVE "B" TO SKEY.
           RELEASE SREC.
           MOVE "0" TO SKEY.
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
