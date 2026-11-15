      *>--- conformance-case
      *> id: g2-dde-typedef-level-1
      *> rules: [2002-SR-13.14.2-14]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.14.2 SR14 - "The TYPEDEF clause may be specified only
      *> in a data description entry whose level-number is 1 and for
      *> which the data-name format of the entry-name clause is
      *> specified." The granted form, a level 1 named TYPEDEF, is
      *> written and used by TYPE in working storage and in a record
      *> after a CD. Refusing the type declaration fails the compile;
      *> a wrong type (not PIC X(3)) shows as wrong truncation below.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DDE08.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T3 IS TYPEDEF PIC X(3).
       01  V1 TYPE T3.
       COMMUNICATION SECTION.
       CD  CI FOR INPUT
           SYMBOLIC QUEUE IS Q1.
       01  CREC.
           05 CT TYPE T3.
           05 F PIC X(84).
       PROCEDURE DIVISION.
           MOVE "ABCDE" TO V1
           MOVE "XYZW" TO CT
           DISPLAY "V1=[" V1 "] CT=[" CT "]"
           STOP RUN.
