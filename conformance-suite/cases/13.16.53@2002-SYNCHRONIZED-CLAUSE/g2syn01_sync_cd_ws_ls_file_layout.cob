      *>--- conformance-case
      *> id: g2syn01_sync_cd_ws_ls_file_layout
      *> rules: [2002-SR-13.16.53.2-1, 2002-SR-13.16.53.2-2,
      *>    2002-GR-13.16.53.3-1, 2002-GR-13.16.53.3-2,
      *>    2002-GR-13.16.53.3-3, 2002-GR-13.16.53.3-4,
      *>    2002-GR-13.16.53.3-5, 2002-GR-13.16.53.3-8,
      *>    2002-GR-13.16.53.3-9]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.53.2 SR1 - "The SYNCHRONIZED clause may be specified
      *>   only for an elementary item": every SYNCHRONIZED below is on an
      *>   elementary item and is ACCEPTED, in a record under an input CD,
      *>   in working-storage (01 and 77), in local-storage and in a file.
      *>   (The group form is refused by g2syn02_sync_group_in_cd_record.)
      *> §2002:13.16.53.2 SR2 - "SYNC is an abbreviation for
      *>   SYNCHRONIZED": SYNC, SYNC LEFT, SYNC RIGHT lay out and hold
      *>   values exactly as SYNCHRONIZED and SYNCHRONIZED RIGHT/LEFT.
      *>
      *> THE IMPLEMENTOR'S SPECIFICATION the general rules defer to:
      *> §2002:13.16.53.3 GR2 "The specific positioning is determined by
      *> the implementor"; GR8 "the implementor shall specify how
      *> elementary items associated with this clause are handled" for
      *> a) "The format on the external media of records or groups
      *> containing elementary items" and b) "Any necessary generation of
      *> implicit FILLER"; GR9 "An implementor may optionally specify
      *> automatic alignment for any internal data representations".
      *> WiseOwl COBOL's specification (docs/conformance DOC-A.1-195,
      *> DOC-A.1-197): every byte position is a natural boundary, no
      *> implicit FILLER is generated, no automatic alignment is
      *> specified; PIC 9(4) COMP is 2 bytes (DOC-A.1-205, DOC-A.1-208).
      *>
      *> DERIVATION.
      *>  GR1: no other item occupies a byte between the natural
      *>   boundaries delimiting a SYNC item; with one-byte boundaries
      *>   there are no unused bytes, so each item starts right after its
      *>   predecessor. GR3 (LEFT: "begin at the left byte of the natural
      *>   boundary") and GR4 (RIGHT: "terminate on the right byte of the
      *>   natural boundary") hold with no adjustment. GR5: "does not
      *>   affect the size of the synchronized data item" -> 2 bytes.
      *>  CD-REC1 = 1+2+1+2+1+2+1+2+1+2+72 = 87, which is also what
      *>   §2002:A.1.2.2 SR5 demands of an input CD record ("shall
      *>   describe a record of exactly 87 alphanumeric character
      *>   positions"); a slack byte anywhere would make it 88 or more.
      *>   CN5X REDEFINES the SYNC RIGHT item: GR1 "The unused bytes are
      *>   not included in the bytes redefined" -> 2.
      *>                                     => CDLEN=87 ITEM=2 REDEF=2
      *>  CD-REC2 implicitly redefines the CD record area (A.1.2.2 SR5),
      *>   so with CA, CB, CC, CD1, CE, CG at bytes 1, 4, 7, 10, 13, 16
      *>   (each separated by one 2-byte binary item)  => CDPOS=ABCDEF
      *>   Any 2-byte boundary would move B..F; an overlap would clobber
      *>   a letter or a value.        => CDVAL=1234 2345 3456 4567 5678
      *>   CN4 has no clause: GR9's optional automatic alignment is not
      *>   specified, so it too follows CD1 directly (byte 11-12 -> E at
      *>   13).
      *>  W-REC: 1+2+1 = 4, WB at byte 4 of the redefining W-IMG;
      *>   77 W77 SYNC RIGHT holds its value     => WS=4 WX 1234 4321
      *>  L-REC (SYNCHRONIZED LEFT): 4            => LS=4 2222
      *>  F-REC written and read back as PIC X(4) through a second file
      *>   on the same name: GR8 a) external format = the record image,
      *>   P at byte 1 and Q at byte 4             => FILE=4 PQ
      *> cite.py --check: OK §2002:13.16.53.2 SR1, SR2;
      *>   §2002:13.16.53.3 GR1, GR2, GR3, GR4, GR5, GR8, GR9;
      *>   §2002:A.1.2.2 SR5.
      *> The COMMUNICATION SECTION draws the obsolete warning COBOL0903.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SYN01.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT OUTF ASSIGN TO "g2syn01.dat".
           SELECT INF  ASSIGN TO "g2syn01.dat".
       DATA DIVISION.
       FILE SECTION.
       FD  OUTF.
       01  F-REC.
           05  FA  PIC X.
           05  FN  PIC 9(4) COMP SYNC RIGHT.
           05  FB  PIC X.
       FD  INF.
       01  I-REC   PIC X(4).
       WORKING-STORAGE SECTION.
       01  W-REC.
           05  WA  PIC X.
           05  WN  PIC 9(4) COMP SYNC.
           05  WB  PIC X.
       01  W-IMG REDEFINES W-REC PIC X(4).
       77  W77 PIC 9(4) COMP SYNC RIGHT.
       LOCAL-STORAGE SECTION.
       01  L-REC.
           05  LA  PIC X.
           05  LN  PIC 9(4) COMP SYNCHRONIZED LEFT.
           05  LB  PIC X.
       COMMUNICATION SECTION.
       CD  CD-IN FOR INPUT.
       01  CD-REC1.
           05  CA   PIC X.
           05  CN1  PIC 9(4) COMP SYNC.
           05  CB   PIC X.
           05  CN2  PIC 9(4) COMP SYNCHRONIZED RIGHT.
           05  CC   PIC X.
           05  CN3  PIC 9(4) COMP SYNC LEFT.
           05  CD1  PIC X.
           05  CN4  PIC 9(4) COMP.
           05  CE   PIC X.
           05  CN5  PIC 9(4) COMP SYNC RIGHT.
           05  CN5X REDEFINES CN5 PIC XX.
           05  CG   PIC X(72).
       01  CD-REC2  PIC X(87).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE SPACES TO CD-REC2.
           MOVE "A" TO CA. MOVE "B" TO CB. MOVE "C" TO CC.
           MOVE "D" TO CD1. MOVE "E" TO CE. MOVE "F" TO CG.
           MOVE 1234 TO CN1. MOVE 2345 TO CN2. MOVE 3456 TO CN3.
           MOVE 4567 TO CN4. MOVE 5678 TO CN5.
           DISPLAY "CDLEN=" FUNCTION BYTE-LENGTH(CD-REC1)
               " ITEM=" FUNCTION BYTE-LENGTH(CN2)
               " REDEF=" FUNCTION BYTE-LENGTH(CN5X).
           DISPLAY "CDPOS=" CD-REC2(1:1) CD-REC2(4:1) CD-REC2(7:1)
               CD-REC2(10:1) CD-REC2(13:1) CD-REC2(16:1).
           DISPLAY "CDVAL=" CN1 " " CN2 " " CN3 " " CN4 " " CN5.
           MOVE "W" TO WA. MOVE "X" TO WB. MOVE 1234 TO WN.
           MOVE 4321 TO W77.
           DISPLAY "WS=" FUNCTION BYTE-LENGTH(W-REC) " "
               W-IMG(1:1) W-IMG(4:1) " " WN " " W77.
           MOVE "L" TO LA. MOVE "M" TO LB. MOVE 2222 TO LN.
           DISPLAY "LS=" FUNCTION BYTE-LENGTH(L-REC) " " LN.
           OPEN OUTPUT OUTF.
           MOVE "P" TO FA. MOVE "Q" TO FB. MOVE 1111 TO FN.
           WRITE F-REC.
           CLOSE OUTF.
           OPEN INPUT INF.
           READ INF.
           DISPLAY "FILE=" FUNCTION BYTE-LENGTH(F-REC) " "
               I-REC(1:1) I-REC(4:1).
           CLOSE INF.
           STOP RUN.
