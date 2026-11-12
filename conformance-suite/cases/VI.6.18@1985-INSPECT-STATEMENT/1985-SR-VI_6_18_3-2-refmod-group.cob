      *>--- conformance-case
      *> id: 1985-SR-VI_6_18_3-2-refmod-group
      *> rules: [1985-SR-VI.6.18.3-2]
      *> accept: [85]
      *> source-format: fixed
      *>---
      *> kb/Work PB5216. §1985:VI.6.18.3 SR2: "Identifier-3, ... ,
      *> identifier-n must reference an elementary item described,
      *> implicitly or explicitly, as USAGE IS DISPLAY." A reference-
      *> modified identifier references a unique data item that "is
      *> considered an elementary data item without the JUSTIFIED
      *> clause" (§1985:IV.4.3.8.4.4 GR5), and a group whose usage is
      *> DISPLAY may be reference modified (§1985:IV.4.3.8.4.3 SR1), so
      *> G (1:2) is the elementary item "AB" and G (1:1), G (2:1) are
      *> the elementary items "A" and "B": each is a legal identifier-3,
      *> identifier-5, identifier-6 and identifier-7. Each of the three
      *> statements fails if the compiler refuses the group's slice.
      *> Expected, from the rules: the string ABABCABAAB holds AB four
      *> times (positions 1, 3, 6 and 9); REPLACING ALL "AB" BY "AB"
      *> leaves it unchanged; CONVERTING "A" TO "B" turns every A into
      *> B, giving BBBBCBBBBB.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. INSRMG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G.
           05 G1 PIC X VALUE "A".
           05 G2 PIC X VALUE "B".
       01  X PIC X(10) VALUE "ABABCABAAB".
       01  N PIC 99 VALUE 0.
       01  NX PIC XX.
       PROCEDURE DIVISION.
       P1.
           INSPECT X TALLYING N FOR ALL G (1:2).
           MOVE N TO NX.
           DISPLAY "N " NX.
           INSPECT X REPLACING ALL "AB" BY G (1:2).
           INSPECT X CONVERTING G (1:1) TO G (2:1).
           DISPLAY "X " X.
           STOP RUN.
