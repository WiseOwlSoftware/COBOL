*>--- conformance-case
*> id: l1_encoding_marked_malformed_is_refused
*> rules: [DOC-A.1-25]
*> files: [L1BADMRK.cpy]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §8.1.3.2 GR1 (Annex A.1 item 25) - the counterpart of
      *> l1_encoding_unmarked_non_utf8_is_latin1.
      *>   cite.py --check 8.1.3.2 "The implementor shall define a
      *>     mapping of each basic letter, basic digit, basic special"
      *>     -> OK 8.1.3.2 1)
      *> THE DETERMINATION PINNED (docs/CONFORMANCE.md DOC-A.1-25): a
      *> file that BEGINS WITH a byte-order mark is read in exactly the
      *> encoding the mark names, and when its content is not well-formed
      *> in that encoding the file is refused (COBOL5434, naming the file
      *> and the byte offset) - the mark states the encoding, so there is
      *> no other reading to fall back to, and no byte is replaced.
      *> THIS FILE is well-formed. Its library text L1BADMRK.cpy begins
      *> with the UTF-8 mark EF BB BF and holds a lone E9 byte inside a
      *> literal, which UTF-8 does not define; the COPY is therefore a
      *> compile-time error at every edition. A reading that kept the
      *> byte (as the unmarked sibling does) or replaced it would
      *> compile this program, so each can fail this test.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1BADM01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       COPY L1BADMRK.
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
