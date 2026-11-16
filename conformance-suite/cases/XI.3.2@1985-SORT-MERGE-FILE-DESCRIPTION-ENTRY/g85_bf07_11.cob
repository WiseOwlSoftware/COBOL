      *>--- conformance-case
      *> id: g85_bf07_11
      *> rules: [1985-SR-XI.3.2.3-2, 1985-GR-XI.3.2.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XI.3.2.3 SR2 - "The clauses which follow file-name-1 are
      *> optional, and their order of appearance is immaterial."
      *>   SD-A has no clause at all; SD-B writes DATA RECORD before
      *>   RECORD CONTAINS (the format prints RECORD first). Both must
      *>   be accepted and sort (A1, B1).
      *> §1985:XI.3.2.4 GR2 - "The RECORD clause for the Sort-Merge
      *> module is the same as the RECORD clause for the Sequential I-O
      *> module." So the three formats of §1985:VII.3.8 hold in an SD:
      *>   SD-B format 1, RECORD CONTAINS 4 CHARACTERS;
      *>   SD-C format 2, RECORD IS VARYING IN SIZE FROM 2 TO 6
      *>     CHARACTERS DEPENDING ON C-LEN;
      *>   SD-D format 3, RECORD CONTAINS 2 TO 6 CHARACTERS.
      *>   For SD-C, §1985:VII.3.8.4 GR10a: on RELEASE the record's
      *>   length is C-LEN's content; GR11: after a successful RETURN
      *>   C-LEN holds the length of the record just returned. Records
      *>   (key, length) Z/3, A/6, M/2 return A6 M2 Z3 (C1). Only the
      *>   key and C-LEN are shown: the area past a short record is not
      *>   defined. The key lies in the first 2 positions (the minimum
      *>   record size, §1985:XI.4.4.3 SR4f).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF0711.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SD-A ASSIGN TO "BF0711AW".
           SELECT SD-B ASSIGN TO "BF0711BW".
           SELECT SD-C ASSIGN TO "BF0711CW".
           SELECT SD-D ASSIGN TO "BF0711DW".
       DATA DIVISION.
       FILE SECTION.
       SD  SD-A.
       01  A-REC.
           05  A-KEY PIC X.
       SD  SD-B
           DATA RECORD IS B-REC
           RECORD CONTAINS 4 CHARACTERS.
       01  B-REC.
           05  B-KEY PIC X.
           05  B-REST PIC X(3).
       SD  SD-C
           RECORD IS VARYING IN SIZE FROM 2 TO 6 CHARACTERS
               DEPENDING ON C-LEN.
       01  C-REC.
           05  C-KEY PIC X.
           05  C-REST PIC X(5).
       SD  SD-D
           RECORD CONTAINS 2 TO 6 CHARACTERS.
       01  D-SHORT.
           05  D-KEY PIC X.
           05  FILLER PIC X.
       01  D-LONG PIC X(6).
       WORKING-STORAGE SECTION.
       01  C-LEN PIC 9.
       01  RESULT-LINE PIC X(6) VALUE SPACES.
       01  IX PIC 9 VALUE 0.
       01  EOF-FLAG PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN-SEC SECTION.
       MAIN-PARA.
           SORT SD-A ON ASCENDING KEY A-KEY
               INPUT PROCEDURE IS A-IN
               OUTPUT PROCEDURE IS A-OUT.
           DISPLAY "A1=" RESULT-LINE.
           SORT SD-B ON DESCENDING KEY B-KEY
               INPUT PROCEDURE IS B-IN
               OUTPUT PROCEDURE IS B-OUT.
           DISPLAY "B1=" RESULT-LINE.
           SORT SD-C ON ASCENDING KEY C-KEY
               INPUT PROCEDURE IS C-IN
               OUTPUT PROCEDURE IS C-OUT.
           DISPLAY "C1=" RESULT-LINE.
           SORT SD-D ON ASCENDING KEY D-KEY
               INPUT PROCEDURE IS D-IN
               OUTPUT PROCEDURE IS D-OUT.
           DISPLAY "D1=" RESULT-LINE.
           STOP RUN.
       A-IN SECTION.
       A-IN-PARA.
           MOVE "Q" TO A-KEY.
           RELEASE A-REC.
           MOVE "P" TO A-KEY.
           RELEASE A-REC.
       A-OUT SECTION.
       A-OUT-PARA.
           MOVE SPACES TO RESULT-LINE.
           MOVE 0 TO IX.
           MOVE "N" TO EOF-FLAG.
           PERFORM A-RET UNTIL EOF-FLAG = "Y".
           GO TO A-OUT-EXIT.
       A-RET.
           RETURN SD-A AT END MOVE "Y" TO EOF-FLAG.
           IF EOF-FLAG = "N"
               ADD 1 TO IX
               MOVE A-KEY TO RESULT-LINE (IX:1).
       A-OUT-EXIT.
           EXIT.
       B-IN SECTION.
       B-IN-PARA.
           MOVE "1BBB" TO B-REC.
           RELEASE B-REC.
           MOVE "2CCC" TO B-REC.
           RELEASE B-REC.
       B-OUT SECTION.
       B-OUT-PARA.
           MOVE SPACES TO RESULT-LINE.
           MOVE 0 TO IX.
           MOVE "N" TO EOF-FLAG.
           PERFORM B-RET UNTIL EOF-FLAG = "Y".
           GO TO B-OUT-EXIT.
       B-RET.
           RETURN SD-B AT END MOVE "Y" TO EOF-FLAG.
           IF EOF-FLAG = "N"
               ADD 1 TO IX
               MOVE B-KEY TO RESULT-LINE (IX:1).
       B-OUT-EXIT.
           EXIT.
       C-IN SECTION.
       C-IN-PARA.
           MOVE "ZZZ" TO C-REC.
           MOVE 3 TO C-LEN.
           RELEASE C-REC.
           MOVE "AAAAAA" TO C-REC.
           MOVE 6 TO C-LEN.
           RELEASE C-REC.
           MOVE "MM" TO C-REC.
           MOVE 2 TO C-LEN.
           RELEASE C-REC.
       C-OUT SECTION.
       C-OUT-PARA.
           MOVE SPACES TO RESULT-LINE.
           MOVE 0 TO IX.
           MOVE "N" TO EOF-FLAG.
           PERFORM C-RET UNTIL EOF-FLAG = "Y".
           GO TO C-OUT-EXIT.
       C-RET.
           MOVE 0 TO C-LEN.
           RETURN SD-C AT END MOVE "Y" TO EOF-FLAG.
           IF EOF-FLAG = "N"
               ADD 1 TO IX
               MOVE C-KEY TO RESULT-LINE (IX:1)
               ADD 1 TO IX
               MOVE C-LEN TO RESULT-LINE (IX:1).
       C-OUT-EXIT.
           EXIT.
       D-IN SECTION.
       D-IN-PARA.
           MOVE "YYYYYY" TO D-LONG.
           RELEASE D-LONG.
           MOVE "XX" TO D-SHORT.
           RELEASE D-SHORT.
       D-OUT SECTION.
       D-OUT-PARA.
           MOVE SPACES TO RESULT-LINE.
           MOVE 0 TO IX.
           MOVE "N" TO EOF-FLAG.
           PERFORM D-RET UNTIL EOF-FLAG = "Y".
           GO TO D-OUT-EXIT.
       D-RET.
           RETURN SD-D AT END MOVE "Y" TO EOF-FLAG.
           IF EOF-FLAG = "N"
               ADD 1 TO IX
               MOVE D-KEY TO RESULT-LINE (IX:1).
       D-OUT-EXIT.
           EXIT.
