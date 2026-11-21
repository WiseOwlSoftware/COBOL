      *>--- conformance-case
      *> id: g2aql01_assign_using_qualified
      *> rules: [2002-SR-12.3.4.2-8]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:12.3.4.2 SR8 - "Data-name-1 may be qualified."
      *>   cite.py --check "§2002:12.3.4.2 SR8" -> OK
      *> §2002:12.3.4.3 GR3b: with USING the connector "is associated
      *> with a physical file identified by the content of the data item
      *> referenced by data-name-1"  -> cite.py OK §2002:12.3.4.3 GR3
      *> Two items share the name FNAME (FNAME OF G1, FNAME OF G2), so
      *> only a qualified data-name-1 can name either. F1 is assigned
      *> USING FNAME OF G1 (content G2AQLA.DAT), F2 USING FNAME IN G2
      *> (content G2AQLB.DAT). The implementor's content rule
      *> (docs/conformance DOC-A.1-73: the trimmed content is the
      *> assign target, mapped as literal-1 is) lets the literal
      *> entries CHKA/CHKB reach the same files.
      *> DERIVATION OF THE EXPECTED OUTPUT:
      *>   F1 written with AAAA goes to the file G1's FNAME names; F2's
      *>   only file is G2AQLB.DAT, so after F1 alone G2AQLB.DAT is
      *>   absent (OPTIONAL CHKB opens 05)  => F1=00 / CHKB1=05
      *>   CHKA reads it back                => CHKA=00 AAAA
      *>   F2 written with BBBB, read by CHKB => F2=00 / CHKB2=00 BBBB
      *>   CHKA still holds AAAA (F2 did not reach G1's file)
      *>                                     => CHKA2=00 AAAA
      *> A leg fails if the qualified form is refused, if a qualifier is
      *> ignored so both SELECTs reach one FNAME (CHKB1 00 or CHKA2
      *> BBBB), or if the content is not that of the qualified item.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2AQL01.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN USING FNAME OF G1
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS ST-1.
           SELECT F2 ASSIGN USING FNAME IN G2
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS ST-2.
           SELECT OPTIONAL CHKA ASSIGN TO "G2AQLA.DAT"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS ST-C.
           SELECT OPTIONAL CHKB ASSIGN TO "G2AQLB.DAT"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS ST-C.
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       01  F1-REC      PIC X(4).
       FD  F2.
       01  F2-REC      PIC X(4).
       FD  CHKA.
       01  CHKA-REC    PIC X(4).
       FD  CHKB.
       01  CHKB-REC    PIC X(4).
       WORKING-STORAGE SECTION.
       01  G1.
           05 FNAME    PIC X(20) VALUE "G2AQLA.DAT".
       01  G2.
           05 FNAME    PIC X(20) VALUE "G2AQLB.DAT".
       01  ST-1        PIC XX.
       01  ST-2        PIC XX.
       01  ST-C        PIC XX.
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT F1.
           DISPLAY "F1=" ST-1.
           MOVE "AAAA" TO F1-REC. WRITE F1-REC.
           CLOSE F1.
           OPEN INPUT CHKB.
           DISPLAY "CHKB1=" ST-C.
           CLOSE CHKB.
           OPEN INPUT CHKA.
           MOVE SPACES TO CHKA-REC.
           READ CHKA AT END CONTINUE END-READ.
           DISPLAY "CHKA=" ST-C " " CHKA-REC.
           CLOSE CHKA.
           OPEN OUTPUT F2.
           DISPLAY "F2=" ST-2.
           MOVE "BBBB" TO F2-REC. WRITE F2-REC.
           CLOSE F2.
           OPEN INPUT CHKB.
           MOVE SPACES TO CHKB-REC.
           READ CHKB AT END CONTINUE END-READ.
           DISPLAY "CHKB2=" ST-C " " CHKB-REC.
           CLOSE CHKB.
           OPEN INPUT CHKA.
           MOVE SPACES TO CHKA-REC.
           READ CHKA AT END CONTINUE END-READ.
           DISPLAY "CHKA2=" ST-C " " CHKA-REC.
           CLOSE CHKA.
           STOP RUN.
