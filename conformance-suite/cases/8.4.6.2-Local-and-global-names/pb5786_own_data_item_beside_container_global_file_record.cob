      *>--- conformance-case
      *> id: pb5786_own_data_item_beside_container_global_file_record
      *> rules: [GR-8.4.6.2.1-1, GR-8.4.6.2.1-3]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5786 review R1-1, the legal reverse side of pb5786_own_data_item_hides_container_global_file_name - ISO
      *> 8.4.6.2.1 3) a) hides the WORD GX in PB5786H (its own data item is what GX names there), not the container's other global
      *> names: the record GXR of the GLOBAL file GX is still a global name, so PB5786H WRITEs it (the record identifies the
      *> container's file) and PB5786G, which owns the file, reads it back.
      *> EXPECTED OUTPUT, DERIVED: PB5786H moves its own GX (DATA) to GXR and writes it; PB5786G displays A GXR=DATA.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5786G.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT GX ASSIGN TO "PB5786G.DAT".
       DATA DIVISION.
       FILE SECTION.
       FD GX IS GLOBAL.
       01 GXR PIC X(4).
       PROCEDURE DIVISION.
       PA.
           OPEN OUTPUT GX.
           CALL "PB5786H".
           CLOSE GX.
           OPEN INPUT GX.
           READ GX AT END DISPLAY "EOF".
           CLOSE GX.
           DISPLAY "A GXR=" GXR.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5786H.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 GX PIC X(4) VALUE "DATA".
       PROCEDURE DIVISION.
       PB.
           MOVE GX TO GXR.
           WRITE GXR.
           EXIT PROGRAM.
       END PROGRAM PB5786H.
       END PROGRAM PB5786G.
