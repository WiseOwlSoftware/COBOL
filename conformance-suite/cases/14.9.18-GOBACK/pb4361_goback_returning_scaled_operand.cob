*>--- conformance-case
*> id: pb4361_goback_returning_scaled_operand
*> rules: [GR-14.9.18.4-2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4361. GOBACK RETURNING is an IMPLEMENTOR EXTENSION (ISO 14.9.18.2 has no RETURNING phrase), so
      *> 14.9.18.3 SR6 ("Identifier-2 shall reference an integer data item or a data item with usage display or
      *> usage national") governs the WITH ... STATUS phrase only and is no rule of this statement. With no header
      *> RETURNING item the operand is the main program exit status, screened as the sender of the MOVE the
      *> extension stands for (14.9.25.3 SR1: not index, message-tag, object or pointer). A scaled COMP-3 item was
      *> refused under SR6 although the same statement is accepted when a header RETURNING item exists. 14.9.18.4
      *> GR10 hands the OS an integer: 0.5 truncates to 0 (CONFORMANCE.md item 192), so the run exits 0.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4361POS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 SCALED PIC S9(3)V99 COMP-3 VALUE 0.5.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "RAN".
           GOBACK RETURNING SCALED.
