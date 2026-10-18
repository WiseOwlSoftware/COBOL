*>--- conformance-case
*> id: pb3355-cancel-target-strong-group
*> rules: [SR-14.9.5.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3355 - ISO 14.9.5.3 SR1: "Identifier-1 shall be defined as an alphanumeric or national data item"
      *> (cite.py --check 14.9.5.3 "Identifier-1 shall be defined as an alphanumeric or national data item" -> OK).
      *> The rule names a CATEGORY (8.5.2.1: "Use of the name of a data class or data category in the rules of COBOL
      *> refers to the category unless class is specifically indicated" -> OK) and "Both the class and the category
      *> of a strongly-typed group item are the type-name specified in the TYPE clause" (8.5.2.1 -> OK), so the
      *> strongly-typed group V is not an alphanumeric data item and CANCEL V is refused (COBOLNET1696).
      *> Why this can fail: the class lattice answers alphanumeric for a strongly-typed group (15.3 item 2, a function
      *> ARGUMENT), so a screen that asked it accepted the group.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3355CAN1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 REC-T IS TYPEDEF STRONG.
          05 F1 PIC X(8).
       01 V TYPE REC-T.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "PB3355SB" TO F1 OF V
           CANCEL V
           STOP RUN.
