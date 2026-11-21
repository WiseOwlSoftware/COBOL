      *>--- conformance-case
      *> id: g2ext01_external_entries_own_status
      *> rules: [2002-GR-12.3.4.3-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:12.3.4.3 GR1 - for an external file connector "all file
      *> control entries in the run unit that reference this file
      *> connector shall have" items a) to m); h) "The same
      *> specification of the PADDING CHARACTER clause. If data-name-8
      *> is specified, it shall reference an external data item."
      *>   cite.py --check "§2002:12.3.4.3 GR1" -> OK (both quotes)
      *> The 2002 list has NO FILE STATUS item: each entry may name its
      *> own, non-external status item. §2002:13.16.20.3 GR2a: the FD
      *> with EXTERNAL gives "an external file connector" (cite.py OK),
      *> so both programs' entries reference ONE connector.
      *> G2EXT08 and G2EXT10 write identical entries for XF (OPTIONAL,
      *> sequential, RESERVE 2, PADDING CHARACTER IS the external EXT-PAD)
      *> except FILE STATUS: FS-A in G2EXT08, FS-B in G2EXT10, both
      *> local working-storage items.
      *> DERIVATION OF THE EXPECTED OUTPUT:
      *>   G2EXT08 OPEN OUTPUT, WRITE AAAA      => P1 OPEN 00 / P1 WRITE 00
      *>   G2EXT10 WRITEs BBBB on the SAME open connector, then CLOSEs it
      *>                                        => P2 WRITE 00 / P2 CLOSE 00
      *>   G2EXT08 reopens INPUT and reads both records in order, then
      *>   at end                               => P1 OPEN 00 / P1 READ 00
      *>   AAAA / P1 READ 00 BBBB / P1 END 10
      *> A leg fails if the differing FILE STATUS items are refused (the
      *> 2023 item i) applied at 2002), if the PADDING CHARACTER entries
      *> are refused, or if the two entries do not share one connector
      *> (P2 WRITE not 00, or the file lacks BBBB).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2EXT08.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT OPTIONAL XF ASSIGN TO "G2EXT08.DAT"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL
               RESERVE 2 AREAS
               PADDING CHARACTER IS EXT-PAD
               FILE STATUS IS FS-A.
       DATA DIVISION.
       FILE SECTION.
       FD  XF IS EXTERNAL.
       01  XR          PIC X(4).
       WORKING-STORAGE SECTION.
       01  EXT-PAD IS EXTERNAL PIC X.
       01  FS-A        PIC XX.
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT XF.
           DISPLAY "P1 OPEN " FS-A.
           MOVE "AAAA" TO XR. WRITE XR.
           DISPLAY "P1 WRITE " FS-A.
           CALL "G2EXT10".
           OPEN INPUT XF.
           DISPLAY "P1 OPEN " FS-A.
           READ XF AT END CONTINUE END-READ.
           DISPLAY "P1 READ " FS-A " " XR.
           READ XF AT END CONTINUE END-READ.
           DISPLAY "P1 READ " FS-A " " XR.
           READ XF AT END CONTINUE END-READ.
           DISPLAY "P1 END " FS-A.
           CLOSE XF.
           STOP RUN.
       END PROGRAM G2EXT08.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2EXT10.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT OPTIONAL XF ASSIGN TO "G2EXT08.DAT"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL
               RESERVE 2 AREAS
               PADDING CHARACTER IS EXT-PAD
               FILE STATUS IS FS-B.
       DATA DIVISION.
       FILE SECTION.
       FD  XF IS EXTERNAL.
       01  XR          PIC X(4).
       WORKING-STORAGE SECTION.
       01  EXT-PAD IS EXTERNAL PIC X.
       01  FS-B        PIC XX.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE "BBBB" TO XR. WRITE XR.
           DISPLAY "P2 WRITE " FS-B.
           CLOSE XF.
           DISPLAY "P2 CLOSE " FS-B.
           GOBACK.
       END PROGRAM G2EXT10.
