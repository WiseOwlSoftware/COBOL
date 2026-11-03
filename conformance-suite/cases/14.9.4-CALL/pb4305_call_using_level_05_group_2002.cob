      *>--- conformance-case
      *> id: pb4305_call_using_level_05_group_2002
      *> rules: [SR-14.9.4.3-3]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4305 - A CALL argument may be a group item at any
      *>   level (14.9.4.3 SR3 names "a data item defined in the ...
      *>   working-storage ... section"; 2002 Annex F.2 item 10:
      *>   "Formerly, they had to be elementary or have a level number
      *>   of 1 or 77"). H is a level-05 group passed BY REFERENCE to a
      *>   contained program, which shows it and changes its first
      *>   half. Expected: "CDAB" (from the callee, L2 then L1) and
      *>   "ZZCD" (H after the call). It fails if the level-05 group is
      *>   refused or passed by content.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4305CL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
           05 H.
               10 H1 PIC X(2) VALUE "AB".
               10 H2 PIC X(2) VALUE "CD".
       PROCEDURE DIVISION.
       MAIN-P.
           CALL "PB4305CS" USING H
           DISPLAY H
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4305CS.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L.
           05 L1 PIC X(2).
           05 L2 PIC X(2).
       PROCEDURE DIVISION USING L.
       MAIN-S.
           DISPLAY L2 L1
           MOVE "ZZ" TO L1
           GOBACK.
       END PROGRAM PB4305CS.
       END PROGRAM PB4305CL.
