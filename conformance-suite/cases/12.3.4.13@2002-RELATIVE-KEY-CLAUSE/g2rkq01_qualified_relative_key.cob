      *>--- conformance-case
      *> id: g2rkq01_qualified_relative_key
      *> rules: [2002-SR-12.3.4.13.2-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:12.3.4.13.2 SR1 - "Data-name-1 may be qualified."
      *>   cite.py --check "§2002:12.3.4.13.2 SR1" -> OK
      *> The rule GRANTS a form, so this ACCEPTED case writes it:
      *> RELATIVE KEY IS RK OF G2. The qualification is load-bearing:
      *> G1, declared FIRST, has its own RK, and every statement below
      *> gives RK OF G1 a value that differs from RK OF G2, so a
      *> compiler that ignored the qualifier, or took the first
      *> declaration, writes and reads other slots.
      *> §2002:12.3.4.13.3 GR2: "The relative key data item associated
      *> with the execution of an input-output statement is the data
      *> item referenced by data-name-1" -> cite.py OK
      *> Derived (each line can fail):
      *>  W1, W2: random WRITE puts the record at the relative record
      *>   number in the relative key data item (§2002:14.8.47.3 GR25b)
      *>   - "R3R3" at 3, "R5R5" at 5 - status "00" each.
      *>  R1: RK OF G2 = 3 (RK OF G1 = 5): random READ makes record 3
      *>   available (§2002:14.8.29.3 GR26) -> "00 R3R3". Had the
      *>   records gone to G1's slots (9 and 3), slot 5 is empty: "23".
      *>  R2: RK OF G2 = 5 (RK OF G1 = 9) -> "00 R5R5".
      *>  R3: RK OF G2 = 9 (RK OF G1 = 3): no record at 9 - "an attempt
      *>   is made to randomly access a record that does not exist in
      *>   the physical file" (§2002:9.1.12.4) -> "23", the INVALID KEY
      *>   branch: "R3 23 INV".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RKQ01.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RL ASSIGN TO "G2RKQ01.REL"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RK OF G2
               FILE STATUS IS RFS.
       DATA DIVISION.
       FILE SECTION.
       FD RL.
       01 RR PIC X(4).
       WORKING-STORAGE SECTION.
       01 RFS PIC XX.
       01 INV PIC X(3).
       01 G1.
          05 RK PIC 9(4).
       01 G2.
          05 RK PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RL
           MOVE 3 TO RK OF G2
           MOVE 9 TO RK OF G1
           MOVE "R3R3" TO RR
           WRITE RR INVALID KEY CONTINUE END-WRITE
           DISPLAY "W1 " RFS
           MOVE 5 TO RK OF G2
           MOVE 3 TO RK OF G1
           MOVE "R5R5" TO RR
           WRITE RR INVALID KEY CONTINUE END-WRITE
           DISPLAY "W2 " RFS
           CLOSE RL
           OPEN INPUT RL
           MOVE 3 TO RK OF G2
           MOVE 5 TO RK OF G1
           MOVE SPACES TO RR
           READ RL INVALID KEY CONTINUE END-READ
           DISPLAY "R1 " RFS " " RR
           MOVE 5 TO RK OF G2
           MOVE 9 TO RK OF G1
           MOVE SPACES TO RR
           READ RL INVALID KEY CONTINUE END-READ
           DISPLAY "R2 " RFS " " RR
           MOVE 9 TO RK OF G2
           MOVE 3 TO RK OF G1
           MOVE "OK " TO INV
           READ RL INVALID KEY MOVE "INV" TO INV END-READ
           DISPLAY "R3 " RFS " " INV
           CLOSE RL
           STOP RUN.
