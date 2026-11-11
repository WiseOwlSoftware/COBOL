      *>--- conformance-case
      *> id: pb5158_io_phrases_reversed
      *> rules: [FMT-14.9.30.2, FMT-14.9.51.2, FMT-14.9.35.2,
      *>    FMT-14.9.41.2, FMT-14.9.10.2, SR-14.9.34.3-4]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5158. The input-output conditional phrases, each
      *> written NOT phrase first. ISO/IEC 1989:2002 Annex F.2 item
      *> 33: "The conditional phrases in the ... DELETE, ... READ,
      *> ... REWRITE, START, ... and WRITE statements may be
      *> specified in any order" (§2002:F.2, cite.py OK); the 2023
      *> formats enclose each pair in choice indicators, whose
      *> "alternatives may be specified in any order" (5.2.6.4,
      *> cite.py OK); RETURN 14.9.34.3 SR4: "The AT END phrase and
      *> the NOT AT END phrase, when specified, may be written in
      *> reversed order" (cite.py OK). COBOL-85 refuses the program
      *> (§1985:IV.2.1), so it is accepted from 2002.
      *> Why each leg can fail: a compiler that binds the phrases by
      *> position instead of by role runs the wrong arm.
      *> Expected values: the first READ delivers the one record (NOT
      *> AT END), the second finds no next record, the at-end
      *> condition (14.9.30.4, AT END); a WRITE of a key already in
      *> the file and a READ, START or DELETE of a key that is not
      *> are the invalid key condition (9.1.14, cite.py OK), the
      *> others succeed (NOT INVALID KEY); the sort returns its one
      *> record and then meets the at-end condition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5158IO.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "PB5158SF.DAT".
           SELECT IX ASSIGN TO "PB5158IX.DAT" ORGANIZATION INDEXED
               ACCESS MODE DYNAMIC RECORD KEY IK.
           SELECT SR ASSIGN TO "PB5158SR.TMP".
       DATA DIVISION.
       FILE SECTION.
       FD  SF.
       01  SREC PIC X(10).
       FD  IX.
       01  IREC.
           05 IK  PIC X(4).
           05 ID1 PIC X(6).
       SD  SR.
       01  RREC PIC X(10).
       PROCEDURE DIVISION.
       P0 SECTION.
       P1.
           OPEN OUTPUT SF.
           MOVE "B" TO SREC.
           WRITE SREC.
           CLOSE SF.
           OPEN INPUT SF.
           READ SF NOT AT END DISPLAY "R1 NOT " SREC
               AT END DISPLAY "R1 END" END-READ.
           READ SF NOT AT END DISPLAY "R2 NOT " SREC
               AT END DISPLAY "R2 END" END-READ.
           CLOSE SF.
           OPEN OUTPUT IX.
           MOVE "K001" TO IK.
           MOVE "DATA" TO ID1.
           WRITE IREC NOT INVALID KEY DISPLAY "W1 NOT"
               INVALID KEY DISPLAY "W1 INV" END-WRITE.
           WRITE IREC NOT INVALID KEY DISPLAY "W2 NOT"
               INVALID KEY DISPLAY "W2 INV" END-WRITE.
           CLOSE IX.
           OPEN I-O IX.
           MOVE "K999" TO IK.
           READ IX KEY IS IK NOT INVALID KEY DISPLAY "RD NOT"
               INVALID KEY DISPLAY "RD INV" END-READ.
           MOVE "K001" TO IK.
           READ IX KEY IS IK NOT INVALID KEY DISPLAY "RD NOT"
               INVALID KEY DISPLAY "RD INV" END-READ.
           REWRITE IREC NOT INVALID KEY DISPLAY "RW NOT"
               INVALID KEY DISPLAY "RW INV" END-REWRITE.
           MOVE "K999" TO IK.
           START IX KEY = IK NOT INVALID KEY DISPLAY "ST NOT"
               INVALID KEY DISPLAY "ST INV" END-START.
           DELETE IX RECORD NOT INVALID KEY DISPLAY "DL NOT"
               INVALID KEY DISPLAY "DL INV" END-DELETE.
           MOVE "K001" TO IK.
           DELETE IX RECORD NOT INVALID KEY DISPLAY "DL NOT"
               INVALID KEY DISPLAY "DL INV" END-DELETE.
           CLOSE IX.
           SORT SR ON ASCENDING KEY RREC USING SF
               OUTPUT PROCEDURE P2.
           STOP RUN.
       P2 SECTION.
       P3.
           RETURN SR NOT AT END DISPLAY "RT NOT " RREC
               AT END DISPLAY "RT END" END-RETURN.
           RETURN SR NOT AT END DISPLAY "RT NOT " RREC
               AT END DISPLAY "RT END" END-RETURN.
