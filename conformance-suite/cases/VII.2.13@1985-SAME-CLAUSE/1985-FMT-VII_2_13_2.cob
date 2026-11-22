      *>--- conformance-case
      *> id: 1985-FMT-VII_2_13_2
      *> rules: [1985-FMT-VII.2.13.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.2.13.2 (THE SAME CLAUSE), the general format
      *>   SAME [ RECORD ] AREA FOR file-name-1 { file-name-2 } ...
      *> grants AREA and FOR as optional words (neither is underlined),
      *> with or without RECORD, and two or more file-names. The
      *> I-O-CONTROL paragraph below writes each: SAME AREA FOR F1 F2,
      *> SAME F3 F4 (AREA FOR omitted), SAME RECORD F1 F2 F5 (RECORD with
      *> AREA FOR omitted, three files, F5 additional as §1985:VII.2.13.3
      *> SR3 c allows) and SAME RECORD AREA FOR F3 F4. The program must
      *> translate and run at 85 (kb/Work PB5740: a general format is
      *> witnessed by an accepted case, never by refusals alone).
      *> §1985:VII.2.13.4 GR2: "A logical record in the SAME RECORD AREA
      *> is considered as a logical record of each file open in the
      *> output mode", so with F1 and F5 open OUTPUT, F1's record moved
      *> to is F5's (aligned on the leftmost position): SHARED [ABC][DEF],
      *> and WRITE F5-REC writes ABCDEF. F3 and F4 (one SAME AREA) are
      *> never open together (GR1) and keep their own files.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SAMEFMT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "SAMEFMF1".
           SELECT F2 ASSIGN TO "SAMEFMF2".
           SELECT F3 ASSIGN TO "SAMEFMF3".
           SELECT F4 ASSIGN TO "SAMEFMF4".
           SELECT F5 ASSIGN TO "SAMEFMF5".
       I-O-CONTROL.
           SAME AREA FOR F1 F2
           SAME F3 F4
           SAME RECORD F1 F2 F5
           SAME RECORD AREA FOR F3 F4.
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       01  F1-REC PIC X(6).
       FD  F2.
       01  F2-REC PIC X(6).
       FD  F3.
       01  F3-REC PIC X(6).
       FD  F4.
       01  F4-REC PIC X(6).
       FD  F5.
       01  F5-REC.
           05  F5-A PIC X(3).
           05  F5-B PIC X(3).
       PROCEDURE DIVISION.
       P0.
           OPEN OUTPUT F1 F5.
           MOVE "ABCDEF" TO F1-REC.
           DISPLAY "SHARED [" F5-A "][" F5-B "]".
           WRITE F5-REC.
           CLOSE F1 F5.
           OPEN OUTPUT F3.
           MOVE "THREE" TO F3-REC.
           WRITE F3-REC.
           CLOSE F3.
           OPEN OUTPUT F4.
           MOVE "FOUR" TO F4-REC.
           WRITE F4-REC.
           CLOSE F4.
           OPEN INPUT F5.
           READ F5 AT END DISPLAY "F5 EOF".
           DISPLAY "F5 [" F5-REC "]".
           CLOSE F5.
           OPEN INPUT F3.
           READ F3 AT END DISPLAY "F3 EOF".
           DISPLAY "F3 [" F3-REC "]".
           CLOSE F3.
           OPEN INPUT F4.
           READ F4 AT END DISPLAY "F4 EOF".
           DISPLAY "F4 [" F4-REC "]".
           CLOSE F4.
           STOP RUN.
