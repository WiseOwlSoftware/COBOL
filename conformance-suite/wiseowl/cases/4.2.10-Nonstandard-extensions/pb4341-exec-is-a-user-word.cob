      *>--- conformance-case
      *> id: pb4341-exec-is-a-user-word
      *> rules: [GR-4.2.10-1]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4341 - EXEC, SQL and END-EXEC are user-defined words. 4.2.10: documentation "shall specify
      *> any reserved words added for nonstandard extensions" (cite.py OK 4.2.10 3)), and the refusal of an
      *> embedded block (COBOL5818) reserves none: the 8.9 list of no edition holds EXEC or END-EXEC, so EXEC
      *> opens a block only in a source whose every EXEC opens one. Here the source writes EXEC as a word (01
      *> EXEC), so it is read as COBOL throughout: DISPLAY EXEC SQL END-EXEC names three data items.
      *> EXPECTED VALUES, DERIVED: 14.9.11.4 GR1, "The DISPLAY statement causes
      *> the content of each operand to be transferred to the device in the order listed" (cite.py OK
      *> 14.9.11.4 1)), so the line is the three one-character VALUEs in order, ESX; the MOVE gives EXEC the
      *> content of SQL (14.9.25), so the second line is S. It fails if the word EXEC is ever taken for a block
      *> in a program that writes it as a word.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4341U1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 EXEC     PIC X VALUE "E".
       01 SQL      PIC X VALUE "S".
       01 END-EXEC PIC X VALUE "X".
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY EXEC SQL END-EXEC.
           MOVE SQL TO EXEC.
           DISPLAY EXEC.
           STOP RUN.
