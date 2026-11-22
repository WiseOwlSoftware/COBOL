      *>--- conformance-case
      *> id: pb3212-external-fd-item-padding-refused
      *> rules: [1985-SR-VII.2.7.3-3, 2002-SR-12.3.4.10.2-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3212 review R1-1 (d). PB3212 made an item of an
      *> EXTERNAL file's record an external data item (ISO 2023
      *> 13.18.22.4 GR4 b), which 1985:VII.2.7.4 GR6 asks of a PADDING
      *> CHARACTER data-name-1 of an external file.  Being external does
      *> not lift 1985:VII.2.7.3 SR3: "Data-name-1 ... must not be defined
      *> in the Communication Section, the File Section, or the Report
      *> Section" (2002:12.3.4.10.2 SR3).  PADC is in PAD-FILE's record,
      *> so both programs are refused COBOL6848.  The clause is deleted
      *> from 2014.  The legal external form (an EXTERNAL working-storage
      *> item) is conformance:pb5069_external_padding_agrees.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3212F.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PAD-FILE ASSIGN TO "PB3212PF.DAT".
           SELECT EXT-FILE ASSIGN TO "PB3212PX.DAT"
               PADDING CHARACTER IS PADC.
       DATA DIVISION.
       FILE SECTION.
       FD  PAD-FILE IS EXTERNAL.
       01  PAD-REC.
           05 PADC PIC X.
           05 FILLER PIC X(9).
       FD  EXT-FILE IS EXTERNAL.
       01  EXT-REC PIC X(10).
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "A".
           CALL "PB3212G".
           STOP RUN.
       END PROGRAM PB3212F.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3212G.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PAD-FILE ASSIGN TO "PB3212PF.DAT".
           SELECT EXT-FILE ASSIGN TO "PB3212PX.DAT"
               PADDING CHARACTER IS PADC.
       DATA DIVISION.
       FILE SECTION.
       FD  PAD-FILE IS EXTERNAL.
       01  PAD-REC.
           05 PADC PIC X.
           05 FILLER PIC X(9).
       FD  EXT-FILE IS EXTERNAL.
       01  EXT-REC PIC X(10).
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "B".
           EXIT PROGRAM.
       END PROGRAM PB3212G.
