      *>--- conformance-case
      *> id: pb1906-typedef-member-without-group
      *> rules: [GR-13.18.58.4-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §13.18.58.4 GR1 - "The data-names of the items described
      *> in these subordinate entries may be referenced only as
      *> subordinate items of groups defined using the type-name ...
      *> If there is no such group, any reference to a name that is the
      *> same as a data-name in the subordinate entry is a reference to
      *> another resource, if there is another resource with that name,
      *> or is an invalid reference."  No entry here is described with
      *> TYPE T and nothing else is named M, so MOVE "A" TO M is an
      *> invalid reference (§8.4.2.1). Expected: COBOL1639 naming M as a
      *> member of the type declaration T - never "not defined", since
      *> the TYPEDEF entry declares it (kb/Work PB1906).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1906TM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T IS TYPEDEF.
          05 M PIC X(5).
       01 W PIC X(5).
       PROCEDURE DIVISION.
           MOVE "A" TO W
           MOVE "A" TO M
           DISPLAY W
           STOP RUN.
