      *>--- conformance-case
      *> id: g2-sign-cd-record-usage
      *> rules: [2002-SR-13.16.50.2-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.50.2 SR2 - "The usage of an elementary item for
      *> which the SIGN clause is specified shall be display or
      *> national." The entries of a record under a CD are ordinary data
      *> description entries (§2002:A.1.1.2 "A record description
      *> consists of a set of data description entries"), so the rule
      *> reaches them. C1 is an elementary S9(3) item of usage COMP
      *> (computational, neither display nor national) that specifies
      *> SIGN LEADING SEPARATE in the CD record: refused (COBOL2422) at
      *> line 28. The accepted display-usage subjects of the same CD
      *> record are g2-sign-cd-record and g2-sign-cd-default-sign.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SGN05.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 C1 PIC S9(3) COMP SIGN IS LEADING SEPARATE.
           05 D1 PIC S9(3) SIGN IS LEADING SEPARATE.
       PROCEDURE DIVISION.
           STOP RUN.
