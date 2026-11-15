- **D-USAGE-NATIONAL-CLASS-2002 — at `--std 2002` a numeric or boolean item of USAGE NATIONAL is class and category numeric
  or boolean, never national** (kb/Work PB5625 reading B, adjudicated 2026-11-15; §2002:8.5.2.7 item 3 is a defect in
  the 2002 text, recorded here). §2002:8.5.2: "Each data item and each literal has a class and a category" (one of
  each). §2002:8.5.2.9 item 1 puts in category numeric "An elementary data item described as numeric by its PICTURE
  character-string and not described with a BLANK WHEN ZERO clause", and §2002:8.5.2.4 item 1 does the same for boolean
  ("An elementary data item described as boolean by its PICTURE character-string"); neither depends on usage. Table 3 of
  §2002:8.5.2 gives class numeric to category numeric unconditionally, and where usage does move the class it says so:
  "Numeric-edited (if usage is national)". The USAGE clause is about representation (§2002:13.16.58.3 GR2: "It does not
  affect the use of the data item"), §2002:13.16.58.2 SR11 lets a usage-national item "describes a boolean, national,
  national-edited, numeric, or numeric-edited data item", and §2002:13.16.22.3 GR4 lists "class national" and, as a
  separate entry, "class numeric with usage national". Against all of these, §2002:8.5.2.7 item 3 lists as category
  national "An elementary data item explicitly or implicitly described as usage national", which read literally gives a
  PIC 9 USAGE NATIONAL item two categories. **Determination:** in 2002 a PIC 9 item of USAGE NATIONAL is category and
  class numeric, and a PIC 1 item of USAGE NATIONAL is category and class boolean. §2002:8.5.2.7 item 3 is read as
  covering only an item whose PICTURE leaves it national, where it adds nothing to item 1. Where a rule asks for class
  or category national, such an item does not qualify; where a rule speaks of usage national as a representation
  ("class numeric with usage national"), it does. The refusals of such an item where class national is required
  (COBOL1546, COBOL1627) follow this reading. Each 2002 inventory row it decides closes only on its own witness.
