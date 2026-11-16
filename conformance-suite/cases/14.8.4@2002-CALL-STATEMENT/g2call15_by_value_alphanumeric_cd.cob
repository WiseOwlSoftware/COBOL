      *>--- conformance-case
      *> id: g2call15_by_value_alphanumeric_cd
      *> rules: [2002-SR-14.8.4.2-19]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.4.2 SR19 -- "If identifier-4 or its corresponding
      *> formal parameter is specified with a BY VALUE phrase,
      *> identifier-4 shall be of class numeric, object, or pointer."
      *> The prohibition: the STATUS KEY of an input CD is "an
      *> elementary alphanumeric data item of 2 characters"
      *> (§2002:A.1.2.3 GR2), passed with the BY VALUE phrase written
      *> in a format 3 CALL. The formal parameter is numeric, as
      *> §2002:14.1.2 SR2 requires of a BY VALUE formal. Refused at
      *> 2002 at the CALL with the SR19 class diagnostic (the same
      *> argument also fails §2002:14.7.1.2.2's COMPUTE conformance,
      *> which every alphanumeric BY VALUE argument does).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CAL15N.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDIN FOR INPUT
           SYMBOLIC QUEUE IS IN-QUEUE
           SYMBOLIC SOURCE IS IN-SOURCE
           TEXT LENGTH IS IN-LEN
           END KEY IS IN-END
           STATUS KEY IS IN-STATUS
           MESSAGE COUNT IS IN-COUNT.
       PROCEDURE DIVISION.
       M-P.
           MOVE "AB" TO IN-STATUS
           CALL "G2C15T" AS NESTED USING BY VALUE IN-STATUS
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2C15T.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9(2).
       PROCEDURE DIVISION USING BY VALUE L-X.
       T-P.
           GOBACK.
       END PROGRAM G2C15T.
       END PROGRAM G2CAL15N.
