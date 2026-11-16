      *>--- conformance-case
      *> id: g2ent01_filler_ws_cd_record
      *> rules: [2002-SR-13.16.18.2-1, 2002-SR-13.16.18.2-2,
      *>    2002-GR-13.16.18.3-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.18.2 SR1 - "If the entry-name clause is not specified
      *> in a data description entry, a report group description entry, or
      *> a screen description entry, the word FILLER is assumed."
      *>   cite.py: OK  §2002:13.16.18.2 SR1
      *> §2002:13.16.18.2 SR2 - "either data-name-1 or FILLER shall be
      *> specified" when the clause is written.
      *>   cite.py: OK  §2002:13.16.18.2 SR2
      *> §2002:13.16.18.3 GR1 - "The word FILLER may be used to name a
      *> data, report, or screen item."
      *>   cite.py: OK  §2002:13.16.18.3 GR1
      *> In 2002 a record description under a CD is made of data
      *> description entries "followed by the data-name or FILLER clause"
      *> (§2002:A.1.1.2, cite.py OK), so the rules reach CD records: CDIN-REC
      *> below carries an unnamed entry and a FILLER entry (the facility is
      *> obsolete at 2002 and draws COBOL0903, a warning). The input CD
      *> record is 87 characters (§2002:A.1.2.2 SR5): 12+4+3+2+66.
      *> DERIVATION:
      *>   WS group G = unnamed "A", FILLER "B", GX "C", unnamed "DE": each
      *>   is an elementary item holding storage and its VALUE, so G is
      *>   "ABCDE" and GX, the third byte, is "C".
      *>   CD: MOVE ALL "Z" fills the 87 bytes; MOVE "QQ" to CD-Q (12) pads
      *>   with spaces; the unnamed X(4) and FILLER X(3) keep "Z" and push
      *>   CD-T to bytes 20-21, so MOVE "TT" TO CD-T gives bytes 1-23
      *>   "QQ" + 10 spaces + "ZZZZZZZ" + "TT" + "ZZ".
      *> Fails if an omitted name or FILLER dropped the item's storage
      *> (GX would not be byte 3; CD-T would shift left), or if either form
      *> were refused in the working-storage or communication section.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2ENT01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G.
           05          PIC X    VALUE "A".
           05  FILLER  PIC X    VALUE "B".
           05  GX      PIC X    VALUE "C".
           05          PIC X(2) VALUE "DE".
       COMMUNICATION SECTION.
       CD  CDIN FOR INPUT.
       01  CDIN-REC.
           02  CD-Q    PIC X(12).
           02          PIC X(4).
           02  FILLER  PIC X(3).
           02  CD-T    PIC X(2).
           02  FILLER  PIC X(66).
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "WS [" G "] GX [" GX "]".
           MOVE ALL "Z" TO CDIN-REC.
           MOVE "QQ" TO CD-Q.
           MOVE "TT" TO CD-T.
           DISPLAY "CD [" CDIN-REC(1:23) "]".
           STOP RUN.
