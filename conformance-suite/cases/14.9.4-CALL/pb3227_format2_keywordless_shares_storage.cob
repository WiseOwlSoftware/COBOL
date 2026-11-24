*>--- conformance-case
*> id: pb3227_format2_keywordless_shares_storage
*> rules: [GR-14.9.4.4-5, GR-14.9.4.4-8, GR-14.9.4.4-9, GR-14.2.3-8]
*> accept: [2002, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3227. ISO 14.9.4.4 GR5 ("Both the BY CONTENT and BY REFERENCE phrases are transitive across the
      *> parameters that follow them") is printed under FORMAT 1. In Format 2, GR9 decides an argument written without
      *> BY REFERENCE, BY CONTENT or BY VALUE from its CORRESPONDING FORMAL: a) 1. BY REFERENCE when the formal is BY
      *> REFERENCE and the argument meets SR3, whatever phrase an earlier argument wrote. 14.2.3 GR8: "If the argument is
      *> passed by reference, the activated runtime element operates as if the formal parameter occupies the same storage
      *> area as the argument." So when NUM is passed twice BY REFERENCE, to A (redefined by AX) and to B, a store through
      *> AX is B's value at once: each callee displays B: 5678, and the caller's NUM is 5678 after each CALL.
      *> Why each leg can fail: the storage claim that keeps a by-reference argument in one shared area used to apply
      *> GR5 to Format 2 too, so a keyword-less NUM after BY VALUE (leg 1) or BY CONTENT (legs 2 and 4) was not claimed,
      *> and A held a private copy: B displayed the old 1234. Leg 3: the later argument's B-AND steers the keyword-less
      *> NUM into the boolean node of the parse, and GR8 ("An argument that consists merely of a single identifier or
      *> literal is regarded as an identifier") still makes it identifier-2; the claim read only the arithmetic node and
      *> missed it. Leg 4 is Format 2 by the bare program-prototype-name (14.9.4.2, the AS phrase omitted), declared in
      *> the REPOSITORY paragraph; the program prototype definition in the group gives its formals (12.3.8.4 GR10 b)).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3227PRO IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 C PIC 9(4) BINARY.
       01 A PIC 9(4).
       01 B PIC 9(4).
       PROCEDURE DIVISION USING C A B.
       END PROGRAM PB3227PRO.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3227MAIN.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM PB3227PRO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9(4) BINARY VALUE 7.
       01 NUM PIC 9(4) VALUE 1234.
       01 B1 PIC 1 VALUE B"1".
       01 B2 PIC 1 VALUE B"0".
       PROCEDURE DIVISION.
           CALL "PB3227VAL" AS NESTED USING BY VALUE N NUM NUM
           DISPLAY "AFTER 1: " NUM
           MOVE 1234 TO NUM
           CALL "PB3227REF" AS NESTED USING BY CONTENT N NUM NUM
           DISPLAY "AFTER 2: " NUM
           MOVE 1234 TO NUM
           CALL "PB3227BOO" AS NESTED USING NUM NUM BY CONTENT B1 B-AND B2
           DISPLAY "AFTER 3: " NUM
           MOVE 1234 TO NUM
           CALL PB3227PRO USING BY CONTENT N NUM NUM
           DISPLAY "AFTER 4: " NUM
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3227VAL.
       DATA DIVISION.
       LINKAGE SECTION.
       01 V PIC 9(4) BINARY.
       01 A PIC 9(4).
       01 AX REDEFINES A PIC X(4).
       01 B PIC 9(4).
       PROCEDURE DIVISION USING BY VALUE V BY REFERENCE A B.
           MOVE "5678" TO AX
           DISPLAY "B 1: " B
           GOBACK.
       END PROGRAM PB3227VAL.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3227REF.
       DATA DIVISION.
       LINKAGE SECTION.
       01 C PIC 9(4) BINARY.
       01 A PIC 9(4).
       01 AX REDEFINES A PIC X(4).
       01 B PIC 9(4).
       PROCEDURE DIVISION USING C A B.
           MOVE "5678" TO AX
           DISPLAY "B 2: " B
           GOBACK.
       END PROGRAM PB3227REF.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3227BOO.
       DATA DIVISION.
       LINKAGE SECTION.
       01 A PIC 9(4).
       01 AX REDEFINES A PIC X(4).
       01 B PIC 9(4).
       01 C PIC 1.
       PROCEDURE DIVISION USING A B C.
           MOVE "5678" TO AX
           DISPLAY "B 3: " B
           GOBACK.
       END PROGRAM PB3227BOO.
       END PROGRAM PB3227MAIN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3227PRO.
       DATA DIVISION.
       LINKAGE SECTION.
       01 C PIC 9(4) BINARY.
       01 A PIC 9(4).
       01 AX REDEFINES A PIC X(4).
       01 B PIC 9(4).
       PROCEDURE DIVISION USING C A B.
           MOVE "5678" TO AX
           DISPLAY "B 4: " B
           GOBACK.
       END PROGRAM PB3227PRO.
