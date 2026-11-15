- **D-DL3 — the DYNAMIC LENGTH STRUCTURE clause: the PREFIXED length field bounds the item, the layout is the
  item's form in the record a file holds, and no physical-structure-name is supported** (kb/Work PB829, PB1094;
  rows `FMT-12.3.7.2`, `GR-12.3.7.4-19`). §2023:12.3.7.4 GR18
  sizes the PREFIXED length field ("If SIGNED is specified, the length field is a signed binary field"; SHORT the
  shorter one) and GR19 puts a binary-zero delimiter after DELIMITED data. WiseOwl COBOL's length fields are exactly
  those binary fields (32-bit, or 16-bit with SHORT), so §2023:8.5.1.10.1's candidate "the largest integer that can be
  stored in an item of the usage specified in the PREFIXED phrase" is GR18's own table value — 65535 for SHORT
  PREFIXED, 32767 for SIGNED SHORT PREFIXED; 4294967295 and 2147483647 exceed the implementor maximum (item 62)
  and bound nothing. "The maximum length associated with dynamic-length-structure-name-1" (§2023:13.18.19.3 SR4) is
  that capacity bounded by the implementor maximum (a DELIMITED-only structure: the implementor maximum); a larger
  LIMIT is COBOL2258. **The LAYOUT is realized in the record image** (kb/Work PB1094, which refuted this
  determination's earlier premise that "no operation gives the item an addressable byte image": a WRITE does).
  §2023:8.5.1.10.3 lets a dynamic-length item sit "within the record they are subordinate to", which is what a record on
  a medium is, and §2023:12.3.7.4 GR18 / GR19 then say what the item looks like there: *"data described with
  dynamic-length-structure-name-1 is prefixed by a length field"* and *"a delimiter shall directly follow the data"*.
  So a structured item occupies **`[length field][data][delimiter]`** in a record a file or a sort store holds, the
  length field present for PREFIXED and the delimiter for DELIMITED, in that order whichever order the clause wrote
  them (a structure with both has both). **Representation** (the standard leaves it implementor-defined, GR18's
  "capable of supporting" and GR19's "data of the length of an alphanumeric character"): the length field is the
  item's current length in CHARACTER POSITIONS as a binary integer **most significant byte first** (the
  implementation's binary byte order, A.1 item 205), **four characters** for PREFIXED / SIGNED PREFIXED and **two**
  for SHORT PREFIXED / SIGNED SHORT PREFIXED (SIGNED changes the range, never the bytes: a length is never
  negative), one image character per byte; the delimiter is **one character position of zero bits** — one
  character for an alphanumeric item, **two** for a national item, whether it is a member of a group or is itself
  the record (its image is its UTF-16BE byte pairs, D-N1: the image of a group is its storage, A.1 items 57 and 63;
  kb/Work PB3441). **The program never sees these characters:** §2023:8.5.1.11.2 makes a
  variable-length item behave "as though it were in fact contiguous with its neighbors whenever a procedural
  operation is applied to a group containing it", so a MOVE, a comparison, DISPLAY or READ … INTO of the record is
  over the data alone. A record has TWO FORMS: the *contiguous* image (the data alone, what the procedural
  view is) and the *medium* image (what the file holds: each structured member framed). They are produced and
  consumed in ONE place, `CobolContiguousLayout` (`MediumImage` / `MediumExtents` on a WRITE, REWRITE or RELEASE;
  `Decompose` on a READ or RETURN), over the per-member layout `CobolDynStructure`, and the frame's extent table
  (D-FRA (v)) keeps describing the stored image, so a key behind a structured member, the sort store and the fixed
  form (D-FRA (vi)) need no second rule. **Sizes include the structure**: the record on the medium physically
  contains the length field and delimiter, so §2023:13.18.43.4 GR8's size of a record description, GR9 / GR10's
  minimum and maximum, every RECORD clause integer a syntax rule compares against them (§2023:13.18.43.3 SR3 / SR4), the
  bytes a RECORD VARYING … DEPENDING ON item holds (GR13 a), GR15) and the fixed length a Format 1 file's records
  have (GR6 — each member then at its MAXIMUM extent, length field + maximum size + delimiter, blank padded)
  all count them; `FileModel.MaxDynamicExtent` / `MinExtent` are the one place. **Reading**: the frame's extent table
  says where each member's extent ends and the structure says what is inside it (length field first, else up to the
  delimiter; an extent that ends in its delimiter is exactly the data before it, so data that itself holds a binary
  zero survives); a record that carries NO table (a file another program wrote, a Format 1 file) is taken
  apart by the structures themselves, and a member with no structure takes what is left beyond the fixed material and
  the minimum (length field + delimiter) of the structured members after it — so with NO table a plain member
  followed by a structured one is read as the take step reads it (D-FRA), and data that holds the delimiter of a
  DELIMITED-only member ends there. A length field larger than the record holds is clamped to what is there (a record
  that ends inside its own structure gives what is left) and no condition is set: §2023:14.6.13.2 rule 5's
  EC-DATA-INCOMPATIBLE speaks of a dynamic-length item's own internal format, which here is a native string and is
  always well formed, and the standard defines no status for a medium record that disagrees with its structure.
  **LINE SEQUENTIAL**: the characters below U+0020 are outside a line sequential
  file's character set (A.1 item 115), and a length field (whose first byte is zero) and a delimiter are such
  characters, so a WRITE of a record that holds a structured member to a LINE SEQUENTIAL file is the status '71' of
  §2023:14.9.51.4 GR23, exactly as for any record holding a binary field — an unstructured dynamic-length member
  (§2023:13.18.19.3 SR3) is unaffected. **Rejected readings:** keeping the structure off the medium because the item is
  a native `string` outside the record (§2023:8.5.1.10.3's other option — it makes GR18 and GR19 unobservable, and the
  clause's purpose is the layout a second program reading the file sees); putting the structure in the
  contiguous image (it would break §2023:8.5.1.11.2's procedural view, which is why the two forms are separate);
  counting the structure bytes against nothing (a record of maximum content then exceeded the RECORD clause's own
  bound and the WRITE failed '44'). §2023:12.3.7.3 SR32 ("The implementor shall
  specify the names supported for physical-structure-name-1") is answered with the EMPTY set: every layout the
  carrier could honour is already expressible with PREFIXED / DELIMITED, so a physical-structure-name is refused by
  name (COBOL2257) rather than silently mapped to one of them. Witnessed by `conformance:cases/12.3.7-SPECIAL-NAMES/pb829_dynamic_length_structure`
  (a 70000-character sender stores 65535 / 32767 / 70000 / 70000 / LIMIT 10), `conformance:cases/13.18.19-DYNAMIC-LENGTH/pb829-dynamic-length-structure-limit`
  and, for the layout in the record, `conformance:cases/12.3.7-SPECIAL-NAMES/pb1094_dynamic_length_structure_record_image` (every
  PREFIXED / SIGNED / SHORT / DELIMITED form and a plain member, read back as the characters the file holds),
  `…_elementary_record` (a record that is the item: alphanumeric and national), `…_organizations` (indexed
  variable and Format 1, relative, SORT USING / GIVING and procedures) and `…_record_size` (the Format 1 fixed form
  and RECORD VARYING … DEPENDING ON). **Two DYNAMIC LENGTH clauses name the same structure when the structures have
  the same DEFINITION** — the PREFIXED phrase, DELIMITED and the physical structure, never the
  dynamic-length-structure-name (kb/Work PB2103; DESIGN-external-repository §6.4). The rules that ask for "the same …
  DYNAMIC LENGTH … clauses" (§2023:8.5.3.1, §2023:9.3.6 3 e), §2023:14.8.2.3.2 rule 2, §2023:14.8.3.3) compare two descriptions that may
  have been compiled in two source elements, and a structure-name is a word of its own element's SPECIAL-NAMES
  paragraph: two separately compiled programs name one layout two ways, and the layout is what the clause makes the
  item. `DynamicLengthStructure.DefinitionKey` is the one key every such relation compares. Witnessed by
  `conformance:wiseowl/cases/14.8.2.3.2-Elementary-items-passed-by-reference/pb2103_structure_compared_by_definition`
  (one layout under two names conforms; one name over two layouts does not).
