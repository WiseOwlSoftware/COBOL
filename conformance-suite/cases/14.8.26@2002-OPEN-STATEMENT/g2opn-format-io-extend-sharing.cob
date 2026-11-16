      *>--- conformance-case
      *> id: g2opn-format-io-extend-sharing
      *> rules: [2002-FMT-14.8.26.1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.26.1 (general format) - the arms the other 2002
      *>   OPEN cases do not write: EXTEND, I-O, SHARING WITH NO OTHER,
      *>   SHARING WITH READ ONLY, two file-names in one group, and two
      *>   groups of different modes in one OPEN. (INPUT, OUTPUT,
      *>   retry-phrase and SHARING WITH ALL OTHER: pb316_open_group_
      *>   scope; WITH NO REWIND: g2002_open_norewind_input_output.)
      *> DERIVATION (2002 text only). A successful OPEN sets the open
      *>   mode named by its keyword (§2002:14.8.26.3 GR2) and I-O
      *>   status 00 (§2002:9.1.12.1); the files exist after O1.
      *>   O1 OPEN OUTPUT SQ IX (GR19: as two OPENs): 00 00; SQ gets
      *>     AAAA, IX gets 1one.
      *>   O2 OPEN EXTEND SHARING WITH NO OTHER SQ: 00; EXTEND
      *>     positions after the last record (GR13), so BBBB follows
      *>     AAAA.
      *>   O3 one OPEN, INPUT SHARING WITH READ ONLY SQ and I-O
      *>     SHARING WITH NO OTHER IX: 00 00. SQ reads AAAA, BBBB, then
      *>     the at end status 10 (§2002:9.1.12.3); IX, open I-O,
      *>     accepts a READ and a REWRITE: 00.
      *>   O4 OPEN I-O SQ: 00 (GR14); READ AAAA, REWRITE CCCC: 00.
      *>   O5 OPEN INPUT SQ IX: CCCC BBBB and 1ONE, the rewritten
      *>     records.
      *> A compiler that refuses a form, opens in the wrong mode,
      *>   drops a group or file, or rewinds on EXTEND fails a line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2OPNFM.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SQ ASSIGN TO "g2opnfm.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS1.
           SELECT IX ASSIGN TO "g2opnfmx.dat"
               ORGANIZATION IS INDEXED ACCESS MODE IS DYNAMIC
               RECORD KEY IS IX-K
               FILE STATUS IS FS2.
       DATA DIVISION.
       FILE SECTION.
       FD SQ.
       01 SQ-R PIC X(4).
       FD IX.
       01 IX-R.
          05 IX-K PIC X.
          05 IX-D PIC X(3).
       WORKING-STORAGE SECTION.
       01 FS1 PIC XX.
       01 FS2 PIC XX.
       01 A1  PIC X(4).
       01 A2  PIC X(4).
       PROCEDURE DIVISION.
       M-0.
           OPEN OUTPUT SQ IX
           DISPLAY "O1 " FS1 " " FS2
           WRITE SQ-R FROM "AAAA"
           MOVE "1one" TO IX-R
           WRITE IX-R
           CLOSE SQ IX
           OPEN EXTEND SHARING WITH NO OTHER SQ
           DISPLAY "O2 " FS1
           WRITE SQ-R FROM "BBBB"
           CLOSE SQ
           OPEN INPUT SHARING WITH READ ONLY SQ
                I-O SHARING WITH NO OTHER IX
           DISPLAY "O3 " FS1 " " FS2
           READ SQ INTO A1
           READ SQ INTO A2
           READ SQ
           DISPLAY "O3 SQ " A1 " " A2 " " FS1
           MOVE "1" TO IX-K
           READ IX KEY IS IX-K
           MOVE "ONE" TO IX-D
           REWRITE IX-R
           DISPLAY "O3 IX " FS2
           CLOSE SQ IX
           OPEN I-O SQ
           DISPLAY "O4 " FS1
           READ SQ
           MOVE "CCCC" TO SQ-R
           REWRITE SQ-R
           DISPLAY "O4 REWRITE " FS1
           CLOSE SQ
           OPEN INPUT SQ IX
           READ SQ INTO A1
           READ SQ INTO A2
           MOVE "1" TO IX-K
           READ IX KEY IS IX-K
           DISPLAY "O5 " A1 " " A2 " " IX-R
           CLOSE SQ IX
           STOP RUN.
