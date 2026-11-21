      *>--- conformance-case
      *> id: pb5525-indexed-not-at-end-85
      *> rules: [1985-SR-IX.4.5.3-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.4.5.3 SR5 (THE READ STATEMENT). A refusal: "The NEXT
      *> phrase must be specified for files in dynamic access mode, when
      *> records are to be retrieved sequentially." The 1985 READ prints
      *> NOT AT END, like AT END, in format 1 only (the sequential
      *> retrieval; format 2 carries INVALID KEY), so the READ of IFILE
      *> on line 50 writes NOT AT END alone for a dynamic-access file and
      *> omits NEXT: WiseOwl refuses it at 85 with COBOL7144 there. Its AT
      *> END twin is 1985-SR-IX_4_5_3-5; a compiler that tested only AT
      *> END (or read the later editions' implied NEXT, §2023:14.9.30.3
      *> SR9) accepts this one and fails. The USE procedure keeps the READ
      *> clear of the rule that it write AT END or INVALID KEY without one,
      *> so COBOL7144 is its one refusal. (kb/Work PB5525)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5525IDXNAE.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IFILE ASSIGN TO "PB5525IN"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS IKEY.
       DATA DIVISION.
       FILE SECTION.
       FD  IFILE
           LABEL RECORDS ARE STANDARD.
       01  IREC.
           05  IKEY  PIC X(3).
           05  IDATA PIC X(5).
       PROCEDURE DIVISION.
       DECLARATIVES.
       FILE-ERR SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON IFILE.
       FILE-ERR-PARA.
           DISPLAY "FILE ERROR".
       END DECLARATIVES.
       MAIN-SECT SECTION.
       MAIN-PARA.
           OPEN OUTPUT IFILE.
           MOVE "001" TO IKEY.
           MOVE "ALPHA" TO IDATA.
           WRITE IREC INVALID KEY DISPLAY "BADW1".
           CLOSE IFILE.
           OPEN INPUT IFILE.
           READ IFILE RECORD
               NOT AT END DISPLAY IDATA.
           CLOSE IFILE.
           STOP RUN.
