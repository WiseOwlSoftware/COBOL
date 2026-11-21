      *>--- conformance-case
      *> id: pb5525-relative-not-at-end-85
      *> rules: [1985-SR-VIII.4.5.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.5.3 SR3 (THE READ STATEMENT). A refusal: "The NEXT
      *> phrase must be specified for files in dynamic access mode when
      *> records are to be retrieved sequentially." The 1985 READ prints
      *> NOT AT END, like AT END, in format 1 only (the sequential
      *> retrieval; format 2 carries INVALID KEY), so the READ of REL-FILE
      *> on line 50 writes NOT AT END alone for a dynamic-access file and
      *> omits NEXT: WiseOwl refuses it at 85 with COBOL7144 there. Its AT
      *> END twin is 1985-SR-VIII_4_5_3-3; a compiler that tested only AT
      *> END (or read the later editions' implied NEXT, §2023:14.9.30.3
      *> SR9) accepts this one and fails. The USE procedure keeps the READ
      *> clear of the rule that it write AT END or INVALID KEY without one,
      *> so COBOL7144 is its one refusal. (kb/Work PB5525)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5525RELNAE.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT REL-FILE ASSIGN TO "PB5525RN"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS REL-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD  REL-FILE
           LABEL RECORDS ARE STANDARD.
       01  REL-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01  REL-KEY PIC 9(4).
       PROCEDURE DIVISION.
       DECLARATIVES.
       FILE-ERR SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON REL-FILE.
       FILE-ERR-PARA.
           DISPLAY "FILE ERROR".
       END DECLARATIVES.
       MAIN-SECT SECTION.
       MAIN-PARA.
           OPEN OUTPUT REL-FILE.
           MOVE 1 TO REL-KEY.
           MOVE "AAAA" TO REL-REC.
           WRITE REL-REC INVALID KEY DISPLAY "W1 ERR".
           CLOSE REL-FILE.
           OPEN INPUT REL-FILE.
           READ REL-FILE RECORD
               NOT AT END DISPLAY REL-REC.
           CLOSE REL-FILE.
           STOP RUN.
