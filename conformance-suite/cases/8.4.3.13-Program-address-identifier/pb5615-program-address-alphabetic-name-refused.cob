      *>--- conformance-case
      *> id: pb5615-program-address-alphabetic-name-refused
      *> rules: [SR-8.4.3.13.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5615 - §2023:8.4.3.13.3 SR1: "Identifier-1 shall be of
      *>   category alphanumeric or national." NM is PIC A, a category
      *>   of its own (§2023:8.5.2.1 Table 2), and from COBOL-2014 on no
      *>   sentence treats an alphabetic item as alphanumeric (§2014:E.2).
      *>   Expected: COBOL2157 at 2014 and 2023; the 2002 twin
      *>   pb5615-program-address-alphabetic-name-2002 accepts it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5615AR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  NM  PIC A(8) VALUE "NOSUCHPG".
       01  PP  USAGE PROGRAM-POINTER.
       PROCEDURE DIVISION.
       MAIN-P.
           SET PP TO ADDRESS OF PROGRAM NM
           STOP RUN.
