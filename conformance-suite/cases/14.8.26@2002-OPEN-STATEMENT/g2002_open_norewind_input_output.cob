      *>--- conformance-case
      *> id: g2002_open_norewind_input_output
      *> rules: [2002-SR-14.8.26.2-4, 2002-SR-14.8.26.2-5]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.26.2 SR4 - "The NO REWIND phrase may be specified
      *> only for sequential files." and §2002:14.8.26.2 SR5 - "The NO
      *> REWIND phrase may be specified only when the INPUT or OUTPUT
      *> phrase is specified." Both GRANT a form: NO REWIND on a
      *> sequential file opened OUTPUT and opened INPUT. This program
      *> writes both granted forms; that it compiles and runs at all is
      *> the first assertion (a compiler that refused NO REWIND outright
      *> fails here).
      *> The statuses are derived from §2002:14.8.26.3 GR9 ("If the NO
      *> REWIND phrase is ignored, the OPEN statement is successful and
      *> the I-O status associated with file-name-1 is set to '07'") and
      *> §2002:9.1.12.1 item 5 ('07' when "an OPEN statement with the NO
      *> REWIND phrase references a physical file on a non-reel/unit
      *> medium"): the file is a disk file, so OUT-NR and IN-NR are 07.
      *> The read-back shows the opens took effect: §2002:14.8.26.3 GR16
      *> (OUTPUT creates the file), GR12 (INPUT sets the file position
      *> indicator to 1), so the two records read back in order, and the
      *> third READ is '10' (§2002:9.1.12.3 item 1 a). A compiler that
      *> accepted the phrase but dropped the OPEN, or opened in another
      *> mode, fails the W/R lines.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2OPN01.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT S ASSIGN TO "g2opn01.dat"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS ST.
       DATA DIVISION.
       FILE SECTION.
       FD S.
       01 S-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 ST PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT S WITH NO REWIND
           DISPLAY "OUT-NR=" ST
           MOVE "AAAA" TO S-REC
           WRITE S-REC
           DISPLAY "W1=" ST
           MOVE "BBBB" TO S-REC
           WRITE S-REC
           DISPLAY "W2=" ST
           CLOSE S
           DISPLAY "CLOSE1=" ST
           OPEN INPUT S WITH NO REWIND
           DISPLAY "IN-NR=" ST
           READ S AT END DISPLAY "R1 AT END" END-READ
           DISPLAY "R1=" ST " " S-REC
           READ S AT END DISPLAY "R2 AT END" END-READ
           DISPLAY "R2=" ST " " S-REC
           READ S AT END DISPLAY "R3 AT END" END-READ
           DISPLAY "R3=" ST
           CLOSE S
           DISPLAY "CLOSE2=" ST
           STOP RUN.
