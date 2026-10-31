      *>--- conformance-case
      *> id: pb4160-verb-phrase-diagnostics-name-the-phrase
      *> rules: [FMT-14.9.4.2, FMT-14.9.12.2, FMT-14.9.22.2,
      *>     FMT-14.9.26.2, FMT-14.9.30.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4160. Each statement below breaks a rule of its
      *> verb's general format (14.9.12.2 DIVIDE Formats 3-5 print
      *> GIVING, 14.9.26.2 MULTIPLY Format 2 prints one BY operand,
      *> 14.9.30.2 READ prints KEY only for the random read,
      *> 14.9.41.2 START, 14.9.51.2 WRITE, 14.9.22.2 INSPECT, and
      *> CALL Format 1 admits no BY VALUE), and each diagnostic must
      *> name the PHRASE it is about as the programmer wrote it.
      *> Before the repair about forty opened with the verb and an
      *> ellipsis (CALL ... USING BY VALUE, READ ... KEY IS), which
      *> the console renders as "CALL . USING BY VALUE": a statement
      *> nobody wrote. The conformance suite's refusal test checks
      *> the RENDERED text of every negative test for that opening,
      *> so this program is the net for the procedure-division
      *> binders.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4160P.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SQF ASSIGN TO "sqf" ORGANIZATION IS SEQUENTIAL.
           SELECT LSF ASSIGN TO "lsf" ORGANIZATION IS LINE SEQUENTIAL.
           SELECT RLF ASSIGN TO "rlf" ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC RELATIVE KEY IS RK.
       DATA DIVISION.
       FILE SECTION.
       FD SQF.
       01 SQR PIC X(4).
       FD LSF.
       01 LSR PIC X(4).
       FD RLF.
       01 RLR PIC X(4).
       WORKING-STORAGE SECTION.
       01 W PIC X(4).
       01 N PIC 9(4).
       01 A PIC 9(4).
       01 B PIC 9(4).
       01 C PIC 9(4).
       01 Q PIC 9(4).
       01 R PIC 9(4).
       01 RK PIC 9(4).
       PROCEDURE DIVISION.
           CALL "X" USING BY VALUE 3.
           INSPECT W TALLYING N FOR FIRST "A".
           INSPECT W REPLACING TRAILING "A" BY "B".
           DIVIDE A BY B.
           DIVIDE A INTO B REMAINDER R.
           DIVIDE A INTO B GIVING Q R REMAINDER C.
           MULTIPLY A BY B C GIVING Q.
           READ SQF KEY IS SQR.
           READ LSF KEY IS LSR.
           START RLF KEY = RK WITH LENGTH 3.
           WRITE SQR AFTER ADVANCING PAGE AT END-OF-PAGE CONTINUE.
           STOP RUN.
