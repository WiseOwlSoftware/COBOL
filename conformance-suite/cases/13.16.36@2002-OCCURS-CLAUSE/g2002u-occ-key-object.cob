      *>--- conformance-case
      *> id: g2002u-occ-key-object
      *> rules: [2002-SR-13.16.36.2-8]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.36.2 SR8 - "The KEY phrase shall not be specified
      *> for a data item of class boolean, object, or pointer." O is
      *> USAGE OBJECT REFERENCE, a universal object reference (class
      *> object), named by the ASCENDING KEY phrase. The table is under a
      *> TYPEDEF STRONG type declaration because §2002:13.16.58.2 SR13
      *> admits this usage only at level 1 or subordinate to a type
      *> declaration with the STRONG phrase, so the KEY rule is the only
      *> one the entry breaks. The compiler must
      *> refuse the entry at the KEY phrase; it fails if the object
      *> leg of the class check is missing.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2OCKO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T1 TYPEDEF STRONG.
           05 E OCCURS 3 TIMES ASCENDING KEY IS O.
              10 O USAGE OBJECT REFERENCE.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "COMPILED"
           STOP RUN.
