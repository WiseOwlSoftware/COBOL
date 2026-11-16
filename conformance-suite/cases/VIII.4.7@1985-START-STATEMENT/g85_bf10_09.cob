      *>--- conformance-case
      *> id: g85_bf10_09
      *> rules: [1985-SR-VIII.4.7.3-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.7.3 SR2 - "Data-name-1 may be qualified." START RELF KEY IS
      *> ... RK OF KEYS-A, the RELATIVE KEY (itself qualified, §1985:VIII.2.4.3
      *> SR1); KEYS-B holds another RK (value 1) that START must not use.
      *> Derivation: §1985:VIII.4.7.4 GR4 a - the file position indicator is set
      *> to the first record whose key satisfies the comparison; READ NEXT then
      *> returns it and moves its number to the relative key (§1985:VIII.4.5.4
      *> GR14).
      *>   NLT 2 -> R2 RK=2 : the START from RK OF KEYS-A = 2 (KEYS-B's 1 -> R1).
      *>   EQ 3  -> R3 RK=3
      *>   GT 3  -> INVALID : no record 4 or above (GR4 b).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF1009.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RELF ASSIGN TO "bf1009.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS RK OF KEYS-A
               FILE STATUS IS RELF-ST.
       DATA DIVISION.
       FILE SECTION.
       FD  RELF
           LABEL RECORDS ARE STANDARD.
       01  RELF-REC                  PIC X(2).
       WORKING-STORAGE SECTION.
       01  KEYS-A.
           05  RK                  PIC 99.
       01  KEYS-B.
           05  RK                  PIC 99 VALUE 1.
       01  RELF-ST                   PIC XX.
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT RELF.
           MOVE 1 TO RK OF KEYS-A.
           MOVE "R1" TO RELF-REC.
           WRITE RELF-REC INVALID KEY DISPLAY "W-ERR".
           MOVE 2 TO RK OF KEYS-A.
           MOVE "R2" TO RELF-REC.
           WRITE RELF-REC INVALID KEY DISPLAY "W-ERR".
           MOVE 3 TO RK OF KEYS-A.
           MOVE "R3" TO RELF-REC.
           WRITE RELF-REC INVALID KEY DISPLAY "W-ERR".
           CLOSE RELF.
           OPEN INPUT RELF.
           MOVE 2 TO RK OF KEYS-A.
           START RELF KEY IS NOT LESS THAN RK OF KEYS-A
               INVALID KEY DISPLAY "INVALID".
           MOVE 0 TO RK OF KEYS-A.
           READ RELF NEXT RECORD AT END DISPLAY "EOF".
           DISPLAY "NLT 2 -> " RELF-REC " RK=" RK OF KEYS-A.
           MOVE 3 TO RK OF KEYS-A.
           START RELF KEY IS EQUAL TO RK OF KEYS-A
               INVALID KEY DISPLAY "INVALID".
           MOVE 0 TO RK OF KEYS-A.
           READ RELF NEXT RECORD AT END DISPLAY "EOF".
           DISPLAY "EQ 3  -> " RELF-REC " RK=" RK OF KEYS-A.
           MOVE 3 TO RK OF KEYS-A.
           START RELF KEY IS GREATER THAN RK OF KEYS-A
               INVALID KEY DISPLAY "GT 3  -> INVALID".
           CLOSE RELF.
           STOP RUN.
