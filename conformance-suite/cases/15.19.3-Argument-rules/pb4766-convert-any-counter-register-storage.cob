*>--- conformance-case
*> id: pb4766-convert-any-counter-register-storage
*> rules: [AR-15.19.3-7]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB4766 review R1-2 - ISO §2023:15.19.3 r7: "When the source-format is ANY, argument-1 shall be of any usage, except index,
      *> message-tag, object reference, pointer, function-pointer or program-pointer." (cite.py --check OK): CONVERT reads the
      *> argument's STORAGE. PAGE-COUNTER and LINAGE-COUNTER reference "temporary unsigned integer data items" (§2023:8.4.3.15.4 GR1,
      *> §2023:8.4.3.14.4 GR1), so each converts the storage of its description (docs/CONFORMANCE.md §3, the counter registers'
      *> declared description): PAGE-COUNTER is an unsigned 32-bit binary integer, four bytes, so after INITIATE (value 1,
      *> §2023:14.9.21.4 GR1 c) it converts exactly as a declared BINARY-LONG UNSIGNED item holding 1 and FUNCTION BYTE-LENGTH
      *> says 4; LINAGE-COUNTER is a DISPLAY item of the page size's two digits (LINAGE IS 20 LINES), 1 after OPEN OUTPUT, so
      *> it converts as a PIC 99 item holding 1: the characters "01", hexadecimal 3031. The register's DISPLAY image
      *> ("0000000001") is not its storage, and converting that image was a silent wrong answer.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4766CVT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb4766-cvt-linage.dat".
           SELECT R ASSIGN TO "pb4766-cvt-report.dat".
       DATA DIVISION.
       FILE SECTION.
       FD F LINAGE IS 20 LINES.
       01 FR    PIC X(10).
       FD R REPORT IS RP.
       WORKING-STORAGE SECTION.
       01 BL    BINARY-LONG UNSIGNED VALUE 1.
       01 DI    PIC 99 VALUE 1.
       REPORT SECTION.
       RD RP PAGE LIMIT 20 LINES.
       01 TYPE DETAIL.
          02 LINE 1.
             03 COLUMN 1 PIC X VALUE "X".
       PROCEDURE DIVISION.
           OPEN OUTPUT F
           OPEN OUTPUT R
           INITIATE RP
           DISPLAY "PC=" FUNCTION CONVERT(PAGE-COUNTER ANY ANUM HEX)
           DISPLAY "BL=" FUNCTION CONVERT(BL ANY ANUM HEX)
           DISPLAY "LEN=" FUNCTION BYTE-LENGTH(PAGE-COUNTER)
           DISPLAY "LC=" FUNCTION CONVERT(LINAGE-COUNTER ANY ANUM HEX)
           DISPLAY "DI=" FUNCTION CONVERT(DI ANY ANUM HEX)
           CLOSE F
           TERMINATE RP
           CLOSE R
           STOP RUN.
