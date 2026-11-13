      *>--- conformance-case
      *> id: pb5198_rerun_records_device_85
      *> rules: [1985-GR-VII.2.12.4-1, 1985-GR-VII.2.12.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5198 - the integer-1 RECORDS form of the X3.23-1985
      *> RERUN clause (§1985:VII.2.12.4 GR2 d), the one form WiseOwl
      *> COBOL provides (GR2: "The implementor must provide at least
      *> one of the specified forms"): "the rerun information is
      *> written on the device specified by implementor-name-1 ...
      *> whenever integer-1 records of file-name-2 have been
      *> processed. File-name-2 may be either an input or output file
      *> with any organization or access." GR1 b: the information is
      *> "written as a separate file on a device specified by the
      *> implementor". The implementor-defined parts (docs/CONFORMANCE.md
      *> section 7): the device of implementor-name-1 is the host file
      *> the name spells as an ASSIGN target spells one, and each rerun
      *> point appends the line RERUN, file-name-2, and the count of
      *> records processed since the file's OPEN.
      *> The program creates both devices empty, then processes:
      *>   SEQF (EVERY 2): five WRITEs, then three READs of a second
      *>     OPEN - rerun points after WRITE 2 and 4, and after READ 2
      *>     (the count is per OPEN).
      *>   RELF (EVERY 1, RANDOM access): two WRITEs, a REWRITE and a
      *>     DELETE - a rerun point after each of the four statements.
      *> It then reads each device (a line feed ends each line, so a
      *> device record is 13 characters) and displays its lines.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5198A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SEQF ASSIGN TO "P5198S"
               ORGANIZATION IS SEQUENTIAL.
           SELECT RELF ASSIGN TO "P5198R"
               ORGANIZATION IS RELATIVE ACCESS MODE IS RANDOM
               RELATIVE KEY IS RK.
           SELECT DEVA ASSIGN TO "DEVA5198"
               ORGANIZATION IS SEQUENTIAL.
           SELECT DEVB ASSIGN TO "DEVB5198"
               ORGANIZATION IS SEQUENTIAL.
       I-O-CONTROL.
           RERUN ON DEVA5198 EVERY 2 RECORDS OF SEQF
           RERUN ON DEVB5198 EVERY 1 RECORDS OF RELF.
       DATA DIVISION.
       FILE SECTION.
       FD  SEQF.
       01  SEQ-REC PIC X(10).
       FD  RELF.
       01  REL-REC PIC X(10).
       FD  DEVA.
       01  DEVA-REC PIC X(13).
       FD  DEVB.
       01  DEVB-REC PIC X(13).
       WORKING-STORAGE SECTION.
       77  RK PIC 9(4) VALUE 1.
       77  N PIC 9 VALUE 0.
       77  EOF-FLAG PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT DEVA.
           CLOSE DEVA.
           OPEN OUTPUT DEVB.
           CLOSE DEVB.
           OPEN OUTPUT SEQF.
           PERFORM 5 TIMES
               ADD 1 TO N
               MOVE "RECORD-00" TO SEQ-REC
               MOVE N TO SEQ-REC(9:1)
               WRITE SEQ-REC
           END-PERFORM.
           CLOSE SEQF.
           OPEN INPUT SEQF.
           READ SEQF AT END DISPLAY "UNEXPECTED END".
           READ SEQF AT END DISPLAY "UNEXPECTED END".
           READ SEQF AT END DISPLAY "UNEXPECTED END".
           CLOSE SEQF.
           OPEN OUTPUT RELF.
           MOVE 1 TO RK.
           MOVE "FIRST" TO REL-REC.
           WRITE REL-REC INVALID KEY DISPLAY "INVALID KEY".
           MOVE 2 TO RK.
           MOVE "SECOND" TO REL-REC.
           WRITE REL-REC INVALID KEY DISPLAY "INVALID KEY".
           CLOSE RELF.
           OPEN I-O RELF.
           MOVE 1 TO RK.
           MOVE "CHANGED" TO REL-REC.
           REWRITE REL-REC INVALID KEY DISPLAY "INVALID KEY".
           MOVE 2 TO RK.
           DELETE RELF RECORD INVALID KEY DISPLAY "INVALID KEY".
           CLOSE RELF.
           DISPLAY "DEVICE A".
           OPEN INPUT DEVA.
           MOVE "N" TO EOF-FLAG.
           PERFORM UNTIL EOF-FLAG = "Y"
               READ DEVA AT END MOVE "Y" TO EOF-FLAG
                   NOT AT END DISPLAY DEVA-REC(1:12)
               END-READ
           END-PERFORM.
           CLOSE DEVA.
           DISPLAY "DEVICE B".
           OPEN INPUT DEVB.
           MOVE "N" TO EOF-FLAG.
           PERFORM UNTIL EOF-FLAG = "Y"
               READ DEVB AT END MOVE "Y" TO EOF-FLAG
                   NOT AT END DISPLAY DEVB-REC(1:12)
               END-READ
           END-PERFORM.
           CLOSE DEVB.
           STOP RUN.
