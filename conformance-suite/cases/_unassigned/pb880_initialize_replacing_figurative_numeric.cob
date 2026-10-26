      *>--- conformance-case
      *> id: pb880_initialize_replacing_figurative_numeric
      *> rules: []
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 1989:2023 14.9.20.4 GR4: "Otherwise, the implicit statement is: MOVE sending-operand TO
      *> receiving-operand." INITIALIZE G REPLACING NUMERIC DATA BY ALL "X" is therefore MOVE ALL "X" TO N
      *> for the numeric item N - permitted through ISO 2014 (14.9.25.3 SR5 removed it in 2023; Annex E.2
      *> item 1), and stored as the explicit MOVE stores it: three "X" characters in N's character positions
      *> (the pre-removal fill semantics - MOVE ALL "X" TO PIC 9(3) leaves "XXX").
      *> kb/Work PB880: the implicit MOVE was built by the EMITTER, after the bind-time pass that gives N the
      *> image-backed storage a character fill needs, so this ABORTED the run unit ("without image-backed
      *> storage") at every edition. The move is bound now. The explicit twin at the end is the control: both
      *> forms must print the same thing, and N2 is touched ONLY by INITIALIZE so no other statement can have
      *> supplied the storage fact.
      *> The same program at --std 2023 is refused COBOL0902 (kb/Work PB879 - the negative twin
      *> pb879-initialize-replacing-high-value-numeric-2023). This test wrote BY SPACE until kb/Work PB3605:
      *> SPACE into a numeric item is refused at every edition, so it has no pre-removal fill to witness.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB880I85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 N2 PIC 9(3) VALUE 123.
          05 A2 PIC X(3) VALUE "XYZ".
       01 N3 PIC 9(3) VALUE 456.
       PROCEDURE DIVISION.
           INITIALIZE G REPLACING NUMERIC DATA BY ALL "X"
           DISPLAY "N2=[" N2 "] A2=[" A2 "]"
           MOVE ALL "X" TO N3
           DISPLAY "N3=[" N3 "]"
           STOP RUN.
