*>--- conformance-case
*> id: pb5602-indexed-keys-subscripted-2002
*> rules: [2002-SR-12.3.4.12.2-2, 2002-SR-12.3.4.12.2-5, 2002-GR-12.3.4.12.3-3, 2002-SR-12.3.4.5.2-2,
*>    2002-SR-12.3.4.5.2-6, 2002-SR-12.3.4.7.2-1]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB5602 - at COBOL-2002 the RECORD KEY and ALTERNATE RECORD KEY clauses take a subscripted data-name.
      *> §2002:12.3.4.12.2 SR1 ("Data-name-1 and data-name-2 may be qualified") and §2002:12.3.4.5.2 SR1 say only that
      *> the operand may be qualified; §2002:5.1.3 makes a data-name-n operand a "User-defined word, including
      *> qualification and subscription if needed", and a table element is referenced by its subscripts
      *> (§2002:8.4.1.2.2 SR5). §2014 added "Data-name-1 and data-name-2 shall not be subscripted". The subscripts are
      *> integer literals (docs/conformance, D-CLAUSE-SUBSCRIPT): a key's location is fixed
      *> (§2002:12.3.4.12.3 GR3 "their relative location within a record shall be the same as that used when the file
      *> was created"), and the key of occurrence 2 is not the key of occurrence 1.
      *>
      *> The record is two occurrences of a 5-byte entry (key X(3) and alternate X(2)). The prime key is K(2), bytes
      *> 6..8; the alternate key, with duplicates, is A(1), bytes 4..5. Three records are written:
      *>   r1  K(1)=AAA A(1)=QQ  K(2)=ZZZ A(2)=RR
      *>   r2  K(1)=BBB A(1)=QQ  K(2)=MMM A(2)=SS
      *>   r3  K(1)=CCC A(1)=PP  K(2)=TTT A(2)=UU
      *> Derived: the three prime keys are distinct, so every WRITE succeeds ("00"); r2's alternate key QQ equals r1's
      *> and is allowed by WITH DUPLICATES, which completes with the duplicate-key status "02" (§2002:9.1.12
      *> I-O status 02). A sequential READ NEXT returns the records in prime-key order MMM, TTT, ZZZ: r2, r3, r1, so
      *> K(1)
      *> reads BBB, CCC, AAA (and the second record description R2, which shares the record area, reads r2's ten
      *> bytes "BBBQQMMMSS" after the first); the fourth READ NEXT is at end, "10". START KEY IS GREATER THAN K(2) with K(2) = "MMM"
      *> positions on TTT, so the next READ NEXT returns r3 (K(1) = CCC). READ KEY IS A(1) with A(1) = "PP" returns r3.
      *> READ KEY IS K(2) with K(2) = "ZZZ" returns r1 (K(1) = AAA) and with "QQQ" no record: status "23". The legs
      *> fail separately: a compiler that read K(2) as K(1) would order the records AAA, BBB, CCC and key the READ on
      *> the wrong bytes.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5602C.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "pb5602-indexed-keys-subscripted-2002.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS K(2)
               ALTERNATE RECORD KEY IS A(1) WITH DUPLICATES
               FILE STATUS IS FS(1).
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 R1.
          05 T OCCURS 2.
             10 K PIC X(3).
             10 A PIC X(2).
       01 R2 PIC X(10).
       WORKING-STORAGE SECTION.
       01 FST.
          05 FS PIC XX OCCURS 2.
       PROCEDURE DIVISION.
           OPEN OUTPUT F1
           MOVE "AAA" TO K(1)
           MOVE "QQ" TO A(1)
           MOVE "ZZZ" TO K(2)
           MOVE "RR" TO A(2)
           WRITE R1
           DISPLAY "W FS=" FS(1)
           MOVE "BBB" TO K(1)
           MOVE "QQ" TO A(1)
           MOVE "MMM" TO K(2)
           MOVE "SS" TO A(2)
           WRITE R1
           DISPLAY "W FS=" FS(1)
           MOVE "CCC" TO K(1)
           MOVE "PP" TO A(1)
           MOVE "TTT" TO K(2)
           MOVE "UU" TO A(2)
           WRITE R1
           DISPLAY "W FS=" FS(1)
           CLOSE F1
           OPEN INPUT F1
           READ F1 NEXT
           DISPLAY "N K1=" K(1) " FS=" FS(1)
           DISPLAY "R2=" R2
           READ F1 NEXT
           DISPLAY "N K1=" K(1) " FS=" FS(1)
           READ F1 NEXT
           DISPLAY "N K1=" K(1) " FS=" FS(1)
           READ F1 NEXT
           DISPLAY "N FS=" FS(1)
           MOVE "MMM" TO K(2)
           START F1 KEY IS GREATER THAN K(2)
           DISPLAY "S FS=" FS(1)
           READ F1 NEXT
           DISPLAY "N K1=" K(1) " FS=" FS(1)
           MOVE "PP" TO A(1)
           READ F1 KEY IS A(1)
           DISPLAY "KA K1=" K(1) " FS=" FS(1)
           MOVE "ZZZ" TO K(2)
           READ F1 KEY IS K(2)
           DISPLAY "KP K1=" K(1) " FS=" FS(1)
           MOVE "QQQ" TO K(2)
           READ F1 KEY IS K(2)
           DISPLAY "KX FS=" FS(1)
           CLOSE F1
           STOP RUN.
