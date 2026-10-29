*>--- conformance-case
*> id: pb2101_locate_named_file_beside_a_copy
*> rules: [DOC-A.1-14]
*> accept: [2002, 2014, 2023]
*> files: [pb2101_pay.cbl, pb2101_other.cbl]
*> build: ["pb2101_pay.cbl > P2101PAY", "pb2101_other.cbl > P2101PAY - Copy", "{self}"]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.4.4 GR3 b) and Annex A.1 item 14 (docs/CONFORMANCE.md
      *> DOC-A.1-14, DESIGN-external-repository §7.4, kb/Work PB2101): the
      *> locator reads the file NAMED after the program first, and when that
      *> file carries the program nothing else is read.
      *>   cite.py --check 14.9.4.4 "If the program cannot be located or
      *>     identifier-1 references a zero-length item, the
      *>     EC-PROGRAM-NOT-FOUND exception condition is set to exist."
      *>     -> OK 3)
      *> P2101PAY.dll carries the program P2101PAY, and "P2101PAY - Copy.dll"
      *> (a copy a user made) carries a DIFFERENT build of the same name; a
      *> scan of the directory would find the program in both. The named
      *> file answers, so the program is located, and it is the named
      *> file's.
      IDENTIFICATION DIVISION.
      PROGRAM-ID. P2101C.
      PROCEDURE DIVISION.
      MAIN-PARA.
          CALL "P2101PAY"
              ON EXCEPTION DISPLAY "NOT LOCATED"
          END-CALL
          STOP RUN.
      END PROGRAM P2101C.
