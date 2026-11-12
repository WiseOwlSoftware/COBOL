      *>--- conformance-case
      *> id: pb5071-fd-varying-without-record-85
      *> rules: [1985-FMT-IV.6.3.2.2.1]
      *> source-format: fixed
      *>---
      *> §1985:IV.6.3.2.2.1 prints the file section as
      *> "file-description-entry {record-description-entry} ...", and
      *> §1985:IV.2.1.4: "When braces, { } , enclose a portion of a
      *> general format, one of the options contained within the
      *> braces must be either explicitly specified or implicitly
      *> selected." The FD below is followed by no record description
      *> entry; its RECORD IS VARYING clause does not stand in for one
      *> at 85. A refusal at 85: a compiler that accepts it fails
      *> (kb/Work PB5071).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. N5071FM.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT VAR-FILE ASSIGN TO "N5071FM".
       DATA DIVISION.
       FILE SECTION.
       FD  VAR-FILE
           RECORD IS VARYING IN SIZE FROM 1 TO 10 CHARACTERS.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "DONE".
           STOP RUN.
