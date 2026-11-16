      *>--- conformance-case
      *> id: g2call08_operand_sections
      *> rules: [2002-SR-14.8.4.2-3, 2002-SR-14.8.4.2-7]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.4.2 SR3 - "Identifier-2 shall reference an
      *> address-identifier or a data item defined in the file,
      *> working-storage, local-storage, linkage, or communication
      *> section." §2002:14.8.4.2 SR7 - "Identifier-3 shall reference a
      *> data item defined in the file, working-storage, local-storage,
      *> linkage, or communication section." G2SDK holds an argument
      *> (identifier-2) and a RETURNING item (identifier-3) in each of
      *> the five sections, the communication section through the record
      *> of an input CD, and passes an address-identifier BY CONTENT.
      *> The calls use Format 1 (ON OVERFLOW), Format 2 (ON EXCEPTION)
      *> and Format 3 (AS NESTED), all under the ALL FORMATS rules.
      *> Contained G2SDX shows its argument, stores ZZZZ into it and
      *> returns the argument's entry value with "R" in position 1.
      *> By §2002:14.8.4.3 GR5 an argument with no phrase goes BY
      *> REFERENCE (Formats 1, 2), by GR9a1 an argument meeting SR3 goes
      *> BY REFERENCE (Format 3), and by GR4 the result is placed in
      *> identifier-3. Derived lines:
      *>   X FFFF / F ZZZZ RFFF       file section, Format 1
      *>   X WWWW / W ZZZZ RWWW       working-storage, Format 2
      *>   X LLLL / L ZZZZ RLLL       local-storage, Format 1
      *>   X KKKK / K ZZZZ RKKK       linkage, Format 2
      *>   X CCCC / C ZZZZ RCCC       communication (CD record), Format 1
      *>   X DDDD / D DDDD RDDD       CD item BY CONTENT: stays DDDD
      *>   X QQQQ / Q ZZZZ RQQQ       CD item, Format 3 AS NESTED
      *>   A ZZZZ                     ADDRESS OF WI BY CONTENT
      *>   M ZZZZ RKKK                the linkage legs reached the caller
      *> Fails if: an operand of any section (the communication section
      *> above all, which 2023 removed) is refused, a BY REFERENCE
      *> argument is passed by content (the section's line keeps its
      *> entry value), the BY CONTENT CD argument is changed, or a
      *> RETURNING item in any section misses the result. COBOL0903
      *> (the obsolete communication facility, §2002:G) is a warning.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SDM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 MI PIC X(4) VALUE "KKKK".
       01 MR PIC X(4) VALUE SPACES.
       PROCEDURE DIVISION.
       MAIN.
           CALL "G2SDK" USING MI MR
           DISPLAY "M " MI " " MR
           STOP RUN.
       END PROGRAM G2SDM.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SDK.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "G2SDK.DAT"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 FREC.
          05 FR1 PIC X(4).
          05 FR2 PIC X(4).
       WORKING-STORAGE SECTION.
       01 WI PIC X(4) VALUE "WWWW".
       01 WR PIC X(4) VALUE SPACES.
       LOCAL-STORAGE SECTION.
       01 LI PIC X(4) VALUE "LLLL".
       01 LRR PIC X(4) VALUE SPACES.
       LINKAGE SECTION.
       01 KI PIC X(4).
       01 KR PIC X(4).
       COMMUNICATION SECTION.
       CD CI FOR INPUT
           SYMBOLIC QUEUE IS CQ.
       01 CREC.
          05 CI1 PIC X(4).
          05 CR3 PIC X(4).
          05 FILLER PIC X(79).
       PROCEDURE DIVISION USING KI KR.
       K1.
           OPEN OUTPUT F1
           MOVE "FFFF" TO FR1
           CALL "G2SDX" USING FR1 RETURNING FR2
               ON OVERFLOW DISPLAY "F OVF"
           END-CALL
           DISPLAY "F " FR1 " " FR2
           CLOSE F1
           CALL "G2SDX" USING WI RETURNING WR
               ON EXCEPTION DISPLAY "W EXC"
           END-CALL
           DISPLAY "W " WI " " WR
           CALL "G2SDX" USING LI RETURNING LRR
               ON OVERFLOW DISPLAY "L OVF"
           END-CALL
           DISPLAY "L " LI " " LRR
           CALL "G2SDX" USING KI RETURNING KR
               ON EXCEPTION DISPLAY "K EXC"
           END-CALL
           DISPLAY "K " KI " " KR
           MOVE "CCCC" TO CI1
           CALL "G2SDX" USING BY REFERENCE CI1 RETURNING CR3
               ON OVERFLOW DISPLAY "C OVF"
           END-CALL
           DISPLAY "C " CI1 " " CR3
           MOVE "DDDD" TO CI1
           CALL "G2SDX" USING BY CONTENT CI1 RETURNING CR3
           DISPLAY "D " CI1 " " CR3
           MOVE "QQQQ" TO CI1
           CALL "G2SDX" AS NESTED USING CI1 RETURNING CR3
           END-CALL
           DISPLAY "Q " CI1 " " CR3
           CALL "G2SDA" USING BY CONTENT ADDRESS OF WI
           GOBACK.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SDX.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LX PIC X(4).
       01 LXR PIC X(4).
       PROCEDURE DIVISION USING LX RETURNING LXR.
       X1.
           DISPLAY "X " LX
           MOVE LX TO LXR
           MOVE "R" TO LXR(1:1)
           MOVE "ZZZZ" TO LX
           GOBACK.
       END PROGRAM G2SDX.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SDA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 LT PIC X(4) BASED.
       LINKAGE SECTION.
       01 LP USAGE POINTER.
       PROCEDURE DIVISION USING LP.
       A1.
           SET ADDRESS OF LT TO LP
           DISPLAY "A " LT
           GOBACK.
       END PROGRAM G2SDA.
       END PROGRAM G2SDK.
