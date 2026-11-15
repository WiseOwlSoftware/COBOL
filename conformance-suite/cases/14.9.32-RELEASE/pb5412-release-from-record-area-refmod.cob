      *>--- conformance-case
      *> id: pb5412-release-from-record-area-refmod
      *> rules: [GR-14.9.32.4-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5412 - 1985:XI.4.2.3 SR3: "Record-name-1 and identifier-1
      *>   must not reference the same storage area." SR (1:) is the whole
      *>   of SR (2023:8.4.3.3.4 GR5c: the slice runs to the rightmost
      *>   position); refused COBOL6827 at 85 and 2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5412N2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "PB5412N2.SRT".
       DATA DIVISION.
       FILE SECTION.
       SD  SF.
       01  SR.
           05 SK PIC X(4).
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
           RELEASE SR FROM SR (1:).
       O0 SECTION.
       O1.
           RETURN SF AT END CONTINUE END-RETURN.
