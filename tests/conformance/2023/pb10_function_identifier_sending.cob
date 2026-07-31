      *> PB10 - a FUNCTION-IDENTIFIER in the identifier-N SENDING positions that rejected it outright.
      *> 8.4.3.1.2 Format 1 makes a function-identifier an IDENTIFIER, and 8.4.3.2.3 SR1 bars one ONLY from a
      *> RECEIVING operand - so every SENDING position admits one. The grammar reached functionCall from just
      *> four rules, so these were COBOL0001 "no viable alternative" on legal source.
      *>
      *> Each position below is one the STANDARD ITSELF defines as a MOVE sending item, which is why they are
      *> fixed together and why the operand admits exactly what a MOVE sender admits:
      *>   14.9.51.4 GR5a - WRITE   ... FROM identifier-1  ==  MOVE identifier-1 TO record-name-1
      *>   14.9.35.4      - REWRITE ... FROM identifier-1  ==  the same MOVE
      *>   14.9.32.4      - RELEASE ... FROM identifier-1  ==  the same MOVE
      *>   14.9.20.3 SR4  - INITIALIZE ... REPLACING ... BY identifier-2, "the SENDING item" of a MOVE
      *>
      *> All four bind through the ONE WriteSource / operand helper rather than four call sites, so the next
      *> FROM phrase inherits the arm instead of re-deriving it.
      *>
      *> ⛔ NOT COVERED HERE, DELIBERATELY - INSPECT identifier-1 is a SENDING operand only in Format 1
      *> (TALLYING). In 14.9.22.2 Formats 2/3/4 (REPLACING / TALLYING-and-REPLACING / CONVERTING) it is modified
      *> IN PLACE, so SR1 BARS a function-identifier there and admitting it unconditionally would accept illegal
      *> source. That needs a per-format bind-time screen and is tracked separately on PB10.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB10FNSEND.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb10_fn.dat"
               ORGANIZATION IS LINE SEQUENTIAL.
           SELECT SF ASSIGN TO "pb10_fn_sort.tmp".
           SELECT OF1 ASSIGN TO "pb10_fn_out.dat"
               ORGANIZATION IS LINE SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 FREC PIC X(8).
       SD SF.
       01 SREC PIC X(8).
       FD OF1.
       01 OREC PIC X(8).
       WORKING-STORAGE SECTION.
       01 A PIC X(8) VALUE "abcdefgh".
       01 T PIC X(8).
       PROCEDURE DIVISION.
      *> WRITE ... FROM - 15.97.3 r1 upper-cases "abcdefgh" to "ABCDEFGH", and GR5a moves that to the record.
           OPEN OUTPUT F
           WRITE FREC FROM FUNCTION UPPER-CASE(A)
           CLOSE F
           OPEN INPUT F
           READ F INTO T
           CLOSE F
           DISPLAY "WRITE-FROM=" T

      *> INITIALIZE ... REPLACING ... BY - the same sending operand, via 14.9.20.3 SR4.
           MOVE SPACES TO T
           INITIALIZE T REPLACING ALPHANUMERIC BY FUNCTION UPPER-CASE(A)
           DISPLAY "INIT-BY=" T

      *> RELEASE ... FROM - the SORT path, which reaches the same helper through a different binder.
           SORT SF ASCENDING KEY SREC
               INPUT PROCEDURE IS FEED-SORT
               GIVING OF1
           OPEN INPUT OF1
           READ OF1 INTO T
           CLOSE OF1
           DISPLAY "RELEASE-FROM=" T
           STOP RUN.

       FEED-SORT.
           RELEASE SREC FROM FUNCTION LOWER-CASE("ABCDEFGH").
