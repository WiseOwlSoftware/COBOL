      *>--- conformance-case
      *> id: g2akq01_qualified_key_names
      *> rules: [2002-SR-12.3.4.5.2-1, 2002-SR-12.3.4.12.2-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:12.3.4.5.2 SR1 - "Data-name-1 and data-name-2 may be
      *> qualified." (ALTERNATE RECORD KEY)
      *>   cite.py --check "§2002:12.3.4.5.2 SR1" -> OK
      *> §2002:12.3.4.12.2 SR1 - "Data-name-1 and data-name-2 may be
      *> qualified." (RECORD KEY)
      *>   cite.py --check "§2002:12.3.4.12.2 SR1" -> OK
      *> Both rules GRANT a form, so this ACCEPTED case writes it:
      *>   RECORD KEY IS PK OF IRB                 (12.3.4.12 data-name-1)
      *>   ALTERNATE RECORD KEY IS AK OF IRB ...   (12.3.4.5 data-name-1)
      *>   ALTERNATE RECORD KEY IS SK SOURCE IS S1 OF IRB S2 OF IRB
      *>                                           (12.3.4.5 data-name-2)
      *>   READ ... KEY IS PK OF IRB / AK OF IRB   (the same keys)
      *> The qualification is load-bearing: the decoy record IRA is
      *> declared FIRST and holds PK, AK, S1 and S2 at OTHER byte
      *> positions (IRB: PK 1-3, AK 4-5, S1 6, S2 7; IRA: S2 1, S1 2,
      *> AK 3-4, PK 8-10), so a compiler that ignored the qualifier, or
      *> took the first declaration, keys the file on different bytes.
      *> Derived (each line can fail):
      *>  W1..W3: the three WRITEs succeed. Record 2 repeats AK "AA",
      *>   a key WITH DUPLICATES: §2002:9.1.12.1 "the record just
      *>   written created a duplicate key value for at least one
      *>   alternate record key for which duplicates are allowed" ->
      *>   "02"; records 1 and 3 -> "00". SK (no DUPLICATES) is the
      *>   concatenation S1,S2 (§2002:12.3.4.5.3 GR2 "Record-key-name-1
      *>   defines a record key consisting of the concatenation of all
      *>   occurrences of data-name-2 in the order specified"): XY, ZW,
      *>   QR, unique. Keyed on IRA's S1,S2 it would be "00" each time,
      *>   a duplicate -> "22" (§2002:9.1.12.4).
      *>  N1..N3: OPEN INPUT makes the prime key the key of reference
      *>   (§2002:14.8.26.3 GR12); READ NEXT returns records ascending
      *>   by PK OF IRB: 001, 002, 003. Keyed on IRA's PK (900, 800,
      *>   700) the order would be reversed.
      *>  PK: READ KEY IS PK OF IRB with PK "002" -> 002AAZW800
      *>   (§2002:14.8.29.3 GR27, GR29).
      *>  AK1: AK "BB" -> 003BBQR700. AK2: AK "AA" -> 001AAXY900, "the
      *>   first record in a sequence of duplicates that was released"
      *>   (§2002:14.8.29.3 GR29). IRA's AK bytes hold 1A/2A/3B: no
      *>   record would match, "23".
      *>  SK: S1 "Z", S2 "W" -> key "ZW" -> 002AAZW800.
      *> File IY writes the RECORD KEY SOURCE form with qualified
      *> data-name-2: RECORD KEY IS RN SOURCE IS T1 OF IYB T2 OF IYB.
      *> Decoy IYA, declared first, holds T2 in byte 1 and T1 in byte
      *> 2; IYB holds T1 in byte 1 and T2 in byte 2.
      *>  Y1, Y2: records "BA22" then "AB11" written -> "00" each
      *>   (prime keys "BA" and "AB" unique).
      *>  Y3, Y4: READ NEXT by the prime key RN = T1,T2 of IYB
      *>   (§2002:12.3.4.12.3 GR2) ascends AB11 then BA22. Keyed on
      *>   IYA's bytes (T1,T2 = byte 2, byte 1: "AB" for BA22, "BA" for
      *>   AB11) the order would be BA22, AB11.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2AKQ01.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IX ASSIGN TO "G2AKQ01.IDX"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS PK OF IRB
               ALTERNATE RECORD KEY IS AK OF IRB WITH DUPLICATES
               ALTERNATE RECORD KEY IS SK SOURCE IS S1 OF IRB S2 OF IRB
               FILE STATUS IS IFS.
           SELECT IY ASSIGN TO "G2AKQ01.IDY"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS RN SOURCE IS T1 OF IYB T2 OF IYB
               FILE STATUS IS YFS.
       DATA DIVISION.
       FILE SECTION.
       FD IX.
       01 IRA.
          05 S2 PIC X(1).
          05 S1 PIC X(1).
          05 AK PIC X(2).
          05 FILLER PIC X(3).
          05 PK PIC X(3).
       01 IRB.
          05 PK PIC X(3).
          05 AK PIC X(2).
          05 S1 PIC X(1).
          05 S2 PIC X(1).
          05 TL PIC X(3).
       FD IY.
       01 IYA.
          05 T2 PIC X(1).
          05 T1 PIC X(1).
          05 FILLER PIC X(2).
       01 IYB.
          05 T1 PIC X(1).
          05 T2 PIC X(1).
          05 YD PIC X(2).
       WORKING-STORAGE SECTION.
       01 IFS PIC XX.
       01 YFS PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT IX
           MOVE "001AAXY900" TO IRB WRITE IRB
           DISPLAY "W1 " IFS
           MOVE "002AAZW800" TO IRB WRITE IRB
           DISPLAY "W2 " IFS
           MOVE "003BBQR700" TO IRB WRITE IRB
           DISPLAY "W3 " IFS
           CLOSE IX
           OPEN INPUT IX
           READ IX NEXT
           DISPLAY "N1 " IFS " " IRB
           READ IX NEXT
           DISPLAY "N2 " IFS " " IRB
           READ IX NEXT
           DISPLAY "N3 " IFS " " IRB
           MOVE SPACES TO IRB
           MOVE "002" TO PK OF IRB
           READ IX KEY IS PK OF IRB
           DISPLAY "PK " IFS " " IRB
           MOVE SPACES TO IRB
           MOVE "BB" TO AK OF IRB
           READ IX KEY IS AK OF IRB
           DISPLAY "AK1 " IFS " " IRB
           MOVE SPACES TO IRB
           MOVE "AA" TO AK OF IRB
           READ IX KEY IS AK OF IRB
           DISPLAY "AK2 " IFS " " IRB
           MOVE SPACES TO IRB
           MOVE "Z" TO S1 OF IRB
           MOVE "W" TO S2 OF IRB
           READ IX KEY IS SK
           DISPLAY "SK " IFS " " IRB
           CLOSE IX
           OPEN OUTPUT IY
           MOVE "BA22" TO IYB WRITE IYB
           DISPLAY "Y1 " YFS
           MOVE "AB11" TO IYB WRITE IYB
           DISPLAY "Y2 " YFS
           CLOSE IY
           OPEN INPUT IY
           READ IY NEXT
           DISPLAY "Y3 " YFS " " IYB
           READ IY NEXT
           DISPLAY "Y4 " YFS " " IYB
           CLOSE IY
           STOP RUN.
