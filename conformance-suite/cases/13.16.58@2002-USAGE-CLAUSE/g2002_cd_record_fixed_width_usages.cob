      *>--- conformance-case
      *> id: g2002_cd_record_fixed_width_usages
      *> rules: [2002-GR-13.16.58.3-12, 2002-GR-13.16.58.3-13]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> At 2002 the record description entries after a CD entry are
      *> ordinary data description entries (§2002:A.1.1.2), and an
      *> output CD's records carry no class or size restriction
      *> (§2002:A.1.2.2 SR9), so the float and BINARY-xxx usages are
      *> defined there by the same rules as in working storage.
      *> §2002:13.16.58.3 GR12 - float-short, float-long and
      *>   float-extended are SIGNED numeric items: FS holds -1.5, and
      *>   FL = FS * 2 = -3, FE = FL * 2 = -6 (line FLOAT-SIGNED OK).
      *>   A value held in float-short is expressible in float-long,
      *>   and one held in float-long in float-extended: 0.1 is not
      *>   exact in binary floating point, so FS keeps the nearest
      *>   float-short value; moved to FL and then to FE it compares
      *>   equal to FS (line FLOAT-NEST OK). The size is the
      *>   implementor's: binary32 / binary64 / binary64 (DOC-A.1-207),
      *>   lengths 4 8 8.
      *> §2002:13.16.58.3 GR13 - the length of a SIGNED item equals
      *>   that of the UNSIGNED item of the same usage; DOC-A.1-207
      *>   gives 1 2 4 8. The alignment is the same too: no alignment
      *>   filler is inserted (DOC-A.1-205: alignment none), so the
      *>   record is 4+8+8+1+1+2+2+4+4+8+8 = 50 bytes.
      *> Each leg fails if a CD record item of these usages is
      *> unsigned, loses precision on widening, or takes another width.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2USG02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  L1  PIC 99.
       01  L2  PIC 99.
       01  L3  PIC 99.
       01  L4  PIC 99.
       COMMUNICATION SECTION.
       CD  CO FOR OUTPUT
           TEXT LENGTH IS CO-LEN
           STATUS KEY IS CO-STAT.
       01  CREC.
           05  FS USAGE FLOAT-SHORT.
           05  FL USAGE FLOAT-LONG.
           05  FE USAGE FLOAT-EXTENDED.
           05  BCS USAGE BINARY-CHAR SIGNED.
           05  BCU USAGE BINARY-CHAR UNSIGNED.
           05  BSS USAGE BINARY-SHORT SIGNED.
           05  BSU USAGE BINARY-SHORT UNSIGNED.
           05  BLS USAGE BINARY-LONG SIGNED.
           05  BLU USAGE BINARY-LONG UNSIGNED.
           05  BDS USAGE BINARY-DOUBLE SIGNED.
           05  BDU USAGE BINARY-DOUBLE UNSIGNED.
       PROCEDURE DIVISION.
       MAIN.
           MOVE -1.5 TO FS
           COMPUTE FL = FS * 2
           COMPUTE FE = FL * 2
           IF FS < 0 AND FL = -3 AND FE = -6
               DISPLAY "FLOAT-SIGNED OK"
           ELSE
               DISPLAY "FLOAT-SIGNED BAD"
           END-IF
           MOVE 0.1 TO FS
           MOVE FS TO FL
           MOVE FL TO FE
           IF FL = FS AND FE = FS
               DISPLAY "FLOAT-NEST OK"
           ELSE
               DISPLAY "FLOAT-NEST BAD"
           END-IF
           MOVE FUNCTION BYTE-LENGTH(FS) TO L1
           MOVE FUNCTION BYTE-LENGTH(FL) TO L2
           MOVE FUNCTION BYTE-LENGTH(FE) TO L3
           DISPLAY "FLOAT LEN " L1 " " L2 " " L3
           MOVE FUNCTION BYTE-LENGTH(BCS) TO L1
           MOVE FUNCTION BYTE-LENGTH(BCU) TO L2
           DISPLAY "CHAR LEN " L1 " " L2
           MOVE FUNCTION BYTE-LENGTH(BSS) TO L1
           MOVE FUNCTION BYTE-LENGTH(BSU) TO L2
           DISPLAY "SHORT LEN " L1 " " L2
           MOVE FUNCTION BYTE-LENGTH(BLS) TO L1
           MOVE FUNCTION BYTE-LENGTH(BLU) TO L2
           DISPLAY "LONG LEN " L1 " " L2
           MOVE FUNCTION BYTE-LENGTH(BDS) TO L1
           MOVE FUNCTION BYTE-LENGTH(BDU) TO L2
           DISPLAY "DOUBLE LEN " L1 " " L2
           MOVE FUNCTION BYTE-LENGTH(CREC) TO L1
           DISPLAY "RECORD LEN " L1
           STOP RUN.
