*>--- conformance-case
*> id: pb2101_locate_program_not_named_after_its_file
*> rules: [DOC-A.1-14]
*> accept: [2002, 2014, 2023]
*> files: [pb2101_callee_in_library.cbl]
*> build: ["pb2101_callee_in_library.cbl > P2101LIB", "{self}"]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.4.4 GR3 b) and Annex A.1 item 14: a CALL's program is LOCATED
      *> when it is registered in the run unit, and HOW a separately compiled
      *> program reaches the run unit is the implementor's (docs/CONFORMANCE.md
      *> DOC-A.1-14, DESIGN-external-repository §7.4, §11.2, kb/Work PB2101).
      *>   cite.py --check 14.9.4.4 "If the program cannot be located or
      *>     identifier-1 references a zero-length item, the
      *>     EC-PROGRAM-NOT-FOUND exception condition is set to exist."
      *>     -> OK 3)
      *>   cite.py --check 14.6.1 "A run unit contains one or more runtime
      *>     modules" -> OK
      *> WiseOwl COBOL locates a sibling module by the program's name FIRST
      *> (the file named after it), then by the index every module carries
      *> in its repository record, so a program whose assembly is not named
      *> after it is found too. Here the callee P2101CALLEE is compiled into
      *> P2101LIB.dll, beside the caller: no file is named P2101CALLEE.dll,
      *> and the CALL still locates it (GR3 b is not raised) and runs it.
      IDENTIFICATION DIVISION.
      PROGRAM-ID. P2101A.
      PROCEDURE DIVISION.
      MAIN-PARA.
          DISPLAY "BEFORE"
          CALL "P2101CALLEE"
              ON EXCEPTION DISPLAY "NOT FOUND"
              NOT ON EXCEPTION DISPLAY "LOCATED"
          END-CALL
          DISPLAY "AFTER"
          STOP RUN.
      END PROGRAM P2101A.
