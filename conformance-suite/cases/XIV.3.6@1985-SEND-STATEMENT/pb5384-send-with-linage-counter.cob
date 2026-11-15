      *>--- conformance-case
      *> id: pb5384-send-with-linage-counter
      *> rules: [1985-SR-XIV.3.6.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      * X3.23-1985 XIV 3.6.3 SR2: "Identifier-2 must reference a
      * one-character integer without an operational sign." The
      * LINAGE-COUNTER of a LINAGE IS 60 file is two characters: its
      * implicit description "is that of an unsigned integer whose
      * size is equal to the size of integer-1" (VII 1.3.7). It was
      * accepted because every special register passed the screen
      * whatever its size (kb/Work PB5384); it is refused with
      * COBOL6171. ISO 2002 A.2.6.2 SR3 is the same rule.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5384C.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PFILE ASSIGN TO "PFILE.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  PFILE LINAGE IS 60 LINES.
       01  PREC PIC X(80).
       WORKING-STORAGE SECTION.
       01  MSG PIC X(10) VALUE "HELLO".
       COMMUNICATION SECTION.
       CD  OUT-CD FOR OUTPUT
           DESTINATION COUNT IS OUT-COUNT
           STATUS KEY IS OUT-ST
           SYMBOLIC DESTINATION IS OUT-DEST.
       PROCEDURE DIVISION.
       P-PARA.
           OPEN OUTPUT PFILE.
           SEND OUT-CD FROM MSG WITH LINAGE-COUNTER.
           CLOSE PFILE.
           STOP RUN.
