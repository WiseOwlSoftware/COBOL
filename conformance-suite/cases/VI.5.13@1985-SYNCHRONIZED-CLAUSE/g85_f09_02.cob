      *>--- conformance-case
      *> id: g85_f09_02
      *> rules: [1985-FMT-VI.5.13.2, 1985-GR-VI.5.13.4-1,
      *>    1985-GR-VI.5.13.4-2, 1985-GR-VI.5.13.4-8]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.13 Format 1 - {SYNCHRONIZED|SYNC} [LEFT|RIGHT]:
      *>   all six forms the format grants are written and accepted:
      *>   SYNCHRONIZED (GN), SYNCHRONIZED LEFT (FL), SYNCHRONIZED
      *>   RIGHT (FR), SYNC (SN; §1985:VI.5.13.3 SR2 "SYNC is an
      *>   abbreviation for SYNCHRONIZED"), SYNC LEFT (EN), SYNC
      *>   RIGHT (RN). A compiler refusing one fails to compile this
      *>   program; FMT-* shows FL and FR hold their values.
      *> THE IMPLEMENTOR'S DETERMINATION the layout legs measure
      *>   (docs/CONFORMANCE.md DOC-A.1-195 and DOC-A.1-197): every
      *>   character position is a natural boundary, no positioning
      *>   is performed and no implicit FILLER is generated. A COMP
      *>   PIC 9(3) or 9(4) item is 2 positions (DOC-A.1-205).
      *> §1985:VI.5.13.4 GR1 - "no other data item occupies any of
      *>   the character positions between the leftmost and
      *>   rightmost natural boundaries delimiting this data item":
      *>   after MOVEs into GN and EN the neighbours GA/GC and EA/EZ
      *>   are intact (G1-*, L-*). Its a./b.: G is 4 positions, so
      *>   its redefinition GR is GRA,GRN,GRC and GT follows at 5
      *>   (G1-*, W-*). Its last sentence: ENR REDEFINES the
      *>   elementary EN and covers exactly EN's 2 positions; EZ is
      *>   untouched by MOVE "MN" TO ENR (L-*).
      *> §1985:VI.5.13.4 GR2 - bare SYNCHRONIZED: positioning "is,
      *>   however, determined by the implementor"; under the
      *>   determination GN begins at position 2, so GC is 4 (W-*).
      *> GR3 (LEFT) and GR4 (RIGHT) are NOT claimed: with every
      *>   position a natural boundary they hold for every placement,
      *>   so no leg here can fail for a GR3 or GR4 reason. The L-*
      *>   and R-* legs witness GR1 and GR8 b. (no implicit FILLER).
      *> §1985:VI.5.13.4 GR8 a. and b. - the external-media format
      *>   of SREC and the absence of implicit FILLER: two records
      *>   written through SREC (SYNC item SN) read back through
      *>   SRECX with "A" at 1 and "C" at 4 (F-*), and through SF2,
      *>   whose 4-position record S2REC has no SYNC item, as A/C
      *>   then B/D (F2-*): a pad or transform of the SYNC item on
      *>   the media desynchronises the second SF2 read. An
      *>   implementation aligning a binary item to a 2-position
      *>   boundary inserts FILLER after the 1-position item and
      *>   moves every C/Z/T leg one position right, failing them.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F0902.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "G85F0902.DAT"
               ORGANIZATION IS SEQUENTIAL.
           SELECT SF2 ASSIGN TO "G85F0902.DAT"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  SF LABEL RECORDS ARE STANDARD.
       01  SREC.
           05  SA            PIC X.
           05  SN            PIC 9(4) COMP SYNC.
           05  SC            PIC X.
       01  SRECX.
           05  SXA           PIC X.
           05  SXN           PIC XX.
           05  SXC           PIC X.
       FD  SF2 LABEL RECORDS ARE STANDARD.
       01  S2REC.
           05  S2A           PIC X.
           05  S2M           PIC XX.
           05  S2C           PIC X.
       WORKING-STORAGE SECTION.
       01  WG.
           05  G.
               10  GA        PIC X.
               10  GN        PIC 9(4) COMP SYNCHRONIZED.
               10  GC        PIC X.
           05  GR REDEFINES G.
               10  GRA       PIC X.
               10  GRN       PIC XX.
               10  GRC       PIC X.
           05  GT            PIC X(3).
       01  WGR REDEFINES WG.
           05  WRA           PIC X.
           05  FILLER        PIC XX.
           05  WRC           PIC X.
           05  WRT           PIC X(3).
       01  WE.
           05  EA            PIC X VALUE "L".
           05  EN            PIC 9(4) COMP SYNC LEFT.
           05  ENR REDEFINES EN PIC XX.
           05  EZ            PIC X VALUE "Z".
       01  WER REDEFINES WE.
           05  WL1           PIC X.
           05  WL2           PIC XX.
           05  WL3           PIC X.
       01  WRT2.
           05  RA            PIC X VALUE "R".
           05  RN            PIC 9(3) COMP SYNC RIGHT.
           05  RZ            PIC X VALUE "Y".
       01  WRT2R REDEFINES WRT2.
           05  RRA           PIC X.
           05  FILLER        PIC XX.
           05  RRZ           PIC X.
       01  WF.
           05  FA            PIC X VALUE "F".
           05  FL            PIC 9(4) COMP SYNCHRONIZED LEFT.
           05  FR            PIC 9(4) COMP SYNCHRONIZED RIGHT.
           05  FZ            PIC X VALUE "Z".
       01  D4                PIC 9(4).
       01  D4B               PIC 9(4).
       01  D3                PIC 9(3).
       01  EOF-SW            PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "A" TO GA.
           MOVE 1234 TO GN.
           MOVE "C" TO GC.
           MOVE "TTT" TO GT.
           MOVE GN TO D4.
           DISPLAY "G1-GN=" D4 " GRA=" GRA " GRC=" GRC.
           DISPLAY "W-A=" WRA " C=" WRC " T=" WRT.
           MOVE "MN" TO ENR.
           DISPLAY "L-WL2=" WL2 " WL1=" WL1 " WL3=" WL3 " EZ=" EZ.
           MOVE 12 TO EN.
           MOVE EN TO D4.
           DISPLAY "L-EN=" D4 " EA=" EA " EZ=" EZ.
           MOVE 789 TO RN.
           MOVE RN TO D3.
           DISPLAY "R-RN=" D3 " RRA=" RRA " RRZ=" RRZ.
           MOVE 1 TO FL.
           MOVE 2 TO FR.
           MOVE FL TO D4.
           MOVE FR TO D4B.
           DISPLAY "FMT-FL=" D4 " FR=" D4B " FA=" FA " FZ=" FZ.
           OPEN OUTPUT SF.
           MOVE "A" TO SA.
           MOVE 1234 TO SN.
           MOVE "C" TO SC.
           WRITE SREC.
           MOVE "B" TO SA.
           MOVE 5678 TO SN.
           MOVE "D" TO SC.
           WRITE SREC.
           CLOSE SF.
           OPEN INPUT SF.
           READ SF AT END MOVE "Y" TO EOF-SW.
           MOVE SN TO D4.
           DISPLAY "F-EOF=" EOF-SW " A=" SXA " C=" SXC " SN=" D4.
           CLOSE SF.
           OPEN INPUT SF2.
           READ SF2 AT END MOVE "Y" TO EOF-SW.
           DISPLAY "F2-1=" S2A " " S2C.
           READ SF2 AT END MOVE "Y" TO EOF-SW.
           DISPLAY "F2-2=" S2A " " S2C " EOF=" EOF-SW.
           CLOSE SF2.
           STOP RUN.
