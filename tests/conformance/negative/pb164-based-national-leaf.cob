      *> reject-at: 2023
      *> kb/Work PB164 (the Step D arm-1 dissolution): COBOLNET1695's
      *> WITNESS, re-pinned each time the residue boundary narrowed.
      *> Its FIRST witness was a BASED record with a COMP leaf; the
      *> byte-form waves gave every NUMERIC usage - DISPLAY, BINARY,
      *> PACKED-DECIMAL, COMP-5, the IEEE float family and USAGE INDEX -
      *> a pinned byte form, so a COMP leaf rides the shared cell and
      *> the old fixture became a POSITIVE case (2023/stepd_based_comp_leaf).
      *> Its SECOND witness was a NATIONAL leaf. kb/Work PB231 has now
      *> discharged that one too (RESIDUE-11): the byte-addressed class
      *> area lays a national position out as TWO bytes, which is what
      *> 13.18.44.4 GR1 ("an area sufficient to contain the number of
      *> bits required") and 14.9.3.4 GR3 ("the number of bytes required
      *> to hold an item as described by data-name-1") both ask for, and
      *> 13.18.60.4 GR8 leaves that size to the implementor. The window
      *> transcodes the UTF-16BE pair. The positive witnesses are
      *> 2002/pb231_based_national_leaf and 2002/pb231_national_byte_window.
      *> ⛔ WHAT IS LEFT, AND IT IS THE WHOLE OF THIS FIXTURE NOW: a
      *> POINTER-CLASS leaf. It is not a byte sequence at all - a
      *> managed pointer cell has no byte-window overlay - so
      *> 14.9.3.4 GR9's "data items of class object or class pointer in
      *> the allocated storage are initialized to null" has nowhere to
      *> write. The shape below is one of the TWO 13.18.60.3 SR14 does
      *> NOT bar ("a USAGE clause with the MESSAGE-TAG, OBJECT
      *> REFERENCE, POINTER, FUNCTION-POINTER, or PROGRAM-POINTER
      *> phrase may be specified only for an elementary data item at
      *> level 1 or an elementary data item subordinate to a type
      *> declaration that includes the STRONG phrase"): the level-1
      *> elementary one. Written subordinate to a group it would draw
      *> COBOLNET1724 at the DECLARATION instead and never reach the
      *> byte-window gate, which is why the fixture is spelled this way.
      *> Without this fixture the diagnostic would have NO witness at
      *> all and the residue boundary could move unobserved.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB164N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P USAGE POINTER BASED.
       01 WS-P USAGE POINTER.
       PROCEDURE DIVISION.
       MAIN.
           ALLOCATE P RETURNING WS-P
           FREE WS-P
           STOP RUN.
