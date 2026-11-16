      *>--- conformance-case
      *> id: g2rw09_from_user_defined_numeric_function
      *> rules: [2002-GR-14.8.33.3-6, 2002-SR-14.8.33.2-9]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5645. §2002:14.8.33.2 SR9 - "If identifier-1
      *>   references an intrinsic function and the FILE phrase is
      *>   not specified, identifier-1 shall reference an
      *>   alphanumeric, boolean, or national function." (cite.py:
      *>   OK). The restriction is on an INTRINSIC function; a
      *>   user-defined function is a sender the MOVE rules alone
      *>   judge. §2002:14.8.33.2 SR6 - "If record-name-1 is
      *>   specified, identifier-1 or literal-1 shall be valid as a
      *>   sending operand in a MOVE statement specifying
      *>   record-name-1 as the receiving operand." (cite.py: OK):
      *>   a numeric item to an alphanumeric record is a valid MOVE.
      *>   §2002:14.8.33.3 GR6 makes REWRITE R FROM x the MOVE of x
      *>   to R and then the REWRITE. So REWRITE R FROM FUNCTION
      *>   G2RW09N, whose result is PIC 9(3) holding 42, is accepted
      *>   and replaces the record "AAA" with the MOVE's result, the
      *>   integer 42 stored in three characters: [042]. (COBOL-2014
      *>   restates the rule for "a function": §2014:14.9.33.2 SR8,
      *>   so the program is refused there, wiseowl/diagnostics.)
      *>   Fails if the compiler refuses the program, or if the
      *>   record is not [042].
       IDENTIFICATION DIVISION.
       FUNCTION-ID. G2RW09N.
       DATA DIVISION.
       LINKAGE SECTION.
       01 RES PIC 9(3).
       PROCEDURE DIVISION RETURNING RES.
       MAIN-F.
           MOVE 42 TO RES
           GOBACK.
       END FUNCTION G2RW09N.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RW09.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION G2RW09N.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "g2rw09f.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 R PIC X(3).
       WORKING-STORAGE SECTION.
       01 FS PIC XX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT F
           MOVE "AAA" TO R
           WRITE R
           CLOSE F
           OPEN I-O F
           READ F
           REWRITE R FROM FUNCTION G2RW09N
           DISPLAY "RW=" FS
           CLOSE F
           OPEN INPUT F
           READ F
           DISPLAY "R1=[" R "]"
           CLOSE F
           STOP RUN.
       END PROGRAM G2RW09.
