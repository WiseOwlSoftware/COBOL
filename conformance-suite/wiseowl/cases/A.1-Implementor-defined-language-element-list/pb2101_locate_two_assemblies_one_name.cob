*>--- conformance-case
*> id: pb2101_locate_two_assemblies_one_name
*> rules: [DOC-A.1-14]
*> accept: [2002, 2014, 2023]
*> files: [pb2101_dup_first.cbl, pb2101_dup_second.cbl]
*> build: ["pb2101_dup_first.cbl > P2101LIBX", "pb2101_dup_second.cbl > P2101LIBY", "{self}"]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §8.3.2.2 2): one externalized name identifies ONE instance in a
      *> run unit, and §14.9.4.4 GR3 b): a program that "cannot be located"
      *> is EC-PROGRAM-NOT-FOUND (docs/CONFORMANCE.md DOC-A.1-14,
      *> DESIGN-external-repository §7.4, §11.3, kb/Work PB2101).
      *>   cite.py --check 8.3.2.2 "when two or more source elements identify
      *>     something with the same externalized name, they refer to the
      *>     same instance" -> OK 2)
      *>   cite.py --check 14.9.4.4 "If the program cannot be located or
      *>     identifier-1 references a zero-length item, the
      *>     EC-PROGRAM-NOT-FOUND exception condition is set to exist."
      *>     -> OK 3)
      *> Two DIFFERENT compiled modules, P2101LIBX.dll and P2101LIBY.dll,
      *> each define the program P2101DUP, and neither is named after it, so
      *> the locator finds both through their indexes: the name is
      *> ambiguous, no module is loaded, and the program is not located.
      *> (Two copies of ONE build are one entry, not a collision.)
      IDENTIFICATION DIVISION.
      PROGRAM-ID. P2101B.
      PROCEDURE DIVISION.
      MAIN-PARA.
          DISPLAY "BEFORE"
          CALL "P2101DUP"
              ON EXCEPTION DISPLAY "NOT LOCATED"
              NOT ON EXCEPTION DISPLAY "LOCATED"
          END-CALL
          DISPLAY "AFTER"
          STOP RUN.
      END PROGRAM P2101B.
