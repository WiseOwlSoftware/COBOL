      *>--- conformance-case
      *> id: g2fst01_status_item_sections
      *> rules: [2002-SR-12.3.4.7.2-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:12.3.4.7.2 SR2 - "Data-name-1 shall be a two-character
      *> data item of the category alphanumeric, defined in the
      *> working-storage, local-storage, or linkage section."
      *>   cite.py --check "§2002:12.3.4.7.2 SR2" -> OK
      *> One FILE STATUS item from each admitted section: WFS
      *> (working-storage), LFS (local-storage), KFS (linkage, the
      *> caller's WFS2 passed BY REFERENCE). Each file is absent and
      *> not OPTIONAL, so OPEN INPUT gives I-O status 35
      *> (§2002:9.1.12.5 item 4, cite.py OK) and §2002:12.3.4.7.3 GR1
      *> stores it in the entry's item.
      *> DERIVATION: WS=35 / LOCAL=35 / CALLER SEES 35 (the linkage item
      *> is the caller's).
      *> A leg fails if any admitted section's item is refused, or if
      *> the status does not reach the item (WS/LOCAL/CALLER not 35).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2FST01.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F0 ASSIGN TO "G2FST01W-ABSENT.DAT"
               FILE STATUS IS WFS.
       DATA DIVISION.
       FILE SECTION.
       FD  F0.
       01  R0          PIC X(4).
       WORKING-STORAGE SECTION.
       01  WFS         PIC XX VALUE "??".
       01  WFS2        PIC XX VALUE "??".
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN INPUT F0.
           DISPLAY "WS=" WFS.
           CALL "G2FST01S" USING WFS2.
           DISPLAY "CALLER SEES " WFS2.
           STOP RUN.
       END PROGRAM G2FST01.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2FST01S.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "G2FST01L-ABSENT.DAT"
               FILE STATUS IS LFS.
           SELECT F2 ASSIGN TO "G2FST01K-ABSENT.DAT"
               FILE STATUS IS KFS.
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       01  R1          PIC X(4).
       FD  F2.
       01  R2          PIC X(4).
       LOCAL-STORAGE SECTION.
       01  LFS         PIC XX VALUE "??".
       LINKAGE SECTION.
       01  KFS         PIC XX.
       PROCEDURE DIVISION USING KFS.
       MAIN-P.
           OPEN INPUT F1.
           DISPLAY "LOCAL=" LFS.
           OPEN INPUT F2.
           GOBACK.
       END PROGRAM G2FST01S.
