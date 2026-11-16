      *>--- conformance-case
      *> id: g2-redefines-cd-entry-rules
      *> rules: [2002-SR-13.16.42.2-1, 2002-SR-13.16.42.2-2, 2002-SR-13.16.42.2-5, 2002-SR-13.16.42.2-6, 2002-SR-13.16.42.2-7, 2002-SR-13.16.42.2-8]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> The accepted forms of the REDEFINES entry rules of
      *> §2002:13.16.42.2 in a record description under an output CD, where
      *> 2002 allows a REDEFINES below level 1 (§2002:13.16.42.2 SR4 "This
      *> clause shall not be specified in level 1 entries in the
      *> communication section"; §2002:A.1.2.2 SR9). Each leg is a legal
      *> entry the compiler must accept and lay over data-name-2's storage
      *> (§2002:13.16.42.3 GR1), so a wrong refusal fails the compile and a
      *> wrong binding fails the line.
      *> SR1 "The REDEFINES clause shall immediately follow the entry-name
      *> clause; if the entry-name clause is not specified, the REDEFINES
      *> clause shall immediately follow the level-number": C1R, C1S, QR,
      *> T2, AR and BR write REDEFINES right after the name; the unnamed
      *> 05 entry writes it right after the level-number (FILLER assumed,
      *> §2002:13.16.18.2 SR1). Refused forms: g2-redefines-cd-after-picture.
      *> SR2 "The level-numbers of data-name-2 and the subject of the entry
      *> shall be identical": 05 over 05 (C1R, C1S, QR) and 10 over 10
      *> (T2, AR, BR). Refused: g2-redefines-cd-level-mismatch, -88.
      *> SR5 "No entry having a level-number numerically lower than the
      *> level-number of data-name-2 may occur between": Q1 and Q2 (level
      *> 10, higher than Q's 05) stand between Q and QR; QR=ABCD.
      *> Refused: g2-redefines-cd-lower-level-between.
      *> SR6 "data-name-2 may be subordinate to an item whose data
      *> description entry contains an OCCURS clause ... shall not be
      *> subscripted": T2 REDEFINES T1 under OCCURS 2, unsubscripted; each
      *> occurrence redefines its own T1: T2(1)T2(2)=7856. Refused:
      *> g2-redefines-cd-target-occurs, -subscripted, -odo.
      *> SR7 "Data-name-2 shall not be qualified. NOTE If data-name-2 is not
      *> unique, no ambiguity of reference exists because of the required
      *> placement": A is defined in GA and in GB; AR and BR name A bare
      *> and each binds the A just above it: AR=12 BR=34. Refused:
      *> g2-redefines-cd-qualified.
      *> SR8 "Multiple redefinitions of the same storage area shall each
      *> specify as data-name-2 the data-name of the entry that originally
      *> defined the area": C1R, C1S and the unnamed entry all name C1.
      *> ADD 1 TO C1R over "0012" leaves C1=0013 and C1S2=13. Refused:
      *> g2-redefines-cd-of-redefinition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RDF20.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 C1  PIC X(4).
           05 C1R REDEFINES C1 PIC 9(4).
           05 C1S REDEFINES C1.
              10 C1S1 PIC XX.
              10 C1S2 PIC XX.
           05 REDEFINES C1 PIC X(4).
           05 Q.
              10 Q1 PIC XX.
              10 Q2 PIC XX.
           05 QR REDEFINES Q PIC X(4).
           05 T OCCURS 2.
              10 T1 PIC XX.
              10 T2 REDEFINES T1 PIC 99.
           05 GA.
              10 A  PIC XX.
              10 AR REDEFINES A PIC 99.
           05 GB.
              10 A  PIC XX.
              10 BR REDEFINES A PIC 99.
       PROCEDURE DIVISION.
           MOVE "0012" TO C1
           ADD 1 TO C1R
           DISPLAY "C1=" C1
           DISPLAY "C1S2=" C1S2
           MOVE "AB" TO Q1
           MOVE "CD" TO Q2
           DISPLAY "QR=" QR
           MOVE "78" TO T1(1)
           MOVE "56" TO T1(2)
           DISPLAY "T2=" T2(1) T2(2)
           MOVE "12" TO A OF GA
           MOVE "34" TO A OF GB
           DISPLAY "AR=" AR " BR=" BR
           STOP RUN.
