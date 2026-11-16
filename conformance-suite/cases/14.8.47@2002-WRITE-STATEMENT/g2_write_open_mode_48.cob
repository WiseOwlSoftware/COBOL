      *>--- conformance-case
      *> id: g2_write_open_mode_48
      *> rules: [2002-GR-14.8.47.3-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.47.3 GR1 - "If the access mode of the write file is
      *> sequential, the open mode of the write file connector shall be
      *> extend or output. Otherwise, the open mode of the write file
      *> connector shall be I-O or output. If the open mode is not as
      *> described, the execution of the WRITE statement is unsuccessful
      *> and the I-O status for the write file connector is set to '48'."
      *> Sequential access (a sequential file, a relative file): OUTPUT
      *> and EXTEND give 00, INPUT and I-O give 48. Random or dynamic
      *> access (relative dynamic, indexed random): OUTPUT and I-O give
      *> 00, INPUT gives 48. Each line fails if the open-mode check uses
      *> the wrong table for the access mode (e.g. I-O admitted on a
      *> sequential-access relative file) or a wrong status.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2WR02.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "g2wr02s.tmp" ORGANIZATION SEQUENTIAL
               FILE STATUS IS FS.
           SELECT RSF ASSIGN TO "g2wr02r.tmp" ORGANIZATION RELATIVE
               ACCESS MODE IS SEQUENTIAL FILE STATUS IS FS.
           SELECT RDF ASSIGN TO "g2wr02d.tmp" ORGANIZATION RELATIVE
               ACCESS MODE IS DYNAMIC RELATIVE KEY IS RK
               FILE STATUS IS FS.
           SELECT IXF ASSIGN TO "g2wr02i.tmp" ORGANIZATION INDEXED
               ACCESS MODE IS RANDOM RECORD KEY IS IK
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD SF.
       01 SR PIC X(4).
       FD RSF.
       01 RSR PIC X(4).
       FD RDF.
       01 RDR PIC X(4).
       FD IXF.
       01 IXR.
          05 IK PIC X(4).
       WORKING-STORAGE SECTION.
       01 FS PIC XX.
       01 RK PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           MOVE "AAAA" TO SR
           OPEN OUTPUT SF WRITE SR DISPLAY "SEQ-OUTPUT " FS CLOSE SF
           OPEN INPUT SF WRITE SR DISPLAY "SEQ-INPUT " FS CLOSE SF
           OPEN EXTEND SF WRITE SR DISPLAY "SEQ-EXTEND " FS CLOSE SF
           OPEN I-O SF WRITE SR DISPLAY "SEQ-I-O " FS CLOSE SF
           MOVE "BBBB" TO RSR
           OPEN OUTPUT RSF WRITE RSR DISPLAY "RELSEQ-OUTPUT " FS
           CLOSE RSF
           OPEN I-O RSF WRITE RSR DISPLAY "RELSEQ-I-O " FS CLOSE RSF
           MOVE "CCCC" TO RDR
           OPEN OUTPUT RDF MOVE 1 TO RK WRITE RDR
           DISPLAY "RELDYN-OUTPUT " FS CLOSE RDF
           OPEN I-O RDF MOVE 2 TO RK WRITE RDR
           DISPLAY "RELDYN-I-O " FS CLOSE RDF
           OPEN INPUT RDF MOVE 3 TO RK WRITE RDR
           DISPLAY "RELDYN-INPUT " FS CLOSE RDF
           OPEN OUTPUT IXF MOVE "K001" TO IK WRITE IXR
           DISPLAY "IDXRND-OUTPUT " FS CLOSE IXF
           OPEN I-O IXF MOVE "K002" TO IK WRITE IXR
           DISPLAY "IDXRND-I-O " FS CLOSE IXF
           OPEN INPUT IXF MOVE "K003" TO IK WRITE IXR
           DISPLAY "IDXRND-INPUT " FS CLOSE IXF
           STOP RUN.
