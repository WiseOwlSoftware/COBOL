      *>--- conformance-case
      *> id: g2_read_not_open_47
      *> rules: [2002-GR-14.8.29.3-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.29.3 GR1 - "The open mode of the file connector
      *> referenced by file-name-1 shall be input or I-O. If it is any
      *> other value, the execution of the READ statement is
      *> unsuccessful and the I-O status value for file-name-1 is set
      *> to '47'." §2002:9.1.12.6 gives 47 the same meaning.
      *> Every arm below is a connector NOT open in input or I-O mode:
      *> never opened, open OUTPUT, open EXTEND; sequential, relative
      *> and indexed; sequential (NEXT/AT END) and random (KEY/INVALID
      *> KEY) reads. Each leg can fail by any other status.
      *>  - INTO: §2002:14.8.29.3 GR4 "If the execution of a READ
      *>    statement with the INTO phrase is unsuccessful, the content
      *>    of the data item referenced by identifier-1 is unchanged",
      *>    so W stays KEEP (fails if the read moved anything).
      *>  - AT END / INVALID KEY: 47 is neither condition, so by
      *>    §2002:14.8.29.3 GR12 "the AT END phrase or the INVALID KEY
      *>    phrase is ignored"; no "TAKEN" line may appear.
      *>  - 47 is fatal (§2002:9.1.12); the implementor's choice to
      *>    continue when the connector has a FILE STATUS clause is
      *>    docs/conformance/7/DOC-A.1-103.md, so each DISPLAY runs.
      *>  - Controls: OPEN I-O on RL and OPEN INPUT on SQ are the
      *>    admitted modes; both files are empty, so the read finds no
      *>    record (§2002:14.8.29.3 GR18) and gives 10, never 47.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RD02.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SQ ASSIGN TO "g2rd01s.dat"
               ORGANIZATION SEQUENTIAL FILE STATUS IS S1.
           SELECT RL ASSIGN TO "g2rd01r.dat"
               ORGANIZATION RELATIVE ACCESS DYNAMIC
               RELATIVE KEY RK FILE STATUS IS S2.
           SELECT IX ASSIGN TO "g2rd01i.dat"
               ORGANIZATION INDEXED ACCESS DYNAMIC
               RECORD KEY IX-K FILE STATUS IS S3.
       DATA DIVISION.
       FILE SECTION.
       FD SQ.
       01 SQ-R PIC X(4).
       FD RL.
       01 RL-R PIC X(4).
       FD IX.
       01 IX-R.
          05 IX-K PIC X(4).
       WORKING-STORAGE SECTION.
       01 S1 PIC XX.
       01 S2 PIC XX.
       01 S3 PIC XX.
       01 RK PIC 9(4) VALUE 1.
       01 W PIC X(4) VALUE "KEEP".
       PROCEDURE DIVISION.
       MAIN.
           READ SQ INTO W AT END DISPLAY "SQ-NOTOPEN AT END TAKEN"
           END-READ.
           DISPLAY "SQ-NOTOPEN=" S1 " W=" W.
           OPEN OUTPUT SQ.
           READ SQ INTO W AT END DISPLAY "SQ-OUT AT END TAKEN"
           END-READ.
           DISPLAY "SQ-OUTPUT=" S1 " W=" W.
           CLOSE SQ.
           OPEN EXTEND SQ.
           READ SQ AT END DISPLAY "SQ-EXT AT END TAKEN" END-READ.
           DISPLAY "SQ-EXTEND=" S1.
           CLOSE SQ.
           OPEN OUTPUT RL.
           READ RL INVALID KEY DISPLAY "RL INVALID TAKEN" END-READ.
           DISPLAY "RL-OUT-RANDOM=" S2.
           READ RL NEXT AT END DISPLAY "RL AT END TAKEN" END-READ.
           DISPLAY "RL-OUT-NEXT=" S2.
           CLOSE RL.
           OPEN I-O RL.
           READ RL NEXT AT END DISPLAY "RL-IO AT END" END-READ.
           DISPLAY "RL-IO-NEXT=" S2.
           CLOSE RL.
           OPEN OUTPUT IX.
           MOVE "AAAA" TO IX-K.
           READ IX KEY IS IX-K INVALID KEY DISPLAY "IX INVALID TAKEN"
           END-READ.
           DISPLAY "IX-OUT-RANDOM=" S3.
           READ IX NEXT AT END DISPLAY "IX AT END TAKEN" END-READ.
           DISPLAY "IX-OUT-NEXT=" S3.
           CLOSE IX.
           OPEN INPUT SQ.
           READ SQ AT END DISPLAY "SQ-IN AT END" END-READ.
           DISPLAY "SQ-INPUT=" S1.
           CLOSE SQ.
           STOP RUN.
