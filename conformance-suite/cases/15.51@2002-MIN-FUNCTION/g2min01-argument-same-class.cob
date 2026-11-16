      *>--- conformance-case
      *> id: g2min01-argument-same-class
      *> rules: [2002-AR-15.51.2-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.51.2 AR2 - "All arguments shall be of the same class with
      *>   the exception that mixing of arguments of alphabetic and
      *>   alphanumeric classes is allowed." Every MIN below is ACCEPTED:
      *>   MIX-1/MIX-2 mix PIC A(3) with PIC X(3), the granted exception;
      *>   CD-MIX      mixes the communication-section record item IN-Q
      *>               (PIC X(12), class alphanumeric; the CD facility is
      *>               obsolete in 2002 and draws COBOL0903, a warning)
      *>               with PIC A(3);
      *>   EDIT-ALN    mixes PIC ZZ9 (numeric-edited, usage display: class
      *>               alphanumeric by §2002:8.5.2 Table 3) with PIC X(3);
      *>   NUM         mixes PIC 9(3) with PIC 9(3) USAGE NATIONAL: both are
      *>               class numeric in 2002 (§2002:8.5.2.9 item 1; kb/Work
      *>               PB5625 reading B), the same class;
      *>   NAT         mixes PIC N(3) with PIC ZZ9 USAGE NATIONAL, both class
      *>               national (Table 3: "Numeric-edited (if usage is
      *>               national)").
      *> Values (§2002:15.51.3 RV1, RV3; comparisons by the simple-condition
      *>   rules): "ABC" < "PQR", so MIX-1 and MIX-2 return ABC and PQR is
      *>   returned against "ZZZ"; IN-Q "ABC" padded with spaces is less
      *>   than "PQR", returned with its own size 12 (CD-L=12); "  7" begins
      *>   with a space and is less than "ABC"; 125 < 250; " 12" begins with
      *>   a space and is less than "QRS".
      *> A compiler that refused any pairing above (in particular the two
      *>   usages of class numeric) fails to compile the case. Refused
      *>   beside it (not the closing witness): numeric with alphanumeric,
      *>   g2min02-numeric-with-alphanumeric; PIC 9 USAGE NATIONAL with
      *>   PIC N, class numeric with class national,
      *>   g2min03-numeric-national-with-national.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2MIN01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  AL              PIC A(3) VALUE "PQR".
       01  AN              PIC X(3) VALUE "ABC".
       01  AZ              PIC X(3) VALUE "ZZZ".
       01  ZE              PIC ZZ9.
       01  D9              PIC 9(3) VALUE 250.
       01  N9              PIC 9(3) USAGE NATIONAL VALUE 125.
       01  NA              PIC N(3) VALUE N"QRS".
       01  NZ              PIC ZZ9 USAGE NATIONAL.
       01  O               PIC X(12).
       01  ON3             PIC N(3).
       01  R               PIC 999.
       01  L               PIC 99.
       COMMUNICATION SECTION.
       CD  IN-CD FOR INPUT.
       01  IN-REC.
           05  IN-Q        PIC X(12).
           05  IN-R        PIC X(75).
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE 7 TO ZE
           MOVE 12 TO NZ
           MOVE "ABC" TO IN-Q
           MOVE FUNCTION MIN(AL AN) TO O
           DISPLAY "MIX-1=[" O "]"
           MOVE FUNCTION MIN(AZ AL) TO O
           DISPLAY "MIX-2=[" O "]"
           MOVE FUNCTION MIN(IN-Q AL) TO O
           DISPLAY "CD-MIX=[" O "]"
           MOVE FUNCTION LENGTH(FUNCTION MIN(IN-Q AL)) TO L
           DISPLAY "CD-L=" L
           MOVE FUNCTION MIN(AN ZE) TO O
           DISPLAY "EDIT-ALN=[" O "]"
           COMPUTE R = FUNCTION MIN(D9 N9)
           DISPLAY "NUM=" R
           MOVE FUNCTION MIN(NA NZ) TO ON3
           DISPLAY "NAT=[" FUNCTION DISPLAY-OF(ON3) "]"
           STOP RUN.
