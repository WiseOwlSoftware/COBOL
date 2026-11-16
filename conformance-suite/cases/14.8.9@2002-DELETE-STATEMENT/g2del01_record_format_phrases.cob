      *>--- conformance-case
      *> id: g2del01_record_format_phrases
      *> rules: [2002-FMT-14.8.9.1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.9.1 -- "DELETE file-name-1 RECORD [ retry-phrase ]"
      *> with the choice-bracketed INVALID KEY / NOT INVALID KEY
      *> phrases (zero, either or both, in either order) and the
      *> optional END-DELETE; RECORD is an optional word; "where
      *> retry-phrase is described in 14.6.8, RETRY phrase" (RETRY
      *> n TIMES, RETRY FOR n SECONDS, RETRY FOREVER). Every form is
      *> written and run. No record locking is specified, so no
      *> record operation conflict arises and the RETRY phrase leaves
      *> the outcome as it would be without it (§2002:14.6.8.2 GR4
      *> applies only to an operation "unsuccessful on the first
      *> attempt because of a file sharing conflict condition or a
      *> record operation conflict condition").
      *> Outcomes: §2002:14.8.9.3 GR3 (indexed, dynamic: the record of
      *> the prime key is removed; "If the physical file does not
      *> contain the record specified by the key, the invalid key
      *> condition exists"), GR4 (relative, random: the record of the
      *> relative key), GR2 (sequential access: the record of the
      *> preceding READ); I-O status 00 on success (§2002:9.1.12.1)
      *> and 23 for "a record that does not exist in the physical
      *> file" (§2002:9.1.12.4).
      *> Indexed file IX holds keys 001..004:
      *>  D1 RECORD, RETRY 2 TIMES, ON then NOT ON, key 001 -> D1 OK 00
      *>  D2 no RECORD, RETRY FOR 3 SECONDS, NOT ON then ON, key 009
      *>                                               -> D2 INV 23
      *>  D3 RECORD RETRY FOREVER END-DELETE, key 002     -> D3 00
      *>  D4 INVALID KEY alone, period, key 002 again (removed by
      *>     D3, GR5 "can no longer be accessed")        -> D4 INV 23
      *>  D5 NOT INVALID KEY alone, key 003              -> D5 OK 00
      *>  D6 no phrase at all, key 004                   -> D6 00
      *>  D7 a random READ of 003 finds it gone            -> D7 23
      *> Relative file RS, sequential access, no INVALID KEY phrase
      *> (§2002:14.8.9.2 SR2): READ then DELETE RS          -> D8 00
      *>  then the next READ gets record 2 ("BBBB")      -> D8B BBBB
      *> Relative file RR, random access, relative key 1 then 5:
      *>  D9 NOT ON alone, RECORD, END-DELETE             -> D9 OK 00
      *>  D10 INVALID KEY alone, END-DELETE              -> D10 INV 23
      *> Fails if a form is refused, a phrase runs on the wrong
      *> outcome, or a DELETE removes the wrong record or none.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DEL1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IX ASSIGN TO "g2del01i.dat"
               ORGANIZATION INDEXED ACCESS DYNAMIC
               RECORD KEY K FILE STATUS S.
           SELECT RS ASSIGN TO "g2del01s.dat"
               ORGANIZATION RELATIVE ACCESS SEQUENTIAL
               FILE STATUS T.
           SELECT RR ASSIGN TO "g2del01r.dat"
               ORGANIZATION RELATIVE ACCESS RANDOM
               RELATIVE KEY RK FILE STATUS U.
       DATA DIVISION.
       FILE SECTION.
       FD IX.
       01 IR.
          05 K PIC X(3).
          05 D PIC X(5).
       FD RS.
       01 SR PIC X(4).
       FD RR.
       01 QR PIC X(4).
       WORKING-STORAGE SECTION.
       01 S  PIC XX.
       01 T  PIC XX.
       01 U  PIC XX.
       01 RK PIC 9(4).
       01 N  PIC 9 VALUE 2.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT IX
           MOVE "001" TO K MOVE "ONE" TO D WRITE IR
           MOVE "002" TO K MOVE "TWO" TO D WRITE IR
           MOVE "003" TO K MOVE "THREE" TO D WRITE IR
           MOVE "004" TO K MOVE "FOUR" TO D WRITE IR
           CLOSE IX
           OPEN I-O IX
           MOVE "001" TO K
           DELETE IX RECORD RETRY N TIMES
               INVALID KEY DISPLAY "D1 INV " S
               NOT INVALID KEY DISPLAY "D1 OK " S
           END-DELETE
           MOVE "009" TO K
           DELETE IX RETRY FOR N + 1 SECONDS
               NOT INVALID KEY DISPLAY "D2 OK " S
               INVALID KEY DISPLAY "D2 INV " S
           END-DELETE
           MOVE "002" TO K
           DELETE IX RECORD RETRY FOREVER END-DELETE
           DISPLAY "D3 " S
           DELETE IX INVALID KEY DISPLAY "D4 INV " S.
           MOVE "003" TO K
           DELETE IX NOT INVALID KEY DISPLAY "D5 OK " S.
           MOVE "004" TO K
           DELETE IX
           DISPLAY "D6 " S
           MOVE "003" TO K
           READ IX INVALID KEY CONTINUE END-READ
           DISPLAY "D7 " S
           CLOSE IX
           OPEN OUTPUT RS
           MOVE "AAAA" TO SR WRITE SR
           MOVE "BBBB" TO SR WRITE SR
           CLOSE RS
           OPEN I-O RS
           READ RS
           DELETE RS RECORD
           DISPLAY "D8 " T
           READ RS
           DISPLAY "D8B " SR
           CLOSE RS
           OPEN OUTPUT RR
           MOVE 1 TO RK MOVE "CCCC" TO QR WRITE QR
           MOVE 2 TO RK MOVE "DDDD" TO QR WRITE QR
           CLOSE RR
           OPEN I-O RR
           MOVE 1 TO RK
           DELETE RR RECORD
               NOT INVALID KEY DISPLAY "D9 OK " U
           END-DELETE
           MOVE 5 TO RK
           DELETE RR
               INVALID KEY DISPLAY "D10 INV " U
           END-DELETE
           CLOSE RR
           STOP RUN.
