      *>--- conformance-case
      *> id: pb4305_global_in_linkage_2002
      *> rules: [SR-13.18.27.3-1]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4305 - 13.18.27.3 SR1 b): GLOBAL may be written on a
      *>   level-1 entry "specified in the file, working-storage,
      *>   local-storage, or linkage section" (2002 Annex F.2 item 61).
      *>   L is a GLOBAL linkage record of a called program, read by
      *>   the program it contains. Expected: "ABC". It fails if GLOBAL
      *>   is refused in the linkage section.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4305GL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC X(3) VALUE "ABC".
       PROCEDURE DIVISION.
       MAIN-P.
           CALL "PB4305GS" USING W
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4305GS.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC X(3) GLOBAL.
       PROCEDURE DIVISION USING L.
       MAIN-S.
           CALL "PB4305GT"
           GOBACK.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4305GT.
       PROCEDURE DIVISION.
       MAIN-T.
           DISPLAY L
           GOBACK.
       END PROGRAM PB4305GT.
       END PROGRAM PB4305GS.
       END PROGRAM PB4305GL.
