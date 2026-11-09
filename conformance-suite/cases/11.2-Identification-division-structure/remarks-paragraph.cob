      *>--- conformance-case
      *> id: remarks-paragraph
      *> rules: [FMT-11.2.1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5026 - the REMARKS paragraph is in NO held edition,
      *> so every edition refuses it.  X3.23-1985 VI.3.2.1 "defines the
      *> order of presentation in the source program", and its general
      *> format (VI.3.2.1.1) prints PROGRAM-ID, then AUTHOR,
      *> INSTALLATION, DATE-WRITTEN, DATE-COMPILED and SECURITY, and no
      *> REMARKS: the CODASYL Journal of Development 1968 had made "The
      *> deletion of NOTE and REMARKS in favor of a general comment
      *> capability for all divisions" (1985 A.2.5 item 8).  ISO 2002
      *> and later print none of the comment paragraphs (2023 11.2.1).
      *> Leg that can fail: --std 85 accepting the paragraph as a
      *> COBOL-74 carryover (it compiled and ran until PB5026), or any
      *> edition reporting a parse error rather than the named removal
      *> (COBOL0902, removed in COBOL-85).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. NEGRM01.
000300 REMARKS. CCVS-STYLE REMARK TEXT.
000400 PROCEDURE DIVISION.
000500 MAIN-PARA.
000600     STOP RUN.
