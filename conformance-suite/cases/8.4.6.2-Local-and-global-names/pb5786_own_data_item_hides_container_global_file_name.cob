*>--- conformance-case
*> id: pb5786_own_data_item_hides_container_global_file_name
*> rules: [GR-8.4.6.2.1-1, GR-8.4.6.2.1-3]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB5786 - ISO 8.4.6.2.1 3) a), the reverse arm: PB5786C declares the file GX IS GLOBAL; PB5786D, contained in it,
*> declares its own data item GX and writes OPEN OUTPUT GX. GX there is B's data-name, which is no file-name (COBOL1639).
*> Fails if the OPEN is accepted and opens the container's file.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5786C.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT GX ASSIGN TO "PB5786C.DAT".
       DATA DIVISION.
       FILE SECTION.
       FD GX IS GLOBAL.
       01 GXR PIC X(4).
       PROCEDURE DIVISION.
       PA.
           CALL "PB5786D".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5786D.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 GX PIC X(4) VALUE "DATA".
       PROCEDURE DIVISION.
       PB.
           OPEN OUTPUT GX.
           EXIT PROGRAM.
       END PROGRAM PB5786D.
       END PROGRAM PB5786C.
