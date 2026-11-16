      *>--- conformance-case
      *> id: g85_bf18_08
      *> rules: [1985-GR-IX.4.5.4-17]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.4.5.4 GR17 - a format 2 (random) READ of an indexed
      *> file compares the key of reference with the stored records
      *> "until the first record having an equal value is found. In the
      *> case of an alternate key with duplicate values, the first
      *> record found is the first record of a sequence of duplicates
      *> which was released to the mass storage control system (MSCS)."
      *> "If no record can be so identified, the invalid key condition
      *> exists". The records are released (WRITE, random access) in
      *> the prime key order 30, 10, 20 under alternate value XX and 25,
      *> 05 under YY, so a release-order answer and a prime-key-order
      *> answer differ: READ KEY IS IX-ALT finds 30 for XX (not 10) and
      *> 25 for YY (not 05). The prime key leg (KEY phrase absent, the
      *> prime key is the key of reference, §1985:IX.4.5.4 GR16) finds
      *> the one record 20; ZZ and prime key 99 match nothing, so the
      *> INVALID KEY phrase runs. After the XX read, the dynamic access
      *> mode keeps IX-ALT as the key of reference for READ NEXT
      *> (§1985:IX.4.5.4 GR15), which walks the rest of the XX set in
      *> release order (§1985:IX.4.5.4 GR4 e, GR14): 10, 20, then the YY
      *> set 25, 05. Each leg fails if the format 2 READ returns the
      *> lowest prime key of the set, the last one released, or any
      *> record for a value no record holds.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF1808.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IX ASSIGN TO "G85BF1808.DAT"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS IX-KEY
               ALTERNATE RECORD KEY IS IX-ALT WITH DUPLICATES
               FILE STATUS IS IX-ST.
       DATA DIVISION.
       FILE SECTION.
       FD  IX.
       01  IX-REC.
           05  IX-KEY PIC XX.
           05  IX-ALT PIC XX.
           05  IX-VAL PIC X(4).
       WORKING-STORAGE SECTION.
       01  IX-ST PIC XX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT IX.
           MOVE "30XXREL1" TO IX-REC.
           WRITE IX-REC INVALID KEY DISPLAY "W30 INVALID".
           MOVE "10XXREL2" TO IX-REC.
           WRITE IX-REC INVALID KEY DISPLAY "W10 INVALID".
           MOVE "20XXREL3" TO IX-REC.
           WRITE IX-REC INVALID KEY DISPLAY "W20 INVALID".
           MOVE "25YYREL4" TO IX-REC.
           WRITE IX-REC INVALID KEY DISPLAY "W25 INVALID".
           MOVE "05YYREL5" TO IX-REC.
           WRITE IX-REC INVALID KEY DISPLAY "W05 INVALID".
           CLOSE IX.
           OPEN INPUT IX.
           MOVE SPACES TO IX-REC.
           MOVE "YY" TO IX-ALT.
           READ IX KEY IS IX-ALT
               INVALID KEY DISPLAY "YY INVALID".
           DISPLAY "ALT-YY " IX-REC.
           MOVE SPACES TO IX-REC.
           MOVE "20" TO IX-KEY.
           READ IX
               INVALID KEY DISPLAY "P20 INVALID".
           DISPLAY "PRIME-20 " IX-REC.
           MOVE "99" TO IX-KEY.
           READ IX
               INVALID KEY DISPLAY "PRIME-99 INVALID KEY".
           MOVE "ZZ" TO IX-ALT.
           READ IX KEY IS IX-ALT
               INVALID KEY DISPLAY "ALT-ZZ INVALID KEY".
           MOVE SPACES TO IX-REC.
           MOVE "XX" TO IX-ALT.
           READ IX KEY IS IX-ALT
               INVALID KEY DISPLAY "XX INVALID".
           DISPLAY "ALT-XX " IX-REC.
           READ IX NEXT RECORD AT END DISPLAY "NEXT-1 AT END".
           DISPLAY "NEXT-1 " IX-REC.
           READ IX NEXT RECORD AT END DISPLAY "NEXT-2 AT END".
           DISPLAY "NEXT-2 " IX-REC.
           READ IX NEXT RECORD AT END DISPLAY "NEXT-3 AT END".
           DISPLAY "NEXT-3 " IX-REC.
           READ IX NEXT RECORD AT END DISPLAY "NEXT-4 AT END".
           DISPLAY "NEXT-4 " IX-REC.
           READ IX NEXT RECORD AT END DISPLAY "NEXT-5 AT END".
           CLOSE IX.
           STOP RUN.
