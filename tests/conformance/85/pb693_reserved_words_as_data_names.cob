      *> kb/Work PB693 - THE OTHER HALF OF THE RESERVATION GATE: at an
      *> edition where ISO 8.9 does NOT reserve the word, it stays an
      *> ordinary user-defined word, declared AND referenced.
      *>
      *> The gate the fix generates is `{userWordHere("W")}?` on each
      *> cobolWord alternative, not a deletion, precisely so this
      *> program keeps compiling: 8.3.2.1 rule 1 bars a reserved word
      *> from the user-defined-word slot, and none of these nine
      *> words is reserved in X3.23-1985.  Each frees at a different
      *> boundary (reserved-words.json intervals), so the program
      *> walks the whole 8.9 timeline in one compile:
      *>
      *>   reserved from 2002 - UNLOCK, SHARING, RETRY (file sharing
      *>                        and record locking), RAISING (the EC
      *>                        family), CONSTANT (the 13.10 constant
      *>                        entry), PROGRAM-POINTER (13.18.60)
      *>   reserved from 2014 - FUNCTION-POINTER (13.18.60)
      *>   reserved from 2023 - XOR, COMMIT
      *>
      *> Both positions are exercised for each: the DECLARATION in
      *> WORKING-STORAGE and a REFERENCE in a statement operand - the
      *> reference matters because the gate withdraws the word from
      *> the operand slot at the editions that DO reserve it, and at
      *> 85 it must not.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB693WORDS85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  UNLOCK           PIC X(3) VALUE "UNL".
       01  SHARING          PIC X(3) VALUE "SHA".
       01  RETRY            PIC X(3) VALUE "RET".
       01  RAISING          PIC X(3) VALUE "RAI".
       01  CONSTANT         PIC X(3) VALUE "CON".
       01  PROGRAM-POINTER  PIC X(3) VALUE "PGP".
       01  FUNCTION-POINTER PIC X(3) VALUE "FNP".
       01  XOR              PIC X(3) VALUE "XOR".
       01  COMMIT           PIC X(3) VALUE "COM".
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "1=" UNLOCK
           DISPLAY "2=" SHARING
           DISPLAY "3=" RETRY
           DISPLAY "4=" RAISING
           DISPLAY "5=" CONSTANT
           DISPLAY "6=" PROGRAM-POINTER
           DISPLAY "7=" FUNCTION-POINTER
           DISPLAY "8=" XOR
           DISPLAY "9=" COMMIT
           MOVE "ZZZ" TO UNLOCK
           DISPLAY "A=" UNLOCK
           STOP RUN.
