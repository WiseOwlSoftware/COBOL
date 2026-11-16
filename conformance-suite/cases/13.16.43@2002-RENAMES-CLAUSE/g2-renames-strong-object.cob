      *>--- conformance-case
      *> id: g2-renames-strong-object
      *> rules: [2002-SR-13.16.43.2-7]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.43.2 SR7 - "None of the items within the range,
      *> including data-name-2 and data-name-3, if specified, shall be of
      *> class object or pointer". At 2002 a USAGE OBJECT REFERENCE item may
      *> be "an elementary data item with level 1 or subordinate to a type
      *> declaration that includes the STRONG phrase" (§2002:13.16.58.2
      *> SR13), and a level-1 item cannot be a RENAMES operand
      *> (§2002:13.16.43.2 SR5), so the only legal place for an object
      *> reference inside a range is a strongly-typed group: OB sits between
      *> S1 and S2 of R2 TYPE ST. The 66 entry is refused at its line (the
      *> object arm and the strong arm of SR7 both apply; no legal 2002
      *> program separates them). The pointer twin is
      *> g2-renames-strong-pointer. Fails if the entry is accepted.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2REN29.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  ST TYPEDEF STRONG.
           05 S1 PIC X(8).
           05 OB USAGE OBJECT REFERENCE.
           05 S2 PIC X(8).
       01  R2 TYPE ST.
       66  X1 RENAMES S1 OF R2 THRU S2 OF R2.
       PROCEDURE DIVISION.
           DISPLAY "RAN"
           STOP RUN.
