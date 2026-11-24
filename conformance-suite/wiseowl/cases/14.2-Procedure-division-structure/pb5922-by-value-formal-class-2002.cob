      *>--- conformance-case
      *> id: pb5922-by-value-formal-class-2002
      *> rules: [SR-14.2.2-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5922, review round 1 (R1-1). A BY VALUE formal
      *> parameter shall be of class numeric, object, or pointer at
      *> --std 2002 (§2002:14.1.2 SR2); class message-tag joins the
      *> list at 2023 only. The message states the 2002 rule.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5922BV.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC X(4).
       PROCEDURE DIVISION USING BY VALUE L.
           GOBACK.
       END PROGRAM PB5922BV.
