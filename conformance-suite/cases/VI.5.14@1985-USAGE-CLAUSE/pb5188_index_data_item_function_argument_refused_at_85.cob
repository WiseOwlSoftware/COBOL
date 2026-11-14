      *>--- conformance-case
      *> id: pb5188_index_data_item_function_argument_refused_at_85
      *> rules: [1985-SR-VI.5.14.3-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5188. §1985:VI.5.14.3 SR5: "An index data item can be
      *> referenced explicitly only in a SEARCH or SET statement, a
      *> relation condition, the USING phrase of a Procedure Division
      *> header, or the USING phrase of a CALL statement." An intrinsic
      *> function argument is none of them, so FUNCTION ORD-MAX (IX IY)
      *> and FUNCTION LENGTH (IX) are refused at 85 (COBOL6979, lines 30
      *> and 32). COBOL-2002 adds "an intrinsic function argument" to
      *> the list (§2002:13.16.58.2 SR9; §2023:13.18.60.3 SR10), where
      *> the program is legal.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5188F.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  TBL.
           05  ELT PIC X OCCURS 5 TIMES INDEXED BY XI.
       01  IX USAGE IS INDEX.
       01  IY USAGE IS INDEX.
       01  N PIC 99 VALUE 0.
       PROCEDURE DIVISION.
       P1.
           SET XI TO 3.
           SET IX TO XI.
           SET IY TO XI.
           COMPUTE N = FUNCTION ORD-MAX (IX IY).
           DISPLAY N.
           COMPUTE N = FUNCTION LENGTH (IX).
           DISPLAY N.
           STOP RUN.
