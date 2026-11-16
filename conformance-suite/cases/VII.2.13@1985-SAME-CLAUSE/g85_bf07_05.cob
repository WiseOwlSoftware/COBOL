      *>--- conformance-case
      *> id: g85_bf07_05
      *> rules: [1985-SR-VII.2.13.3-3, 1985-GR-VII.2.13.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.2.13.3 SR3 - "More than one SAME clause may be
      *> included in the program", subject to a (a file-name in at
      *> most one SAME AREA clause), b (at most one SAME RECORD AREA
      *> clause) and c (every file-name of a SAME AREA clause that
      *> meets a SAME RECORD AREA clause is in it, and "additional
      *> file-names not appearing in that SAME AREA clause may also
      *> appear in that SAME RECORD AREA clause"). The I-O-CONTROL
      *> paragraph below holds four SAME clauses that keep all three:
      *>   SAME AREA F1 F2; SAME RECORD AREA F1 F2 F3 (c, F3 additional);
      *>   SAME AREA F4 F5; SAME RECORD AREA F4 F5 (a and b, disjoint).
      *> The program must be accepted (the SR grants the form).
      *> §1985:VII.2.13.4 GR2 - in a SAME RECORD AREA "All of these files
      *> may be in the open mode at the same time. A logical record in
      *> the SAME RECORD AREA is considered as a logical record of each
      *> file open in the output mode ... and of the most recently read
      *> file open in the input mode ... This is equivalent to an
      *> implicit redefinition of the area, i.e., records are aligned
      *> on the leftmost character position."
      *> W1 - F1 and F3 open OUTPUT together; MOVE "HELLO WORLD" to F1's
      *>   record; F3's record (two fields, 5 + 6) reads HELLO/ WORLD,
      *>   aligned on the leftmost position.
      *> W2 - WRITE F3-REC with no MOVE to it: F3 holds HELLO WORLD.
      *> R1 - F3 open INPUT and F1 open OUTPUT; F1-REC is set to all X,
      *>   then READ F3 makes the record F1's too: F1-REC shows HELLO
      *>   WORLD (unshared, it keeps the Xs); WRITE F1-REC puts it in
      *>   F1 (read back as R2). F1 and F2 (one SAME AREA) are never
      *>   open together (§1985:VII.2.13.4 GR1).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF0705.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "BF0705F1".
           SELECT F2 ASSIGN TO "BF0705F2".
           SELECT F3 ASSIGN TO "BF0705F3".
           SELECT F4 ASSIGN TO "BF0705F4".
           SELECT F5 ASSIGN TO "BF0705F5".
       I-O-CONTROL.
           SAME AREA FOR F1 F2
           SAME RECORD AREA FOR F1 F2 F3
           SAME AREA FOR F4 F5
           SAME RECORD AREA FOR F4 F5.
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       01  F1-REC PIC X(11).
       FD  F2.
       01  F2-REC PIC X(11).
       FD  F3.
       01  F3-REC.
           05  F3-A PIC X(5).
           05  F3-B PIC X(6).
       FD  F4.
       01  F4-REC PIC X(4).
       FD  F5.
       01  F5-REC PIC X(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT F1 F3.
           MOVE "HELLO WORLD" TO F1-REC.
           DISPLAY "W1 [" F3-A "][" F3-B "]".
           WRITE F3-REC.
           CLOSE F1 F3.
           OPEN INPUT F3.
           READ F3 AT END DISPLAY "W2 EOF".
           DISPLAY "W2 [" F3-REC "]".
           CLOSE F3.
           OPEN INPUT F3.
           OPEN OUTPUT F1.
           MOVE ALL "X" TO F1-REC.
           READ F3 AT END DISPLAY "R1 EOF".
           DISPLAY "R1 [" F1-REC "]".
           WRITE F1-REC.
           CLOSE F1 F3.
           OPEN INPUT F1.
           READ F1 AT END DISPLAY "R2 EOF".
           DISPLAY "R2 [" F1-REC "]".
           CLOSE F1.
           STOP RUN.
