      *>--- conformance-case
      *> id: pb1601-copy-suppress-below-2002
      *> rules: [FMT-7.2.3.2]
      *> files: [PB1601SP.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> The SUPPRESS [ PRINTING ] phrase of the COPY statement (7.2.3.2;
      *> 7.2.3.4 4): "If the SUPPRESS phrase is specified, library text
      *> incorporated as a result of COPY statement processing is not
      *> listed") is a COBOL-2002 introduction (constructs row
      *> copy-suppress-2002, VCR row 7.44; kb/Work PB1601): ANSI
      *> X3.23-1985's COPY has no such phrase. The statement below draws
      *> COBOLNET0900 at --std 85; the positive twin at 2002 is
      *> pb1354_copy_header_2002.
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB1601SP.
000300 DATA DIVISION.
000400 WORKING-STORAGE SECTION.
000500 COPY PB1601SP SUPPRESS PRINTING.
000600 PROCEDURE DIVISION.
000700     DISPLAY W-SP.
000800     STOP RUN.
