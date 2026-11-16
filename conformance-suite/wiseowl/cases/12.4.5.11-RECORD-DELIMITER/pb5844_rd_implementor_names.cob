      *>--- conformance-case
      *> id: pb5844_rd_implementor_names
      *> rules: [FMT-12.4.5.11.2, SR-12.4.5.11.3-2, GR-12.4.5.11.4-4,
      *>    GR-12.4.5.11.4-6, DOC-A.1-150]
      *> accept: [85, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5844 - THE IMPLEMENTOR-NAMES OF THE RECORD DELIMITER
      *> CLAUSE, each selecting its method (docs/CONFORMANCE.md section
      *> 7, Annex A.1 item 150). 1985 makes the method "that associated
      *> with implementor-name-1 by the implementor" (§1985:VII.2.8.4
      *> GR3); 2023 says "The implementor shall specify the names
      *> available for use as feature-name-1" (§2023:12.4.5.11.3 SR2)
      *> and reads the method off the name the same way
      *> (§2023:12.4.5.11.4 GR4). WiseOwl COBOL's names are GnuCOBOL's:
      *> BINARY-SEQUENTIAL, the record sequential file's 4-byte length
      *> prefix, and LINE-SEQUENTIAL, the line sequential file's line
      *> delimiter (Annex A.1 item 151, arms (a) and (b)).
      *> WHAT EACH LINE PROVES. Each file is written through one entry
      *> and read back through a second entry for the same physical
      *> file whose RECORD IS VARYING ... DEPENDING ON phrase reports
      *> the length of the record read ("the contents of the data item
      *> referenced by data-name-1 will indicate the number of character
      *> positions in the record just read", §1985:VII.3.8.4 GR11).
      *> B: the writer's RECORD CONTAINS 2 TO 6 clause gives
      *> variable-length records (Annex A.1 item 148) written at the
      *> record description's size, 6, so the length prefix carries 6
      *> and each record reads back as 6 characters, its trailing
      *> spaces included: LEN=06 [AB    ] and LEN=06 [C D   ].
      *> L: LINE-SEQUENTIAL makes the file line sequential, whose record
      *> length is "the number of characters between the preceding line
      *> delimiter and the following line delimiter" (§2023:9.1.7.2),
      *> and a record of a line sequential file without a DEPENDING
      *> phrase is written without the spaces to the right of its
      *> rightmost non-space character (Annex A.1 item 151 (b)), so
      *> the same two writes read back as LEN=02 [AB] and LEN=03 [C D].
      *> The B and L lines differ only because the names select two
      *> methods; a name that selected nothing would print B's lengths
      *> on both. The LINE-SEQUENTIAL writer's ORGANIZATION clause
      *> FOLLOWS its RECORD DELIMITER clause, and the reader writes the
      *> clause without the optional word IS, so neither the order of
      *> the clauses nor the optional word decides the method.
      *> NEITHER NAME IS A RESERVED WORD. A system-name and a user-defined
      *> word may be the same word (§1985:IV.4.2.2.1, §2023:8.3.2.1 SR4),
      *> so the data item BINARY-SEQUENTIAL and the paragraph
      *> LINE-SEQUENTIAL compile, and W=WORD shows the data item.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5844IN.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT BW ASSIGN TO "p5844b.dat"
               ORGANIZATION IS SEQUENTIAL
               RECORD DELIMITER IS BINARY-SEQUENTIAL.
           SELECT BR ASSIGN TO "p5844b.dat"
               RECORD DELIMITER IS BINARY-SEQUENTIAL.
           SELECT LW ASSIGN TO "p5844l.dat"
               RECORD DELIMITER IS LINE-SEQUENTIAL
               ORGANIZATION IS SEQUENTIAL.
           SELECT LR ASSIGN TO "p5844l.dat"
               RECORD DELIMITER LINE-SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD BW
           RECORD CONTAINS 2 TO 6 CHARACTERS.
       01 BW-REC PIC X(6).
       FD BR
           RECORD IS VARYING IN SIZE FROM 1 TO 6 CHARACTERS
               DEPENDING ON WS-BLEN.
       01 BR-REC PIC X(6).
       FD LW.
       01 LW-REC PIC X(6).
       FD LR
           RECORD IS VARYING IN SIZE FROM 1 TO 6 CHARACTERS
               DEPENDING ON WS-LLEN.
       01 LR-REC PIC X(6).
       WORKING-STORAGE SECTION.
       01 WS-BLEN PIC 99.
       01 WS-LLEN PIC 99.
       01 BINARY-SEQUENTIAL PIC X(4) VALUE "WORD".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT BW
           MOVE "AB" TO BW-REC
           WRITE BW-REC
           MOVE "C D" TO BW-REC
           WRITE BW-REC
           CLOSE BW
           OPEN OUTPUT LW
           MOVE "AB" TO LW-REC
           WRITE LW-REC
           MOVE "C D" TO LW-REC
           WRITE LW-REC
           CLOSE LW
           OPEN INPUT BR
           PERFORM 2 TIMES
               MOVE 99 TO WS-BLEN
               READ BR AT END DISPLAY "B EOF" END-READ
               DISPLAY "B LEN=" WS-BLEN " [" BR-REC(1:WS-BLEN) "]"
           END-PERFORM
           CLOSE BR
           OPEN INPUT LR
           PERFORM 2 TIMES
               MOVE 99 TO WS-LLEN
               READ LR AT END DISPLAY "L EOF" END-READ
               DISPLAY "L LEN=" WS-LLEN " [" LR-REC(1:WS-LLEN) "]"
           END-PERFORM
           CLOSE LR
           PERFORM LINE-SEQUENTIAL
           STOP RUN.
       LINE-SEQUENTIAL.
           DISPLAY "W=" BINARY-SEQUENTIAL.
