      *>--- conformance-case
      *> id: pb1908_string_overlap_function
      *> rules: [GR-14.9.43.4-10]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> PB1908 -- STRING overlap: the argument of a FUNCTION
      *> sending operand or delimiter shares storage with the
      *> receiving item (identifier-3).
      *> ISO 14.9.43.4 GR10 / Annex A.2 item 60: the result is
      *> undefined; 4.4 2): a run unit that allows it is conforming.
      *> Documented implementor choice (docs/CONFORMANCE.md D-STR4,
      *> GnuCOBOL 3.2 libcob strings.c: cobc evaluates the function
      *> in the argument list of cob_string_append, when that
      *> transfer begins): a function whose argument can occupy
      *> identifier-3's storage is evaluated, once, when its own
      *> transfer begins, from the storage the earlier transfers
      *> stored into. A function whose arguments are disjoint from
      *> identifier-3 is evaluated with the statement's other
      *> identifiers, before any character moves (ISO 14.6.4 7)).
      *> F1: "xy" fills WS-F(1:2), so FUNCTION UPPER-CASE(WS-F) is
      *>     XYCDEFGH, of which six characters fit (GR8).
      *>     Result xyXYCDEF. Snapshot: xyABCDEF.
      *> F2: "z" fills WS-X(1:1), so the delimiter
      *>     FUNCTION UPPER-CASE(WS-X(1:1)) is "Z", not "Q": the
      *>     text "12Z34Q56" is cut at "Z" and "12" fills 2-3.
      *>     Result z12....... Snapshot: z12Z34....
      *> F3: the function's argument is WS-S, outside the receiver:
      *>     it is the same before and after the transfers.
      *>     Result ab-ABC-...
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1908S5.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-F   PIC X(8) VALUE "abcdefgh".
       01 WS-X   PIC X(10) VALUE "Q.........".
       01 WS-S   PIC X(3) VALUE "abc".
       01 WS-Y   PIC X(10) VALUE "..........".
       PROCEDURE DIVISION.
           STRING "xy" DELIMITED BY SIZE
                  FUNCTION UPPER-CASE(WS-F) DELIMITED BY SIZE
               INTO WS-F
           END-STRING
           DISPLAY "F1 [" WS-F "]"
           STRING "z" DELIMITED BY SIZE
                  "12Z34Q56" DELIMITED BY FUNCTION UPPER-CASE(WS-X(1:1))
               INTO WS-X
           END-STRING
           DISPLAY "F2 [" WS-X "]"
           STRING "ab-" DELIMITED BY SIZE
                  FUNCTION UPPER-CASE(WS-S) DELIMITED BY SIZE
                  "-" DELIMITED BY SIZE
               INTO WS-Y
           END-STRING
           DISPLAY "F3 [" WS-Y "]"
           STOP RUN.
