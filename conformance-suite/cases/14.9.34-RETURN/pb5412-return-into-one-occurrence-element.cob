      *>--- conformance-case
      *> id: pb5412-return-into-one-occurrence-element
      *> rules: [GR-14.9.34.4-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5412 - 1985:XI.4.3.3 SR1: "The storage area associated
      *>   with identifier-1 and the record area associated with file-name-1
      *>   must not be the same storage area." SE (1), the only occurrence
      *>   of a table that is the whole of the SD's second record, is the
      *>   record area; refused COBOL6827 at 85 and 2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5412N3.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "PB5412N3.SRT".
       DATA DIVISION.
       FILE SECTION.
       SD  SF.
       01  SR.
           05 SK PIC X(4).
       01  ST.
           05 SE PIC X(4) OCCURS 1 TIMES.
       PROCEDURE DIVISION.
       M0 SECTION.
       M1.
           SORT SF ON ASCENDING KEY SK
               INPUT PROCEDURE IS I0
               OUTPUT PROCEDURE IS O0.
           STOP RUN.
       I0 SECTION.
       I1.
           MOVE "AAAA" TO SR.
           RELEASE SR.
       O0 SECTION.
       O1.
           RETURN SF INTO SE (1) AT END CONTINUE END-RETURN.
