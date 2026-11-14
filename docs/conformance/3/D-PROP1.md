- **D-PROP1 — an object property that a statement stores only IN PART: the GET supplies the kept positions, and a class
  WITH NO GET still compiles (kb/Work PB2618).** §2023:14.9.43.4 GR7: "All other portions of the data item referenced by
  identifier-3 will contain data that was present before this execution of the STRING statement"; §2023:8.4.3.3.4 GR5 makes a
  reference-modified receiver "a unique data item that is a subset of the data item referenced by identifier-1", so a
  store into it leaves the rest of the item as it was. An object property stands in for its data item through a
  conceptual temporary (§2023:8.4.3.9.4 GR2, GR3), and what was "present before" is the property's value. **Determination:**
  a property reference at a position that stores only part of the item (STRING's identifier-3, and any receiver written
  through a reference modifier) runs its GET method first, when the class has one, then the statement, then its SET
  method, so the positions the statement did not reach keep the property's value. The standard does not say whether such
  a position is a SENDING use of the property, which would make §2023:8.4.3.9.3 SR3 demand a get method ("If the object
  property is used as a sending item, a get property method shall exist"); §2023:14.9.43.3 SR10 calls STRING's identifier-3 "the
  receiving operand", and a STRING into a property WITH NO GET was already accepted (kb/Work PB1275). This compiler keeps
  that: the position is a RECEIVING use (SR4 asks for a set method, `conformance:cases/_unassigned/pb2618-string-into-property-no-set`),
  and when the class has no get method the receiving temporary starts as spaces (an alphanumeric property; zeros for a numeric one) at EVERY execution of the statement, reset before it (measured: STRING "Q" gives "Q     ", the second time too; `conformance:cases/_unassigned/pb2618_no_get_part_store_every_execution`), so
  the positions a partial store does not reach are those, not an error and not a stale value. GnuCOBOL has no object
  properties, so no vendor behavior informs the reading. Witnesses:
  `conformance:cases/_unassigned/pb2618_string_into_property_keeps_unreferenced`,
  `conformance:cases/_unassigned/pb2618-string-into-property-no-set`, `conformance:cases/8.4.3.9-Object-property/pb1275_property_set_formats` (the
  WITH NO GET case, filled whole).
