      *>--- conformance-case
      *> id: pb4381-exec-is-an-index-name
      *> rules: [GR-4.2.10-1]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4381 review R1-1 - EXEC is a user-defined word (8.9 lists none of EXEC, END-EXEC), so a
      *> table may be indexed by an index-name EXEC. Its declaration INDEXED BY EXEC IDX. puts a user-defined
      *> word after EXEC in a sentence with no END-EXEC, the shape of an embedded block left unterminated
      *> (COBOL5842), and no lexical rule can tell them apart: only the grammar can. The source parses as COBOL
      *> with EXEC as a word, so it is COBOL throughout, and neither COBOL5818 nor COBOL5842 is drawn.
      *> EXPECTED VALUES, DERIVED: 14.9.11.4 GR1, "The DISPLAY statement causes the content of each operand to
      *> be transferred to the device in the order listed" (cite.py OK 14.9.11.4 1)); SET index-name TO 2 gives
      *> the index the occurrence number 2, MOVE "A" TO T (IDX) stores into occurrence 2 and DISPLAY T (IDX)
      *> reads it back, so the one line is A. It fails if a user-defined word after an EXEC with no END-EXEC
      *> is taken for an unterminated block in a program that writes EXEC as a word.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4381U1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TBL.
          05 T PIC X OCCURS 3 INDEXED BY EXEC IDX.
       PROCEDURE DIVISION.
       MAIN-P.
           SET EXEC IDX TO 2.
           MOVE "A" TO T (IDX).
           DISPLAY T (IDX).
           STOP RUN.
