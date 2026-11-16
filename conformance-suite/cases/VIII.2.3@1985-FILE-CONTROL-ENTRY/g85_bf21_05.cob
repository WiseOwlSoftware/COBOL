      *>--- conformance-case
      *> id: g85_bf21_05
      *> rules: [1985-GR-VIII.2.3.4-7]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.2.3.4 GR7 - the ACCESS MODE clause and the
      *> ORGANIZATION IS RELATIVE clause of the relative file control
      *> entry, in the forms the following pages present: ORGANIZATION
      *> IS RELATIVE and the bare RELATIVE; ACCESS MODE IS RANDOM, IS
      *> DYNAMIC (with IS and MODE omitted) and IS SEQUENTIAL, each with
      *> RELATIVE KEY, and the clause omitted. One physical file is
      *> written by key in random mode (5, 2, 7) and read back through
      *> each entry: §1985:VIII.2.4.4 GR1 and GR2 (omitted clause is
      *> sequential, ascending relative record number: the "SEQ" lines),
      *> GR4 with §1985:VIII.4.5.4 GR15 and GR13 (dynamic: a keyed READ
      *> then READ NEXT: the "DYN" lines), §1985:VIII.4.5.4 GR14 (the
      *> sequential READ returns the number in the key: the "SQK"
      *> lines). A refused form, a wrong default access mode or a
      *> clause bound to the wrong key item changes the output.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF2105.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F-RND ASSIGN TO "BF2105.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS K-RND.
           SELECT F-SEQ ASSIGN TO "BF2105.DAT"
               RELATIVE.
           SELECT F-DYN ASSIGN TO "BF2105.DAT"
               ORGANIZATION RELATIVE
               ACCESS DYNAMIC RELATIVE K-DYN.
           SELECT F-SQK ASSIGN TO "BF2105.DAT"
               ACCESS MODE IS SEQUENTIAL RELATIVE KEY IS K-SQK
               ORGANIZATION IS RELATIVE.
       DATA DIVISION.
       FILE SECTION.
       FD  F-RND.
       01  RND-REC              PIC X(6).
       FD  F-SEQ.
       01  SEQ-REC              PIC X(6).
       FD  F-DYN.
       01  DYN-REC              PIC X(6).
       FD  F-SQK.
       01  SQK-REC              PIC X(6).
       WORKING-STORAGE SECTION.
       01  K-RND                PIC 99.
       01  K-DYN                PIC 99.
       01  K-SQK                PIC 99.
       01  WS-EOF               PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT F-RND.
           MOVE 5 TO K-RND.
           MOVE "FIVE" TO RND-REC.
           WRITE RND-REC INVALID KEY DISPLAY "W5 INVALID".
           MOVE 2 TO K-RND.
           MOVE "TWO" TO RND-REC.
           WRITE RND-REC INVALID KEY DISPLAY "W2 INVALID".
           MOVE 7 TO K-RND.
           MOVE "SEVEN" TO RND-REC.
           WRITE RND-REC INVALID KEY DISPLAY "W7 INVALID".
           CLOSE F-RND.
           OPEN INPUT F-SEQ.
           MOVE "N" TO WS-EOF.
           PERFORM READ-SEQ UNTIL WS-EOF = "Y".
           CLOSE F-SEQ.
           OPEN INPUT F-DYN.
           MOVE 5 TO K-DYN.
           READ F-DYN INVALID KEY DISPLAY "DYN KEY 05 INVALID".
           DISPLAY "DYN KEY " K-DYN " [" DYN-REC "]".
           READ F-DYN NEXT AT END DISPLAY "DYN NEXT END".
           DISPLAY "DYN NEXT " K-DYN " [" DYN-REC "]".
           READ F-DYN NEXT AT END DISPLAY "DYN NEXT END".
           CLOSE F-DYN.
           OPEN INPUT F-SQK.
           MOVE "N" TO WS-EOF.
           PERFORM READ-SQK UNTIL WS-EOF = "Y".
           CLOSE F-SQK.
           STOP RUN.
       READ-SEQ.
           READ F-SEQ
               AT END
                   MOVE "Y" TO WS-EOF
                   DISPLAY "SEQ END"
               NOT AT END
                   DISPLAY "SEQ [" SEQ-REC "]"
           END-READ.
       READ-SQK.
           READ F-SQK
               AT END
                   MOVE "Y" TO WS-EOF
                   DISPLAY "SQK END"
               NOT AT END
                   DISPLAY "SQK " K-SQK " [" SQK-REC "]"
           END-READ.
