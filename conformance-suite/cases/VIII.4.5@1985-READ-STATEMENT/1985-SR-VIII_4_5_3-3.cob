      *>--- conformance-case
      *> id: 1985-SR-VIII_4_5_3-3
      *> rules: [1985-SR-VIII.4.5.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.5.3 SR3 (THE READ STATEMENT). A refusal: "The NEXT
      *> phrase must be specified for files in dynamic access mode when
      *> records are to be retrieved sequentially." REL-FILE is a relative
      *> file in dynamic access mode, and the READ on line 41 writes AT END
      *> (format 1, the sequential retrieval) without NEXT: WiseOwl refuses
      *> it at 85 with COBOL7144 there. A compiler that accepts it fails.
      *> (kb/Work PB5102)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SR1985VIII453N.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT REL-FILE ASSIGN TO "RELF"
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
       MAIN-PARA.
           OPEN OUTPUT REL-FILE.
           MOVE 1 TO REL-KEY.
           MOVE "AAAA" TO REL-REC.
           WRITE REL-REC INVALID KEY DISPLAY "W1 ERR".
           MOVE 2 TO REL-KEY.
           MOVE "BBBB" TO REL-REC.
           WRITE REL-REC INVALID KEY DISPLAY "W2 ERR".
           CLOSE REL-FILE.
           OPEN INPUT REL-FILE.
           READ REL-FILE RECORD AT END DISPLAY "EOF".
           DISPLAY REL-REC.
           CLOSE REL-FILE.
           STOP RUN.
