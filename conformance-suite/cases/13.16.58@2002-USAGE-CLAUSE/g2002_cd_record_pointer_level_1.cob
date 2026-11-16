      *>--- conformance-case
      *> id: g2002_cd_record_pointer_level_1
      *> rules: [2002-SR-13.16.58.2-13]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.58.2 SR13 - "A USAGE clause with the OBJECT
      *> REFERENCE, POINTER, or PROGRAM-POINTER phrase may be specified
      *> only for an elementary data item with level 1 or subordinate to
      *> a type declaration that includes the STRONG phrase." Both
      *> permitted positions are written and used here:
      *>  - level 1: CP, CQ and CJ are level-01 records of an output CD
      *>    (§2002:A.1.2.2 SR9: ordinary record description entries
      *>    that implicitly redefine the CD's record area); WS-level 1
      *>    is the same position in working storage.
      *>  - under a STRONG type declaration: SP, level 05 under ST
      *>    TYPEDEF STRONG, reached through SV TYPE ST in working
      *>    storage. The STRONG branch cannot be written under a CD: an
      *>    item of a STRONG type "shall not be implicitly or explicitly
      *>    redefined in whole or in part" (§2002:13.16.55.2 SR4) and a
      *>    CD record implicitly redefines the record area.
      *> The CD records share one area, so each is set and used before
      *> the next is set. Expected lines: CP and SP each address BUF,
      *> so BX shows WXYZ twice; CQ is set to the entry G2UA02S, whose
      *> CALL shows SUB CALLED; CJ, set to NULL, compares equal to NULL.
      *> The refused positions are the g2002-cd-*-in-group cases.
      *> The program fails if any permitted entry is refused or does not
      *> hold the value set into it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2UA02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  BUF PIC X(4) VALUE "WXYZ".
       01  BX BASED.
           05 BXC PIC X(4).
       01  ST TYPEDEF STRONG.
           05 SP USAGE POINTER.
           05 SN PIC 9(3).
       01  SV TYPE ST.
       COMMUNICATION SECTION.
       CD  CO FOR OUTPUT.
       01  CP USAGE POINTER.
       01  CQ USAGE PROGRAM-POINTER.
       01  CJ USAGE OBJECT REFERENCE.
       PROCEDURE DIVISION.
       MAIN.
           SET CP TO ADDRESS OF BUF
           SET ADDRESS OF BX TO CP
           DISPLAY "CP BX=" BXC
           SET SP OF SV TO CP
           SET ADDRESS OF BX TO NULL
           SET ADDRESS OF BX TO SP OF SV
           DISPLAY "SP BX=" BXC
           SET CQ TO ENTRY "G2UA02S"
           CALL CQ
           SET CJ TO NULL
           IF CJ = NULL
               DISPLAY "CJ NULL"
           ELSE
               DISPLAY "CJ NOT NULL"
           END-IF
           STOP RUN.
       END PROGRAM G2UA02.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2UA02S.
       PROCEDURE DIVISION.
           DISPLAY "SUB CALLED".
           EXIT PROGRAM.
       END PROGRAM G2UA02S.
