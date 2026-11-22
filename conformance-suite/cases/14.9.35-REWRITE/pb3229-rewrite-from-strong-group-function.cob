      *>--- conformance-case
      *> id: pb3229-rewrite-from-strong-group-function
      *> rules: [SR-14.9.35.3-9]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3229 review round 1, R1-1, the REWRITE sibling of
      *> pb3229-write-from-strong-group-function. §2023:14.9.35.3 SR9: "If identifier-1 references a
      *> function and the FILE phrase is not specified, identifier-1 shall reference an alphanumeric,
      *> boolean, or national function." P3229RF returns G, of the strong type T, whose class and
      *> category are the type-name T (§2023:8.5.2.1; §2023:8.4.3.2.4 GR1 gives the function's
      *> temporary item its RETURNING item's category), so the REWRITE is refused, naming the category
      *> T. At COBOL-2002 the rule restricts only an intrinsic function (§2002:14.8.33.2 SR9), so the
      *> refusal starts at COBOL-2014 ("a function").
       IDENTIFICATION DIVISION.
       FUNCTION-ID. P3229RF.
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
       END FUNCTION P3229RF.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P3229RW.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION P3229RF.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "p3229rw.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 R PIC X(10).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN I-O F
           READ F
           REWRITE R FROM FUNCTION P3229RF
           CLOSE F
           STOP RUN.
       END PROGRAM P3229RW.
