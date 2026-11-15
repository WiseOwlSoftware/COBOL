*>--- conformance-case
*> id: pb5602-record-key-source-subscripted-2002
*> rules: [2002-SR-12.3.4.12.2-2, 2002-SR-12.3.4.12.2-5, 2002-GR-12.3.4.12.3-3]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB5602 - at COBOL-2002 the data-name-2 operands of a RECORD KEY ... SOURCE phrase take subscripts too.
      *> §2002:12.3.4.12.2 SR1 ("Data-name-1 and data-name-2 may be qualified") and §2002:5.1.3 (a data-name-n operand
      *> includes subscription); §2002:12.3.4.12.3 GR2: "Record-key-name-1 defines a record key consisting of the
      *> concatenation of all occurrences of data-name-2 in the order specified."
      *> cite.py: OK  §2002:12.3.4.12.3 GR2
      *>
      *> The key PK is K(2) followed by K(1): the records with K(1)/K(2) = BBB/MMM, CCC/MMM and AAA/ZZZ have the keys
      *> "MMMBBB", "MMMCCC" and "ZZZAAA", ascending in that order. The file is written in ACCESS MODE SEQUENTIAL, so
      *> each WRITE must carry a key greater than the last: all three complete "00" (a key of K(1) alone would end the
      *> third WRITE, AAA after CCC, with the sequence-error status "21"; a key of K(2) alone would end the second
      *> with "21" or the duplicate "22"). A sequential READ NEXT returns the records in key order, so K(1) reads BBB,
      *> CCC, AAA.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5602E.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "pb5602-record-key-source-subscripted-2002.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS PK SOURCE IS K(2) K(1)
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 R1.
          05 K PIC X(3) OCCURS 2.
       WORKING-STORAGE SECTION.
       01 FS PIC XX.
       PROCEDURE DIVISION.
           OPEN OUTPUT F1
           MOVE "BBB" TO K(1)
           MOVE "MMM" TO K(2)
           PERFORM W
           MOVE "CCC" TO K(1)
           MOVE "MMM" TO K(2)
           PERFORM W
           MOVE "AAA" TO K(1)
           MOVE "ZZZ" TO K(2)
           PERFORM W
           CLOSE F1
           OPEN INPUT F1
           READ F1 NEXT
           DISPLAY "N K1=" K(1) " FS=" FS
           READ F1 NEXT
           DISPLAY "N K1=" K(1) " FS=" FS
           READ F1 NEXT
           DISPLAY "N K1=" K(1) " FS=" FS
           CLOSE F1
           STOP RUN.
       W.
           WRITE R1
           DISPLAY "W FS=" FS.
