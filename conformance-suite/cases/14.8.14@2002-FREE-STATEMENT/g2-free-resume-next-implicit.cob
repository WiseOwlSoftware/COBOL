      *>--- conformance-case
      *> id: g2-free-resume-next-implicit
      *> rules: [2002-GR-14.8.14.3-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.14.3 GR2 - "If more than one data-name-1 is
      *>   specified in a FREE statement, the result of executing this
      *>   FREE statement is the same as if a separate FREE statement had
      *>   been written for each data-name-1 in the same order as
      *>   specified in the FREE statement. If an implicit FREE statement
      *>   results in the execution of a declarative procedure that
      *>   executes a RESUME statement with the NEXT STATEMENT phrase,
      *>   processing resumes at the next implicit FREE statement, if
      *>   any."
      *> Context: §2002:14.8.14.3 GR1a - allocated storage "is released
      *>   and the data-pointer referenced by data-name-1 is set to
      *>   NULL"; GR1c - otherwise (not allocated, not NULL) "the
      *>   EC-STORAGE-NOT-ALLOC exception condition is set to exist".
      *>   §2002:14.5.12.1 - with checking enabled (>>TURN) the condition
      *>   is raised and the USE AFTER EXCEPTION declarative runs.
      *> DERIVATION of every .out line:
      *>   FREE P R Q: P is freed first (P NULL, Q still set); R holds
      *>   the address of WORKING-STORAGE item W, never ALLOCATEd, so the
      *>   implicit FREE R raises EC-STORAGE-NOT-ALLOC. In the
      *>   declarative P is already NULL and Q not yet freed (order):
      *>     DECL-1 P=NULL Q=LIVE
      *>   RESUME NEXT STATEMENT resumes at the next implicit FREE (Q),
      *>   not after the whole FREE, so Q is freed:
      *>     F1 P=NULL Q=NULL
      *>   FREE R P2 S: R and S both unallocated, P2 allocated. The
      *>   implicit FREE R runs first and raises; P2 is not yet freed:
      *>     DECL-2 P2=LIVE
      *>   processing resumes at the next implicit FREE (P2, freed), then
      *>   FREE S raises again and the declarative runs again:
      *>     DECL-3 P2=NULL
      *>     F2 P2=NULL K=3
      *> CAN FAIL: a RESUME that skips the remaining operands (F1
      *>   Q=LIVE, no DECL-3, F2 P2=LIVE), operands freed out of order
      *>   (DECL-1 P=LIVE or Q=NULL, DECL-2 P2=NULL), or no exception
      *>   for R/S (K<3).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2FRE01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P  USAGE POINTER.
       01 Q  USAGE POINTER.
       01 P2 USAGE POINTER.
       01 R  USAGE POINTER.
       01 S  USAGE POINTER.
       01 W  PIC X(4).
       01 K  PIC 9 VALUE 0.
       01 T1 PIC X(4).
       01 T2 PIC X(4).
       PROCEDURE DIVISION.
       DECLARATIVES.
       HNA SECTION.
           USE AFTER EXCEPTION CONDITION EC-STORAGE-NOT-ALLOC.
       HNA-P.
           ADD 1 TO K
           IF K = 1
               PERFORM SHOW-PQ
               DISPLAY "DECL-1 P=" T1 " Q=" T2
           ELSE
               MOVE "LIVE" TO T1
               IF P2 = NULL MOVE "NULL" TO T1 END-IF
               DISPLAY "DECL-" K " P2=" T1
           END-IF
           RESUME AT NEXT STATEMENT.
       SHOW-PQ.
           MOVE "LIVE" TO T1 T2
           IF P = NULL MOVE "NULL" TO T1 END-IF
           IF Q = NULL MOVE "NULL" TO T2 END-IF.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           >>TURN EC-STORAGE-NOT-ALLOC CHECKING ON
           SET R TO ADDRESS OF W
           SET S TO ADDRESS OF W
           ALLOCATE 4 CHARACTERS RETURNING P
           ALLOCATE 4 CHARACTERS RETURNING Q
           ALLOCATE 4 CHARACTERS RETURNING P2
           FREE P R Q
           PERFORM SHOW-PQ
           DISPLAY "F1 P=" T1 " Q=" T2
           FREE R P2 S
           MOVE "LIVE" TO T1
           IF P2 = NULL MOVE "NULL" TO T1 END-IF
           DISPLAY "F2 P2=" T1 " K=" K
           STOP RUN.
