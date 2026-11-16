      *>--- conformance-case
      *> id: g2can06_three_operand_forms
      *> rules: [2002-FMT-14.8.5.1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.5.1 - CANCEL takes identifier-1, literal-1 or
      *> program-prototype-name-1, repeated ("..."). One CANCEL statement
      *> here names all three alternatives: identifier-1 WA (holding
      *> "G2CN6A"), literal-1 "G2CN6B" and program-prototype-name-1
      *> G2CN6C (a prototype in the REPOSITORY paragraph). A second
      *> statement repeats one alternative (identifier-1 twice).
      *> §2002:14.8.5.3 GR1 identifies the program by "the content of the
      *> data item referenced by identifier-1", "the value of literal-1"
      *> or "program-prototype-name-1"; GR3: a program canceled and
      *> called again "is in its initial state", so each counter restarts.
      *> Each subprogram counts its activations in working storage
      *> (VALUE 0). Derived lines:
      *>   call A, B, C twice:             A=1 B=1 C=1 A=2 B=2 C=2
      *>   CANCEL WA "G2CN6B" G2CN6C, call: A=1 B=1 C=1
      *>   CANCEL WA WB, call A and B:      A=1 B=1
      *>   call C (not canceled):          C=2
      *> Fails if: any of the three operand forms (or the repetition) is
      *> refused at 2002, or a CANCEL through it does not restore the
      *> initial state (A=2, B=2 or C=2 where 1 is derived), or the last
      *> CANCEL reaches C (C=1 on the last line).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CN6C IS PROTOTYPE.
       PROCEDURE DIVISION.
       END PROGRAM G2CN6C.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CN6M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM G2CN6C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WA PIC X(6) VALUE "G2CN6A".
       01 WB PIC X(6) VALUE "G2CN6B".
       PROCEDURE DIVISION.
       MAIN.
           CALL "G2CN6A"
           CALL "G2CN6B"
           CALL G2CN6C
           CALL "G2CN6A"
           CALL "G2CN6B"
           CALL G2CN6C
           CANCEL WA "G2CN6B" G2CN6C
           CALL "G2CN6A"
           CALL "G2CN6B"
           CALL G2CN6C
           CANCEL WA WB
           CALL "G2CN6A"
           CALL "G2CN6B"
           CALL G2CN6C
           STOP RUN.
       END PROGRAM G2CN6M.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CN6A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CNT PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       S1.
           ADD 1 TO CNT
           DISPLAY "A=" CNT
           GOBACK.
       END PROGRAM G2CN6A.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CN6B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CNT PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       S1.
           ADD 1 TO CNT
           DISPLAY "B=" CNT
           GOBACK.
       END PROGRAM G2CN6B.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CN6C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CNT PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       S1.
           ADD 1 TO CNT
           DISPLAY "C=" CNT
           GOBACK.
       END PROGRAM G2CN6C.
