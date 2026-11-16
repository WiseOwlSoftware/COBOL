      *>--- conformance-case
      *> id: g2-inspect-converting-overlap
      *> rules: [2002-GR-14.8.21.3-20]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.21.3 GR20 - identifier-4, identifier-6 or
      *> identifier-7 of a CONVERTING occupying the storage of
      *> identifier-1: "the result of the execution of this statement is
      *> undefined" (also §2002:B.2 item 22 f), §2002:3.3). Every
      *> line pins the DOCUMENTED IMPLEMENTOR CHOICE
      *> docs/conformance/3.1/D-INS4
      *> (kb/Work PB1907): the sets and delimiter are taken when the
      *> statement starts; the conversion itself follows
      *> §2002:14.8.21.3 GR19 and GR22 (first occurrence of a repeated
      *> character).
      *> Why it can fail: a set that observes a conversion its own
      *> statement made.
      *> EXPECTED (X5 = "ABCA", X6 = "ABBA", X7 = "ABAB", X8 = "ABBA"):
      *>   F1  CONVERTING X5 TO "WXYZ": the from-set is "ABCA", so
      *>       A->W B->X C->Y, the second A is ignored   [WXYW]
      *>   F2  CONVERTING "AB" TO X6(3:2): the to-set is "BA"
      *>       -> A->B B->A                             [BAAB]
      *>   F3  CONVERTING "AB" TO "BA" AFTER X7(1:1): the delimiter is
      *>       "A", the region starts after the first one: "BAB" ->
      *>       "ABA", so "A" + "ABA"                    [AABA]
      *>   F4  CONVERTING X8(2:2) TO "XY": the from-set is "BB", B->X
      *>                                                [AXXA]
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2INS04.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X5   PIC X(4) VALUE "ABCA".
       01 X6   PIC X(4) VALUE "ABBA".
       01 X7   PIC X(4) VALUE "ABAB".
       01 X8   PIC X(4) VALUE "ABBA".
       PROCEDURE DIVISION.
       MAIN.
           INSPECT X5 CONVERTING X5 TO "WXYZ"
           DISPLAY "F1=[" X5 "]"
           INSPECT X6 CONVERTING "AB" TO X6(3:2)
           DISPLAY "F2=[" X6 "]"
           INSPECT X7 CONVERTING "AB" TO "BA" AFTER X7(1:1)
           DISPLAY "F3=[" X7 "]"
           INSPECT X8 CONVERTING X8(2:2) TO "XY"
           DISPLAY "F4=[" X8 "]"
           STOP RUN.
