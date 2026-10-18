*>--- conformance-case
*> id: pb1136-invoke-null-by-value-alphanumeric
*> rules: [SR-14.9.23.3-1, SR-14.9.23.3-15]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.23.3 SR15: "If identifier-5 or its corresponding formal parameter is specified with the BY
      *> VALUE phrase, identifier-5 shall be of class message-tag, numeric, object or pointer." Through the
      *> predefined NULL no method, and so no formal parameter, exists, but the rule reads the operand alone:
      *> BY VALUE X (class alphanumeric) is refused through NULL exactly as through a typed receiver.
      *> kb/Work PB1136.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1136NV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X(3) VALUE "ABC".
       PROCEDURE DIVISION.
       MAIN.
           INVOKE NULL "PING" USING BY VALUE X.
           STOP RUN.
       END PROGRAM PB1136NV.
