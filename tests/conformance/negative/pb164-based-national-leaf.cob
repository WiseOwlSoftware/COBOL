      *> reject-at: 2023
      *> kb/Work PB164 (the Step D arm-1 dissolution): COBOLNET1695's
      *> WITNESS, re-pinned on the boundary that actually survives.
      *> Its previous witness was a BASED record with a COMP leaf; the
      *> byte-form waves gave every NUMERIC usage - DISPLAY, BINARY,
      *> PACKED-DECIMAL, COMP-5, the IEEE float family and USAGE INDEX -
      *> a pinned byte form, so a COMP leaf now rides the shared cell and
      *> the old fixture became a POSITIVE case (2023/stepd_based_comp_leaf).
      *> What the byte-addressed cell still cannot carry is a NATIONAL
      *> leaf: 13.18.60.4 makes a national position TWO bytes wide over a
      *> byte-addressed cell (RESIDUE-11), so the ALLOCATE/ADDRESS pointer
      *> bridge stays a named, recognized-but-unimplemented residue rather
      *> than inventing a layout. Without this fixture the diagnostic had
      *> NO witness at all and the residue boundary could move unobserved.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB164N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R BASED.
          05 N PIC N(4).
       PROCEDURE DIVISION.
       MAIN.
           ALLOCATE R
           STOP RUN.
