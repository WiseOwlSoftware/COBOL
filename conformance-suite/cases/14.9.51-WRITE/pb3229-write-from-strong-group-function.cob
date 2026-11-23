      *>--- conformance-case
      *> id: pb3229-write-from-strong-group-function
      *> rules: [SR-14.9.51.3-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3229 review round 1, R1-1. §2023:14.9.51.3 SR4: "If identifier-1 is a
      *> function-identifier, it shall reference an alphanumeric or national function." A
      *> user-defined function's temporary item has the class and category of its RETURNING item
      *> (§2023:8.4.3.2.4 GR1), and "Both the class and the category of a strongly-typed group item
      *> are the type-name specified in the TYPE clause" (§2023:8.5.2.1). P3229SF returns G, of the
      *> strong type T, so its category is T: it is no alphanumeric or national function and the
      *> WRITE is refused, its message naming the category T. (The alphanumeric-group twin,
      *> pb3229-write-from-group-function, is accepted.)
       IDENTIFICATION DIVISION.
       FUNCTION-ID. P3229SF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T TYPEDEF STRONG.
          05 T1 PIC X(5).
          05 T2 PIC 9(5).
       LINKAGE SECTION.
       01 G TYPE T.
       PROCEDURE DIVISION RETURNING G.
           MOVE "ABCDE" TO T1 OF G
           MOVE 12345 TO T2 OF G
           GOBACK.
       END FUNCTION P3229SF.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P3229SW.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION P3229SF.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "p3229sw.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 R PIC X(10).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT F
           WRITE R FROM FUNCTION P3229SF
           CLOSE F
           STOP RUN.
       END PROGRAM P3229SW.
