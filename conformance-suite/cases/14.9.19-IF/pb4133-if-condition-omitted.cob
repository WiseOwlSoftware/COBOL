*>--- conformance-case
*> id: pb4133-if-condition-omitted
*> rules: [FMT-14.9.19.2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4133 - ISO 14.9.19.2: "IF condition-1 THEN statement-1 [ ELSE statement-2 ] END-IF". The
      *> condition is not bracketed (5.2.6.2: only bracketed portions may be omitted), so IF DISPLAY "X"
      *> END-IF omits it and is in no format. The one fault is the DISPLAY where the condition is due.
      *> ANTLR's single-token deletion took it for a stray word, read "X" as the condition and drew,
      *> after the true COBOL0001, a false COBOL2072 for the empty block that repair left (the count is
      *> pinned by ParseRecoveryDeletionTests and GeneralFormatViolationTests).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4133IF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-N PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF DISPLAY "X" END-IF
           STOP RUN.
