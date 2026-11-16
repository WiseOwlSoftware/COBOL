      *>--- conformance-case
      *> id: g2-pd-byref-same-storage
      *> rules: [2002-GR-14.1.3-8]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.1.3 GR8 - "If the argument is passed by reference,
      *>   the activated runtime element operates as if the formal
      *>   parameter occupies the same storage area as the argument."
      *> Three arguments, each passed BY REFERENCE TWICE so that two
      *>   formals (§2002:14.1.3 GR2: "established on a positional
      *>   basis") stand for one argument: a store through the first is
      *>   seen through the second inside the callee, and by the caller.
      *>   A copy (by-content semantics) shows neither.
      *>   1 CALL, working-storage WS-A.
      *>   2 CALL, a COMMUNICATION SECTION CD record item: 2002 admits it
      *>     (§2002:14.8.4.2 SR3: "a data item defined in the file,
      *>     working-storage, local-storage, linkage, or communication
      *>     section"). CDIN-REC implicitly redefines the CD's record
      *>     area (§2002:A.1.2.2 SR5), and SYMBOLIC QUEUE IN-QUEUE is
      *>     "an elementary alphanumeric data item of 12 characters
      *>     occupying positions 1 through 12 in the record"
      *>     (§2002:A.1.2.3 GR2a), so CD-Q is IN-QUEUE's storage. The
      *>     facility is obsolete (COBOL0903 warning does not fail it).
      *>   3 INVOKE, working-storage WS-B, into a method's formals.
      *> DERIVATION.
      *>   L-B AFTER L-A MOVE [ZZZZZZ] - L-A and L-B are both WS-A.
      *>   SUB SEES L-C [ORDERS      ] - the caller's MOVE "ORDERS" TO
      *>     IN-QUEUE is in CD-Q's storage, which L-C occupies.
      *>   L-D AFTER L-C MOVE [CHANGEDQUEUE] - L-C and L-D are CD-Q.
      *>   M-B AFTER M-A MOVE [MMMMMM] - M-A and M-B are both WS-B.
      *>   WS-A [ZZZZZZ] / CD-Q [CHANGEDQUEUE] / QUEUE[CHANGEDQUEUE] /
      *>   WS-B [MMMMMM] - the callee's stores are in the caller's
      *>     items, and IN-QUEUE shares CD-Q's positions 1-12.
      *> Fails if: any argument is copied in or out (a bracket shows the
      *>   old value), or the CD record is not the CD area (QUEUE line).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2PDBR.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2PDBRC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  O    USAGE OBJECT REFERENCE G2PDBRC.
       01  WS-A PIC X(6) VALUE "WSWSWS".
       01  WS-B PIC X(6) VALUE "BBBBBB".
       COMMUNICATION SECTION.
       CD  CDIN FOR INPUT
           SYMBOLIC QUEUE IS IN-QUEUE
           SYMBOLIC SOURCE IS IN-SOURCE
           TEXT LENGTH IS IN-LEN
           END KEY IS IN-END
           STATUS KEY IS IN-STATUS
           MESSAGE COUNT IS IN-COUNT.
       01  CDIN-REC.
           05  CD-Q   PIC X(12).
           05  FILLER PIC X(75).
       PROCEDURE DIVISION.
       P1.
           MOVE "ORDERS" TO IN-QUEUE.
           CALL "G2PDBRS" USING BY REFERENCE WS-A WS-A CD-Q CD-Q.
           INVOKE G2PDBRC "NEW" RETURNING O.
           INVOKE O "M1" USING BY REFERENCE WS-B WS-B.
           DISPLAY "WS-A [" WS-A "]".
           DISPLAY "CD-Q [" CD-Q "]".
           DISPLAY "QUEUE[" IN-QUEUE "]".
           DISPLAY "WS-B [" WS-B "]".
           STOP RUN.
       END PROGRAM G2PDBR.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2PDBRS.
       DATA DIVISION.
       LINKAGE SECTION.
       01  L-A PIC X(6).
       01  L-B PIC X(6).
       01  L-C PIC X(12).
       01  L-D PIC X(12).
       PROCEDURE DIVISION USING L-A L-B L-C L-D.
       P2.
           MOVE "ZZZZZZ" TO L-A.
           DISPLAY "L-B AFTER L-A MOVE [" L-B "]".
           DISPLAY "SUB SEES L-C [" L-C "]".
           MOVE "CHANGEDQUEUE" TO L-C.
           DISPLAY "L-D AFTER L-C MOVE [" L-D "]".
           GOBACK.
       END PROGRAM G2PDBRS.

       IDENTIFICATION DIVISION.
       CLASS-ID. G2PDBRC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. M1.
       DATA DIVISION.
       LINKAGE SECTION.
       01  M-A PIC X(6).
       01  M-B PIC X(6).
       PROCEDURE DIVISION USING M-A M-B.
       P3.
           MOVE "MMMMMM" TO M-A.
           DISPLAY "M-B AFTER M-A MOVE [" M-B "]".
       END METHOD M1.
       END OBJECT.
       END CLASS G2PDBRC.
