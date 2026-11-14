*>--- conformance-case
*> id: pb5188-sort-merge-key-index-data-item
*> rules: [SR-13.18.60.3-10, SR-14.9.40.3-6, SR-14.9.24.3-4]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB5188. §2023:13.18.60.3 SR10: "An index data item may be referenced explicitly only in a SEARCH
      *> or SET statement, a relation condition, an intrinsic function argument, an inline method invocation
      *> argument, the USING phrase of a procedure division header, or the USING phrase of a CALL or INVOKE
      *> statement." A KEY phrase references its key data item explicitly and is none of them, so the SORT key
      *> and the MERGE key SK, an index data item, are each refused (COBOL1757). §1985:VI.5.14.3 SR5's shorter
      *> list names no key either, so the refusal holds at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5188K.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "PB5188K1".
           SELECT F2 ASSIGN TO "PB5188K2".
           SELECT F3 ASSIGN TO "PB5188K3".
           SELECT S1 ASSIGN TO "PB5188KS".
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       01  R1 PIC X(12).
       FD  F2.
       01  R2 PIC X(12).
       FD  F3.
       01  R3 PIC X(12).
       SD  S1.
       01  SR.
           05  SK USAGE INDEX.
           05  SD1 PIC X(4).
       PROCEDURE DIVISION.
       P1.
           SORT S1 ASCENDING KEY SK USING F1 GIVING F3.
           MERGE S1 ASCENDING KEY SK USING F1 F2 GIVING F3.
           STOP RUN.
