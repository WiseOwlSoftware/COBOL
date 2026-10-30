*>--- conformance-case
*> id: pb4089_string_into_numeric_display
*> rules: [SR-14.9.43.3-1, GR-14.9.43.4-3, GR-14.9.43.4-6,
*>    GR-14.9.43.4-7]
*> accept: [2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB4089 - a numeric item of usage display as the STRING statement's
      *>   identifier-3 holds CHARACTERS, and only the positions written change.
      *> cite.py --check 14.9.43.3 "all identifiers, except identifier-4, shall
      *>   be described implicitly or explicitly as usage display or national"
      *>   -> OK  14.9.43.3 1)  (a numeric item of usage display is legal; SR5
      *>   forbids only an edited item and the JUSTIFIED clause)
      *> cite.py --check 14.9.43.4 "in accordance with the MOVE statement rules
      *>   for alphanumeric-to-alphanumeric moves" -> OK  14.9.43.4 3) a)
      *> cite.py --check 14.9.43.4 "the moves behave as though the characters
      *>   were moved one at a time" -> OK  14.9.43.4 6)
      *> cite.py --check 14.9.43.4 "only the portion of the data item referenced
      *>   by identifier-3 that was referenced during the execution of the STRING
      *>   statement is changed" -> OK  14.9.43.4 7)
      *> Derivation (each receiver is shown through a reference modification,
      *>   which is an alphanumeric item over the receiver's character positions,
      *>   so no numeric reading intervenes):
      *>   A N PIC 9(6) holds 123456. "AB" moves one character at a time into
      *>     positions 1 and 2 (pointer 1, GR5); positions 3-6 keep "3456" (GR7):
      *>     "AB3456". It is not re-read as a number: 3456 would give 003456.
      *>   B the same N holds 123456 again and the pointer is 3: "CD" fills
      *>     positions 3 and 4, the pointer becomes 5 (GR6): "12CD56".
      *>   C SS PIC S9(6) SIGN TRAILING SEPARATE holds 123456 and so the seven
      *>     characters "123456+" (the sign has its own character position).
      *>     "AB" fills 1-2 and the sign position is untouched: "AB3456+".
      *>   D pointer 7 is the sign position, which receives "-": "123456-".
      *>   E the characters written may be digits: N holds 123456, pointer 5,
      *>     "99" fills 5-6: "123499" is a valid number and ADD 1 gives 123500.
      *>   F the receiver is one occurrence of a table: TE(2) holds 1234, "ZZ"
      *>     fills 1-2 -> "ZZ34"; the other occurrences keep 1234.
      *>   G the receiver shares its storage with a REDEFINES item: the same
      *>     characters AB34 are seen through the redefining item.
      *>   H the RECEIVER is the redefining item: G4 redefines G3, which holds
      *>     1234; "XY" fills positions 1-2 of the one storage area, so G3
      *>     reads "XY34" (GR6, GR7). A redefining item owns no storage of its
      *>     own, so the characters reach the item it redefines.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4089A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9(6) VALUE 123456.
       01 SS PIC S9(6) SIGN TRAILING SEPARATE VALUE 123456.
       01 PT PIC 9 VALUE 1.
       01 TB.
          05 TE PIC 9(4) OCCURS 3 VALUE 1234.
       01 IX PIC 9 VALUE 2.
       01 G1 PIC 9(4) VALUE 1234.
       01 G2 REDEFINES G1 PIC 9(4).
       01 G3 PIC 9(4) VALUE 1234.
       01 G4 REDEFINES G3 PIC 9(4).
       PROCEDURE DIVISION.
           STRING "AB" DELIMITED BY SIZE INTO N
           DISPLAY "A[" N(1:6) "]"
           MOVE 123456 TO N
           MOVE 3 TO PT
           STRING "CD" DELIMITED BY SIZE INTO N WITH POINTER PT
           DISPLAY "B[" N(1:6) "] " PT
           STRING "AB" DELIMITED BY SIZE INTO SS
           DISPLAY "C[" SS(1:7) "]"
           MOVE 123456 TO SS
           MOVE 7 TO PT
           STRING "-" DELIMITED BY SIZE INTO SS WITH POINTER PT
           DISPLAY "D[" SS(1:7) "]"
           MOVE 123456 TO N
           MOVE 5 TO PT
           STRING "99" DELIMITED BY SIZE INTO N WITH POINTER PT
           ADD 1 TO N
           DISPLAY "E[" N "]"
           STRING "ZZ" DELIMITED BY SIZE INTO TE(IX)
           DISPLAY "F[" TE(1) "/" TE(2)(1:4) "/" TE(3) "]"
           STRING "AB" DELIMITED BY SIZE INTO G1
           DISPLAY "G[" G2(1:4) "]"
           STRING "XY" DELIMITED BY SIZE INTO G4
           DISPLAY "H[" G3(1:4) "/" G4(1:4) "]"
           STOP RUN.
