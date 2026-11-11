      *>--- conformance-case
      *> id: pb5178_routine_name_later_verb_word
      *> rules: [1985-FMT-VI.6.12.2, 1985-SR-VI.6.12.3-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      * X3.23-1985 VI 6.12.2: "ENTER language-name-1 [ routine-name-1
      * ] ."; VI 6.12.3 SR2: "Routine-name-1 is a COBOL word". FREE,
      * UNLOCK and VALIDATE are verbs from ISO 2002 on and reserve
      * nothing at 85, so each is a routine-name here. The slot took
      * an IDENTIFIER token only, so FREE began a FREE statement and
      * the sentence was refused at its period (kb/Work PB5178). The
      * test fails if any sentence is refused; ENTER is inert, so the
      * program displays END.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5178A.
       PROCEDURE DIVISION.
       P1.
           ENTER FORTRAN FREE.
           ENTER FORTRAN UNLOCK.
           ENTER FORTRAN VALIDATE.
           ENTER COBOL.
           DISPLAY "END".
           STOP RUN.
