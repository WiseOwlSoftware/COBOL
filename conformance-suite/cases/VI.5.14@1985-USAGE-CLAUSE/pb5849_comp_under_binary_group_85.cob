      *>--- conformance-case
      *> id: pb5849_comp_under_binary_group_85
      *> rules: [1985-SR-VI.5.14.3-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5849 (the ruling: kb/Work/PB5683/
      *> 2026-11-16-ruling-same-usage.md). §1985:VI.5.14.3 SR2: a
      *> usage written for a group may be written for a subordinate,
      *> "but the same usage must be specified". A usage is a manner of
      *> representation (§1985:VI.5.14.4 GR2: "The USAGE clause
      *> specifies the manner in which a data item is represented in
      *> the storage of a computer"), and the determination
      *> (docs/conformance/7/DOC-A.1-208.md) represents COMPUTATIONAL
      *> as BINARY, so E1 under the BINARY group G specifies the same
      *> usage and the program is legal; GnuCOBOL gives both words one
      *> usage. The value moved is 1 and a signed fixed-point item
      *> displays with its sign first (DOC-A.1-56), so the line is
      *> +0001. A compiler that compared the words as written would
      *> refuse the program and print nothing. The control,
      *> pb5849_packed_under_binary_group_refused_85, is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5849U2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G USAGE BINARY.
          05 S.
             10 E1 PIC S9(4) USAGE COMPUTATIONAL.
       PROCEDURE DIVISION.
       P0.
           MOVE 1 TO E1.
           DISPLAY E1.
           STOP RUN.
