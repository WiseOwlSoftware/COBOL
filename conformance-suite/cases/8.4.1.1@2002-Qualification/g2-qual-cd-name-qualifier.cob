      *>--- conformance-case
      *> id: g2-qual-cd-name-qualifier
      *> rules: [2002-SR-8.4.1.1.2-2, 2002-SR-8.4.1.1.2-3]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.4.1.1.1 - "where file-cd-report-qualifier is" IN/OF
      *> file-name-1 | cd-name-1 | report-name-1: at 2002 a cd-name
      *> qualifies the items of the record description entries that
      *> implicitly redefine its record area (§2002:A.1.2.2 SR9).
      *> §2002:8.4.1.1.2 SR2 - "A name may be qualified even though it
      *> does not need qualification; if there is more than one
      *> combination of qualifiers that ensures uniqueness, then any
      *> such set may be used." A1 is not unique (WREC, CDREC); the
      *> sets IN CDOUT, IN CDREC and OF CDREC IN CDOUT each make it
      *> unique and name the same item (Q1-Q3, Q6). B1 is unique, yet
      *> B1 OF CDOUT, B1 IN CDREC and B1 OF CDREC OF CDOUT qualify it
      *> anyway (Q4). A1 OF WREC still names the working-storage A1
      *> (Q5). A condition-name takes the cd-name qualifier too, its
      *> hierarchy that of its conditional
      *> variable (§2002:8.4.1.1.2 SR4): Q7-Q9.
      *> §2002:8.4.1.1.2 SR3 - "The words IN and OF are equivalent":
      *> A1 OF CDREC IN CDOUT and A1 IN CDREC OF CDOUT (Q1, Q2) and the
      *> mixed condition-name qualifiers (Q7, Q8) agree.
      *> Each leg can fail: a qualifier resolved to the wrong A1 prints
      *> WSA, a refused qualifier fails the compile, a MOVE through
      *> B1 OF CDOUT to another item leaves CDREC without BBB (Q6), and
      *> Q9 prints NO only if A1-YES IN CDOUT tests the CD's A1.
      *> The communication facility is obsolete at 2002 (COBOL0903).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2QUAL01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WREC.
           05 A1 PIC X(3) VALUE "WSA".
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 A1 PIC X(3).
              88 A1-YES VALUE "YES".
           05 B1 PIC X(3).
       PROCEDURE DIVISION.
           MOVE "CDA" TO A1 IN CDOUT.
           MOVE "BBB" TO B1 OF CDOUT.
           DISPLAY "Q1 " A1 OF CDREC IN CDOUT.
           DISPLAY "Q2 " A1 IN CDREC OF CDOUT.
           DISPLAY "Q3 " A1 IN CDREC.
           DISPLAY "Q4 " B1 " " B1 IN CDREC " " B1 OF CDREC OF CDOUT.
           DISPLAY "Q5 " A1 OF WREC.
           DISPLAY "Q6 " CDREC.
           SET A1-YES IN CDOUT TO TRUE.
           IF A1-YES OF A1 OF CDOUT
               DISPLAY "Q7 YES " A1 OF CDREC
           ELSE
               DISPLAY "Q7 NO"
           END-IF.
           IF A1-YES IN A1 OF CDREC IN CDOUT
               DISPLAY "Q8 YES"
           ELSE
               DISPLAY "Q8 NO"
           END-IF.
           MOVE "NO " TO A1 OF CDREC.
           IF A1-YES IN CDOUT
               DISPLAY "Q9 YES"
           ELSE
               DISPLAY "Q9 NO"
           END-IF.
           STOP RUN.
