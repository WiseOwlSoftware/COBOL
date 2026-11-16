      *>--- conformance-case
      *> id: pb5815_print_file_not_read_back
      *> rules: [GR-9.1.13.6-6, GR-14.9.27.4-16, GR-14.9.27.4-4,
      *>     GR-12.4.5.5.3-2, GR-12.4.5.10.3-3, GR-14.9.51.4-17,
      *>     GR-14.9.51.4-18]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5815 - the 2023 twin of
      *> VII.4.3@1985-OPEN-STATEMENT/pb5815_print_file_not_read_back_85:
      *> a record sequential PRINT file is not read back, at every
      *> edition. A file written with the ADVANCING phrase (and every
      *> plain WRITE of it) or described with a LINAGE clause carries
      *> its print controls between its records, so a READ cannot
      *> return the records in the predecessor-successor order that
      *> "is established by the order of execution of WRITE
      *> statements" (§2023:14.9.51.4 GR17). The determination
      *> (docs/CONFORMANCE.md DOC-A.1-146 (c')) is that such a file
      *> does not support read operations, so OPEN INPUT is status 37
      *> (§2023:9.1.13.6 GR6a.3: "the INPUT phrase is specified but
      *> the file will not support read operations") and so is OPEN
      *> I-O (§2023:9.1.13.6 GR6a.2: "the I-O phrase is specified but
      *> the file will not support the input and output operations");
      *> a READ or REWRITE of the file not open is 47 or 49.
      *> Why each leg can fail:
      *>   PA  AFTER ADVANCING 2, then a plain WRITE: OPEN INPUT and
      *>       OPEN I-O are 37 where a fixed-block READ would return
      *>       00 with print controls in the record (the defect);
      *>       READ is 47. OPEN EXTEND still writes: 00.
      *>   PL  LINAGE IS 5 LINES, two plain WRITEs: OPEN I-O is 37
      *>       where it was 00 and REWRITE overwrote a line end;
      *>       REWRITE is then 49; OPEN INPUT is 37.
      *>   PD  the only ADVANCING WRITE never executes: the file is a
      *>       print file by its description, so OPEN INPUT is 37
      *>       where three wrong records were read.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5815PRT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PA ASSIGN TO "pb5815a.dat" FILE STATUS IS SA.
           SELECT PL ASSIGN TO "pb5815l.dat" FILE STATUS IS SL.
           SELECT PD ASSIGN TO "pb5815d.dat" FILE STATUS IS SX.
       DATA DIVISION.
       FILE SECTION.
       FD  PA.
       01  RA PIC X(3).
       FD  PL LINAGE IS 5 LINES.
       01  RL PIC X(6).
       FD  PD.
       01  RX PIC X(4).
       WORKING-STORAGE SECTION.
       01  SA PIC XX.
       01  SL PIC XX.
       01  SX PIC XX.
       01  SW PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PA.
           MOVE "A01" TO RA. WRITE RA AFTER ADVANCING 2 LINES.
           MOVE "A02" TO RA. WRITE RA.
           DISPLAY "PA WRITE " SA.
           CLOSE PA.
           OPEN INPUT PA.
           DISPLAY "PA OPEN INPUT " SA.
           READ PA AT END DISPLAY "PA AT END".
           DISPLAY "PA READ " SA.
           OPEN I-O PA.
           DISPLAY "PA OPEN I-O " SA.
           OPEN EXTEND PA.
           DISPLAY "PA OPEN EXTEND " SA.
           MOVE "A03" TO RA. WRITE RA.
           DISPLAY "PA WRITE " SA.
           CLOSE PA.
           DISPLAY "PA CLOSE " SA.
           OPEN OUTPUT PL.
           MOVE "LIN001" TO RL. WRITE RL.
           MOVE "LIN002" TO RL. WRITE RL.
           CLOSE PL.
           OPEN I-O PL.
           DISPLAY "PL OPEN I-O " SL.
           MOVE "REW001" TO RL. REWRITE RL.
           DISPLAY "PL REWRITE " SL.
           OPEN INPUT PL.
           DISPLAY "PL OPEN INPUT " SL.
           OPEN OUTPUT PD.
           MOVE "WXYZ" TO RX. WRITE RX.
           IF SW = 1
               WRITE RX AFTER ADVANCING 1 LINE.
           CLOSE PD.
           OPEN INPUT PD.
           DISPLAY "PD OPEN INPUT " SX.
           STOP RUN.
