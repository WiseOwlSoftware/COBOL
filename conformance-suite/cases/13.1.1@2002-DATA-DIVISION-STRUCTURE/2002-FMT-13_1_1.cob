      *>--- conformance-case
      *> id: 2002-FMT-13_1_1
      *> rules: [2002-FMT-13.1.1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.1.1 - the data division general format: FILE,
      *> WORKING-STORAGE, LOCAL-STORAGE, LINKAGE, COMMUNICATION, REPORT
      *> sections in that order, each optional, and the 2002 form
      *> "COMMUNICATION SECTION. │ communication-description-entry"
      *> followed by { constant-entry | record-description-entry } ...
      *> The SCREEN SECTION is left out: it is processor-dependent
      *> (§2002:B.3 item 1, §2002:3.1.5).
      *> Legs, each a form the format grants and a compiler can refuse:
      *>   FILE: an FD followed by a constant-entry then a record
      *>     (FCON); an FD of a report file (REPORT IS).
      *>   WORKING-STORAGE: a 77-level entry (W-N) and a record (W-X).
      *>   LOCAL-STORAGE and LINKAGE records.
      *>   COMMUNICATION: an input CD followed by a constant-entry and
      *>     no record (KCD; its clauses name the interface area, so
      *>     §2002:A.1.2.2 SR4 needs no 01), then an output CD followed
      *>     by a record (OREC).
      *>   REPORT: an RD followed by a constant-entry (KRC) and a
      *>     report group.
      *> Expected: §2002:13.9.3 GR1 "the effect of specifying
      *> constant-name-1 in other than this entry is as if literal-1"
      *> were written, so the first line is the four constants' and
      *> the VALUE items' literals; the second is O-DC after MOVE 3,
      *> PIC 9(4), 0003. A refusal of any section, of its place, or of
      *> a constant after the CD fails the case at compile time.
      *> The communication facility is obsolete at 2002 (§2002:G):
      *> the COBOL0903 warning does not fail it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DDS1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT DATF ASSIGN TO "g2dds1.dat".
           SELECT RPTF ASSIGN TO "g2dds1.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD DATF.
       01 FCON CONSTANT AS "FSOK".
       01 DREC PIC X(4).
       FD RPTF REPORT IS RP.
       WORKING-STORAGE SECTION.
       77 W-N PIC 9 VALUE 5.
       01 W-X PIC X(4) VALUE "WSOK".
       LOCAL-STORAGE SECTION.
       01 L-X PIC X(4) VALUE "LSOK".
       LINKAGE SECTION.
       01 K-X PIC X(4).
       COMMUNICATION SECTION.
       CD CIN FOR INPUT
           SYMBOLIC QUEUE IS CQ
           STATUS KEY IS CST
           MESSAGE COUNT IS CCNT.
       01 KCD CONSTANT AS "CDOK".
       CD COUT FOR OUTPUT.
       01 OREC.
          02 O-DC PIC 9(4).
          02 O-REST PIC X(19).
       REPORT SECTION.
       RD RP PAGE LIMIT 10.
       01 KRC CONSTANT AS "RDOK".
       01 TYPE DETAIL LINE 1.
          05 COLUMN 1 PIC X(2) VALUE "AB".
       PROCEDURE DIVISION.
       P1.
           DISPLAY FCON " " W-X " " W-N " " L-X " " KCD " " KRC.
           MOVE 3 TO O-DC.
           DISPLAY O-DC.
           STOP RUN.
