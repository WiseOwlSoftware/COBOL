*>--- conformance-case
*> id: pb5602-collating-key-subscripted-2002
*> rules: [2002-SR-12.3.4.6.2-6, 2002-SR-12.3.4.6.2-7, 2002-SR-12.3.4.12.2-2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB5602 - at COBOL-2002 a key-level COLLATING SEQUENCE operand names the subscripted key the RECORD KEY
      *> clause names. §2002:12.3.4.6.2 SR4 requires data-name-1 to be "a name specified as the data-name in an
      *> ALTERNATE RECORD KEY clause or in a RECORD KEY clause", and SR1 of those clauses says only "may be qualified"
      *> while §2002:5.1.3 makes the operand include subscription (docs/conformance, D-CLAUSE-SUBSCRIPT).
      *> cite.py: OK  §2002:12.3.4.6.2 SR4 - "Data-name-1 shall be a name specified as the data-name in an ALTERNATE
      *>   RECORD KEY clause or in a RECORD KEY clause in the file control entry."
      *>
      *> The record is two occurrences of a 3-byte key; the prime key is K(2), ordered by the alphabet REV
      *> "ZYXWVUTSRQPONMLKJIHGFEDCBA" (§2002:12.3.4.6.3: the key's values are ordered by that collating sequence). Three
      *> records: K(2) = AAA (K(1) ONE), MMM (TWO), ZZZ (THR). Under REV the letter Z precedes M, which precedes A, so
      *> the ascending order of the prime key is ZZZ, MMM, AAA and a sequential READ NEXT returns THR, TWO, ONE.
      *> The file is written in ACCESS MODE SEQUENTIAL, so each WRITE must carry a key greater than the last one under the
      *> file's collating sequence: ZZZ, MMM, AAA are written in that order and complete with status "00" only under REV
      *> (native order would end the second WRITE with the sequence-error status "21"). A compiler that ignored the clause
      *> (or applied it to K(1)) fails on those statuses and on the read order.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5602D.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET REV IS "ZYXWVUTSRQPONMLKJIHGFEDCBA".
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "pb5602-collating-key-subscripted-2002.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS K(2)
               COLLATING SEQUENCE OF K(2) IS REV
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
           MOVE "ZZZ" TO K(2)
           MOVE "THR" TO K(1)
           PERFORM W
           MOVE "MMM" TO K(2)
           MOVE "TWO" TO K(1)
           PERFORM W
           MOVE "AAA" TO K(2)
           MOVE "ONE" TO K(1)
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
