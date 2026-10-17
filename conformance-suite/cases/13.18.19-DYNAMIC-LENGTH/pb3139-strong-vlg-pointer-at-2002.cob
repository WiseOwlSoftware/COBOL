      *>--- conformance-case
      *> id: pb3139-strong-vlg-pointer-at-2002
      *> rules: [FMT-13.18.19.2]
      *> source-format: fixed
      *>---
      *> kb/Work PB3139 - a strongly-typed variable-length group with a
      *> POINTER leaf crosses a program-prototype CALL as its storage
      *> area from COBOL 2014
      *> (cases/14.8.2.2-Group-items/pb3139_strong_vlg_pointer_call), but its
      *> DYNAMIC LENGTH member is itself a COBOL-2014 addition (ISO
      *> 8.5.1.10 / 13.18.19), so below 2014 the type is refused by the
      *> edition gate.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P3139NS IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 TG IS TYPEDEF STRONG.
          05 TA PIC X DYNAMIC LENGTH LIMIT IS 10.
          05 TP USAGE POINTER.
       01 LK-G TYPE TG.
       PROCEDURE DIVISION USING LK-G.
       END PROGRAM P3139NS.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P3139NM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM P3139NS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TG IS TYPEDEF STRONG.
          05 TA PIC X DYNAMIC LENGTH LIMIT IS 10.
          05 TP USAGE POINTER.
       01 G TYPE TG.
       PROCEDURE DIVISION.
           CALL P3139NS USING G
           STOP RUN.
       END PROGRAM P3139NM.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P3139NS.
       DATA DIVISION.
       LINKAGE SECTION.
       01 TG IS TYPEDEF STRONG.
          05 TA PIC X DYNAMIC LENGTH LIMIT IS 10.
          05 TP USAGE POINTER.
       01 LK-G TYPE TG.
       PROCEDURE DIVISION USING LK-G.
           GOBACK.
       END PROGRAM P3139NS.
