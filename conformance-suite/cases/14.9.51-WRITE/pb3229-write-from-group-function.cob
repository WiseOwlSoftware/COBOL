      *>--- conformance-case
      *> id: pb3229-write-from-group-function
      *> rules: [SR-14.9.51.3-4]
      *> accept: [2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3229 review R1-1. §2023:14.9.51.3 SR4: "If identifier-1 is a function-identifier, it shall
      *> reference an alphanumeric or national function." A user-defined function's temporary item has "the
      *> description, class, and category" of its RETURNING item (§2023:8.4.3.2.4 GR1), and "an alphanumeric
      *> group item has class and category alphanumeric" (§2023:8.5.2.1). So P3229GF, which returns the
      *> alphanumeric group G, is an alphanumeric function and WRITE R FROM FUNCTION P3229GF is legal; its
      *> value is G's ten characters, "ABCDE" then the display digits of 12345 (§2023:14.9.51.4 GR5: the
      *> FROM phrase moves identifier-1 to record-name-1, a group move, §2023:14.9.25.4 GR4).
      *> Expected: [ABCDE12345].
       IDENTIFICATION DIVISION.
       FUNCTION-ID. P3229GF.
       DATA DIVISION.
       LINKAGE SECTION.
       01 G.
          05 G1 PIC X(5).
          05 G2 PIC 9(5).
       PROCEDURE DIVISION RETURNING G.
           MOVE "ABCDE" TO G1
           MOVE 12345 TO G2
           GOBACK.
       END FUNCTION P3229GF.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P3229GW.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION P3229GF.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "p3229gw.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 R PIC X(10).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT F
           WRITE R FROM FUNCTION P3229GF
           CLOSE F
           OPEN INPUT F
           READ F
           DISPLAY "[" R "]"
           CLOSE F
           STOP RUN.
       END PROGRAM P3229GW.
