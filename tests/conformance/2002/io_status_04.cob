      *> I-O status '04' on a record-sequential READ (ISO §9.1.13.2 item 3 / §14.9.35
      *> GR14; clarified COBOL-2023 Annex E.2 item 15, version-invariant behavior). A
      *> READ whose physical record length is outside the file's min/max record size
      *> is SUCCESSFUL but sets status '04' (the record is still delivered). Here three
      *> 5-character records are written (a 15-byte physical file), then read back
      *> through a 10-character record description: the first read gets a full 10-byte
      *> record ('00'), the second gets the trailing 5-byte partial record ('04', the
      *> short record right-padded), the third hits end-of-file.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. IOSTAT-04.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F-OUT ASSIGN TO "iostat04.txt"
               ORGANIZATION IS SEQUENTIAL.
           SELECT F-IN ASSIGN TO "iostat04.txt"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS WS-FS.
       DATA DIVISION.
       FILE SECTION.
       FD F-OUT.
       01 R5 PIC X(5).
       FD F-IN.
       01 R10 PIC X(10).
       WORKING-STORAGE SECTION.
       01 WS-FS PIC XX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT F-OUT.
           WRITE R5 FROM "AAAAA".
           WRITE R5 FROM "BBBBB".
           WRITE R5 FROM "CCCCC".
           CLOSE F-OUT.
           OPEN INPUT F-IN.
           READ F-IN.
           DISPLAY "S1=[" WS-FS "] R=[" R10 "]".
           READ F-IN.
           DISPLAY "S2=[" WS-FS "] R=[" R10 "]".
           READ F-IN AT END DISPLAY "S3=EOF".
           CLOSE F-IN.
           STOP RUN.
