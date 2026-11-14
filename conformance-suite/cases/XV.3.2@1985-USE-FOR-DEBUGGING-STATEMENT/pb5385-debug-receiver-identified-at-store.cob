      *>--- conformance-case
      *> id: pb5385-debug-receiver-identified-at-store
      *> rules: [1985-GR-XV.3.2.4-14, 1985-GR-XV.3.2.4-24]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5385. §1985:XV.3.2.4 GR14: for the reference that
      *> causes the section, "the occurrence number of each level is
      *> entered in" DEBUG-SUB-n, and GR24c makes DEBUG-CONTENTS the
      *> item's content "at the time that control passes to the
      *> debugging section". A subscript is evaluated at the start of
      *> its statement "Unless otherwise specified by the rules for a
      *> statement" (§1985:IV.4.3.8), and these statements specify
      *> otherwise for their receivers, so the reference is the
      *> occurrence the receiver's store identified:
      *> A MOVE: "Any length evaluation or subscripting associated with
      *>   identifier-2 is evaluated immediately before the data is
      *>   moved to the respective data item" (§1985:VI.6.19.4 GR2), so
      *>   MOVE 3 TO SUB NT (SUB) stores NT (3): [+0003] [3]. Fails if
      *>   the section describes NT (1), where SUB pointed at the start.
      *> B the same rule the other way round: MOVE 4 TO NT (SUB) SUB
      *>   stores NT (1) before SUB changes: [+0001] [4]. Fails if the
      *>   section reads the subscript after the statement.
      *> C, D ADD and COMPUTE with several results are "written in the
      *>   same left-to-right sequence that the multiple results are
      *>   specified" (§1985:VI.6.4.6 GR2): ADD 1 TO SUB NT (SUB)
      *>   stores NT (2), COMPUTE SUB NT (SUB) = 4 stores NT (4).
      *> E DIVIDE: "the subscript is evaluated immediately before the
      *>   remainder is stored" (§1985:VI.6.11.4 GR4): SUB is 3 first.
      *> F SET: "Any subscripting associated with identifier-1 is
      *>   evaluated immediately before the value of the respective
      *>   data item is changed" (§1985:VI.6.23.4 GR3d): NT (5).
      *> G a size error on the quotient leaves "the contents of the
      *>   data items referenced by both identifier-3 and identifier-4"
      *>   unchanged (§1985:VI.6.11.4 GR8a), so NT is not changed and
      *>   its section for the item alone does not run (GR6c). Fails if
      *>   a D1 line follows "G SIZE ERROR".
      *> H READ INTO: "Any subscripting associated with identifier-1 is
      *>   evaluated after the record has been read and immediately
      *>   before it is moved to the data item" (§1985:VII.4.4.4 GR8b):
      *>   the record read carries FR-SUB = 3, so XE (3) receives "3Z".
      *> I an ALL REFERENCES section runs for a statement that names
      *>   the item (GR5) although the zero divisor stored nothing: SUB
      *>   still holds 2, so the reference is A-E (2).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5385.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. ANY-COMPUTER WITH DEBUGGING MODE.
       OBJECT-COMPUTER. ANY-COMPUTER.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "PB5385F".
       DATA DIVISION.
       FILE SECTION.
       FD  F.
       01  FR.
           05 FR-SUB PIC 9.
           05 FR-X   PIC X.
       WORKING-STORAGE SECTION.
       01  T.
           05 NT PIC 9 OCCURS 5 TIMES.
       01  X.
           05 XE PIC X(2) OCCURS 5 TIMES INDEXED BY IX.
       01  A-TAB.
           05 A-E PIC 9 OCCURS 5 TIMES.
       01  SUB PIC 9.
       01  Q   PIC 9.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION.
           USE FOR DEBUGGING ON NT XE.
       D1P.
           DISPLAY "D1 " DEBUG-NAME (1:2) " [" DEBUG-SUB-1 "] ["
               DEBUG-CONTENTS (1:2) "]".
       D2 SECTION.
           USE FOR DEBUGGING ON ALL REFERENCES OF A-E.
       D2P.
           DISPLAY "D2 " DEBUG-NAME (1:3) " [" DEBUG-SUB-1 "] ["
               DEBUG-CONTENTS (1:1) "]".
       END DECLARATIVES.
       M SECTION.
       M1.
           MOVE ZERO TO T. MOVE 1 TO SUB.
           MOVE 3 TO SUB NT (SUB).
           DISPLAY "A " T.
           MOVE ZERO TO T. MOVE 1 TO SUB.
           MOVE 4 TO NT (SUB) SUB.
           DISPLAY "B " T.
           MOVE ZERO TO T. MOVE 1 TO SUB.
           ADD 1 TO SUB NT (SUB).
           DISPLAY "C " T.
           MOVE ZERO TO T. MOVE 1 TO SUB.
           COMPUTE SUB NT (SUB) = 4.
           DISPLAY "D " T.
           MOVE ZERO TO T. MOVE 1 TO SUB.
           DIVIDE 7 BY 2 GIVING SUB REMAINDER NT (SUB).
           DISPLAY "E " T.
           MOVE ZERO TO T. MOVE 1 TO SUB. SET IX TO 5.
           SET SUB NT (SUB) TO IX.
           DISPLAY "F " T.
           MOVE ZERO TO T. MOVE 1 TO SUB.
           DIVIDE 50 BY 2 GIVING Q REMAINDER NT (SUB)
               ON SIZE ERROR DISPLAY "G SIZE ERROR".
           DISPLAY "G " T.
           OPEN OUTPUT F. MOVE "3Z" TO FR. WRITE FR. CLOSE F.
           MOVE ALL "." TO X.
           OPEN INPUT F. MOVE "1A" TO FR.
           READ F INTO XE (FR-SUB) AT END DISPLAY "H AT END".
           CLOSE F.
           DISPLAY "H " X.
           MOVE ZERO TO A-TAB. MOVE 2 TO SUB.
           DIVIDE 7 BY 0 GIVING SUB REMAINDER A-E (SUB)
               ON SIZE ERROR DISPLAY "I SIZE ERROR".
           DISPLAY "I " A-TAB " " SUB.
           STOP RUN.
