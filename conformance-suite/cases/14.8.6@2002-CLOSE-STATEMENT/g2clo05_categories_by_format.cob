      *>--- conformance-case
      *> id: g2clo05_categories_by_format
      *> rules: [2002-GR-14.8.6.3-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.6.3 GR2 -- "all files are divided into the
      *> following categories, where the term 'file' means the
      *> physical file": (a) Non-unit ... (d) "A file with organization
      *> other than sequential, that resides on a mass storage device".
      *> GR3's table 15 gives each 2002 CLOSE format its symbols per
      *> category, including the 2002-only CLOSE WITH LOCK row. The
      *> implementor's placement (DOC-A.1-24): a sequential host file is
      *> (a) Non-unit; a relative or indexed file is (d) by its
      *> organization. Each leg reads one table 15 cell.
      *> (a) CLOSE WITH NO REWIND = c,h: the file IS closed and the
      *>   status is §2002:9.1.12.1 item 5's 07 ("references a physical
      *>   file on a non-reel/unit medium"): A-NOREWIND=07, and the
      *>   re-OPEN succeeds, A-REOPEN=00 (41 if it were still open).
      *> (a) CLOSE UNIT = f, non-unit media: "The file remains in the
      *>   open mode, the file position indicator is unchanged, and no
      *>   action takes place except as specified in general rule 4":
      *>   A-UNIT=07 and the next READ finds no next record,
      *>   A-READ2=10 (00 AAAA if it rewound, 47 if it closed).
      *>   CLOSE REEL FOR REMOVAL (REEL = UNIT, SR2) is the same f:
      *>   A-REMOVAL=07, still open.
      *> (a) CLOSE WITH LOCK = c,e: closed (00) and symbol e "shall not
      *>   be opened again during the execution of this run unit": the
      *>   re-OPEN is 38 (§2002:9.1.12.5 item 6; §2002:14.8.26.3 GR24).
      *> (d) CLOSE = c: 00, re-OPEN 00; CLOSE WITH LOCK = c,e: 00 and
      *>   re-OPEN 38, for an indexed and for a relative file.
      *> §2002:9.1.12: 38 is fatal and the implementor decides whether
      *> execution continues; DOC-A.1-103: it continues, because each
      *> file has a FILE STATUS clause and EC-I-O checking is not
      *> enabled.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CL5C.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SQ ASSIGN TO "g2c5s.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS SQ-ST.
           SELECT IX ASSIGN TO "g2c5i.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS IX-KEY
               FILE STATUS IS IX-ST.
           SELECT RL ASSIGN TO "g2c5r.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS RL-ST.
       DATA DIVISION.
       FILE SECTION.
       FD SQ.
       01 SQ-REC PIC X(4).
       FD IX.
       01 IX-REC.
          05 IX-KEY PIC X(4).
       FD RL.
       01 RL-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 SQ-ST PIC XX.
       01 IX-ST PIC XX.
       01 RL-ST PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT SQ
           MOVE "AAAA" TO SQ-REC
           WRITE SQ-REC
           CLOSE SQ WITH NO REWIND
           DISPLAY "A-NOREWIND=" SQ-ST
           OPEN INPUT SQ
           DISPLAY "A-REOPEN=" SQ-ST
           READ SQ AT END CONTINUE END-READ
           DISPLAY "A-READ1=" SQ-ST " " SQ-REC
           CLOSE SQ UNIT
           DISPLAY "A-UNIT=" SQ-ST
           READ SQ AT END CONTINUE END-READ
           DISPLAY "A-READ2=" SQ-ST
           CLOSE SQ REEL FOR REMOVAL
           DISPLAY "A-REMOVAL=" SQ-ST
           CLOSE SQ WITH LOCK
           DISPLAY "A-LOCK=" SQ-ST
           OPEN INPUT SQ
           DISPLAY "A-RELOCKED=" SQ-ST
           OPEN OUTPUT IX
           MOVE "KKKK" TO IX-KEY
           WRITE IX-REC
           CLOSE IX
           DISPLAY "D-IX-CLOSE=" IX-ST
           OPEN INPUT IX
           DISPLAY "D-IX-REOPEN=" IX-ST
           CLOSE IX WITH LOCK
           DISPLAY "D-IX-LOCK=" IX-ST
           OPEN INPUT IX
           DISPLAY "D-IX-RELOCKED=" IX-ST
           OPEN OUTPUT RL
           MOVE "RRRR" TO RL-REC
           WRITE RL-REC
           CLOSE RL WITH LOCK
           DISPLAY "D-RL-LOCK=" RL-ST
           OPEN I-O RL
           DISPLAY "D-RL-RELOCKED=" RL-ST
           STOP RUN.
