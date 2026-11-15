*>--- conformance-case
*> id: pb5465_invoke_by_phrase_not_transitive
*> rules: [GR-14.9.23.4-6, SR-14.9.23.3-14, FMT-14.9.23.2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> PB5465 (re-verdicted: not a defect) - an INVOKE BY phrase governs
      *>   the ONE argument it is written on; it is NOT transitive across
      *>   the keyword-less arguments that follow it, unlike CALL Format 1.
      *> 14.9.23.2: the printed figure (PDF page 681, folio 651) puts the
      *>   ellipsis outside the BY REFERENCE / BY CONTENT / BY VALUE brace
      *>   and inside the USING bracket, with no inner ellipsis: each
      *>   repetition is "[BY phrase] operand".
      *>   cite.py --check "§2023:14.9.23.2" "the ellipsis is INSIDE those
      *>   brackets" -> OK
      *> 14.9.23.3 SR14 "If an argument is specified without any BY phrase,
      *>   BY REFERENCE is implied for that argument when" (cite.py OK) and
      *>   14.9.23.4 GR6 "If an argument is specified without any of the
      *>   keywords BY REFERENCE, BY CONTENT, or BY VALUE, the manner used
      *>   for passing that argument is determined as follows" (cite.py OK):
      *>   GR6 a) 1. a keyword-less identifier meeting SR9/SR10 is passed
      *>   BY REFERENCE to a BY REFERENCE formal; a) 2. a keyword-less
      *>   literal is passed BY CONTENT; b) a keyword-less argument to a
      *>   BY VALUE formal is passed BY VALUE.
      *>   CALL's carry-over, 14.9.4.4 GR5 "Both the BY CONTENT and BY
      *>   REFERENCE phrases are transitive across the parameters that
      *>   follow them" (cite.py OK), is a FORMAT 1 rule; INVOKE has no
      *>   such rule in 2002, 2014 or 2023.
      *> Derivation (G = "ABC123", N = -42; BUMP sets LA to "XYZ" and adds
      *>   1 to LN; SHOWV displays its BY VALUE formal):
      *>   1. USING BY CONTENT G N : G by content, N by reference (GR6 a) 1.)
      *>      -> after: G=ABC123 N=-041
      *>   2. USING BY CONTENT G BY CONTENT N : both by content
      *>      -> after: G=ABC123 N=-041
      *>   3. USING G BY CONTENT N : G by reference, N by content
      *>      -> after: G=XYZ123 N=-041
      *>   4. USING BY REFERENCE G 7 : the literal is keyword-less, so GR6
      *>      a) 2. passes it BY CONTENT; a transitive BY REFERENCE would
      *>      make a literal identifier-3 (SR9 refuses it) -> method shows
      *>      +007 then +008 in its own copy
      *>   5. USING BY CONTENT G V to SHOWV (formal 2 BY VALUE): GR6 b)
      *>      passes V BY VALUE; a transitive BY CONTENT would break SR5 a)
      *>      -> V=+005
      *> Why each leg can fail: a carried BY CONTENT leaves N at -042 in
      *>   leg 1; a carried BY REFERENCE refuses legs 4; a carried BY
      *>   CONTENT refuses leg 5.
       IDENTIFICATION DIVISION.
       CLASS-ID. PB5465C.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB5465C.
       FACTORY.
       PROCEDURE DIVISION.
       IDENTIFICATION DIVISION.
       METHOD-ID. BUMP.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LG.
          05 LA PIC X(3).
          05 LB PIC 9(3).
       01 LN PIC S9(3).
       PROCEDURE DIVISION USING LG LN.
           MOVE "XYZ" TO LA
           DISPLAY "IN LN=" LN
           COMPUTE LN = LN + 1
           DISPLAY "IN LN=" LN
           GOBACK.
       END METHOD BUMP.
       IDENTIFICATION DIVISION.
       METHOD-ID. SHOWV.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LG.
          05 LA PIC X(3).
          05 LB PIC 9(3).
       01 LV PIC S9(3) BINARY.
       PROCEDURE DIVISION USING BY REFERENCE LG BY VALUE LV.
           DISPLAY "V=" LV
           GOBACK.
       END METHOD SHOWV.
       END FACTORY.
       END CLASS PB5465C.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5465M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB5465C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 GA PIC X(3) VALUE "ABC".
          05 GB PIC 9(3) VALUE 123.
       01 N PIC S9(3) VALUE -42.
       01 V PIC S9(3) BINARY VALUE 5.
       PROCEDURE DIVISION.
       P0.
           INVOKE PB5465C "BUMP" USING BY CONTENT G N
           DISPLAY "1 G=" G " N=" N
           INVOKE PB5465C "BUMP" USING BY CONTENT G BY CONTENT N
           DISPLAY "2 G=" G " N=" N
           INVOKE PB5465C "BUMP" USING G BY CONTENT N
           DISPLAY "3 G=" G " N=" N
           MOVE "ABC" TO GA
           INVOKE PB5465C "BUMP" USING BY REFERENCE G 7
           DISPLAY "4 G=" G
           INVOKE PB5465C "SHOWV" USING BY CONTENT G V
           STOP RUN.
       END PROGRAM PB5465M.
