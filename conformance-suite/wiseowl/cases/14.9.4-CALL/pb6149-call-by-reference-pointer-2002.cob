      *>--- conformance-case
      *> id: pb6149-call-by-reference-pointer-2002
      *> rules: [SR-14.9.4.3-10]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB6149. A Format 1 CALL shall pass neither a strongly-
      *> typed group item nor a data item of class object or pointer BY
      *> REFERENCE. At --std 2002 the rule is §2002:14.8.4.2 SR11
      *> (§2023:14.9.4.3 SR10), and the message cites the 2002 rule.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6149P.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P USAGE POINTER.
       PROCEDURE DIVISION.
       MAIN.
           CALL "PB6149X" USING BY REFERENCE P.
           STOP RUN.
       END PROGRAM PB6149P.
