      *>--- conformance-case
      *> id: g2clo03_same_record_area_lock
      *> rules: [2002-GR-14.8.6.3-7]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.6.3 GR7 -- for a CLOSE without the UNIT phrase, "If
      *> file-name-1 is specified in a SAME RECORD AREA clause, the
      *> record area is available to the runtime element if any of the
      *> file connectors referenced by the other file-names in that SAME
      *> RECORD AREA clause are open." Pinned for plain CLOSE and for
      *> CLOSE ... WITH LOCK (a 2002 alternative later editions removed).
      *> §2002:12.3.6.3 GR2: the shared area holds the record "of the
      *> most recently-read file open in the input mode" and "is
      *> equivalent to an implicit redefinition of the area", so A-REC
      *> and B-REC show one content. After FA is closed while FB is
      *> open the area stays available: it keeps A's record, and a MOVE
      *> into it is seen through both names. Nothing is displayed from
      *> the area once no member is open (GR7: unavailable).
      *>   P-READ=00 A=AAAAAA B=AAAAAA    P-CLOSE=00 A=AAAAAA B=AAAAAA
      *>   P-MOVE A=CCCCCC B=CCCCCC
      *>   L-READ=00 A=AAAAAA B=AAAAAA    L-CLOSE=00 A=AAAAAA B=AAAAAA
      *>   L-MOVE A=DDDDDD B=DDDDDD       L-READB=00 A=BBBBBB B=BBBBBB
      *>   L-CLOSEB=00                    RE-A=38 (§2002:9.1.12.5)
      *> §2002:9.1.12: a 38 status is fatal and the implementor decides
      *> whether execution continues; DOC-A.1-103: it continues,
      *> because FA has a FILE STATUS clause and EC-I-O checking is not
      *> enabled.
      *> Fails if a CLOSE of one member drops the shared area while the
      *> other is open, or WITH LOCK handles the area differently.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2L3M.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FA ASSIGN TO "g2l3a.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS A-ST.
           SELECT FB ASSIGN TO "g2l3b.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS B-ST.
       I-O-CONTROL.
           SAME RECORD AREA FOR FA FB.
       DATA DIVISION.
       FILE SECTION.
       FD FA.
       01 A-REC PIC X(6).
       FD FB.
       01 B-REC PIC X(6).
       WORKING-STORAGE SECTION.
       01 A-ST PIC XX.
       01 B-ST PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT FA
           MOVE "AAAAAA" TO A-REC
           WRITE A-REC
           CLOSE FA
           OPEN OUTPUT FB
           MOVE "BBBBBB" TO B-REC
           WRITE B-REC
           CLOSE FB
           OPEN INPUT FA
           OPEN INPUT FB
           READ FA AT END CONTINUE END-READ
           DISPLAY "P-READ=" A-ST " A=" A-REC " B=" B-REC
           CLOSE FA
           DISPLAY "P-CLOSE=" A-ST " A=" A-REC " B=" B-REC
           MOVE "CCCCCC" TO B-REC
           DISPLAY "P-MOVE A=" A-REC " B=" B-REC
           CLOSE FB
           OPEN INPUT FA
           OPEN INPUT FB
           READ FA AT END CONTINUE END-READ
           DISPLAY "L-READ=" A-ST " A=" A-REC " B=" B-REC
           CLOSE FA WITH LOCK
           DISPLAY "L-CLOSE=" A-ST " A=" A-REC " B=" B-REC
           MOVE "DDDDDD" TO B-REC
           DISPLAY "L-MOVE A=" A-REC " B=" B-REC
           READ FB AT END CONTINUE END-READ
           DISPLAY "L-READB=" B-ST " A=" A-REC " B=" B-REC
           CLOSE FB WITH LOCK
           DISPLAY "L-CLOSEB=" B-ST
           OPEN INPUT FA
           DISPLAY "RE-A=" A-ST
           STOP RUN.
