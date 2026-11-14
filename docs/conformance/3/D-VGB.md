- **D-VGB — a BY REFERENCE formal's FIXED table stored past the capacity of the argument's dynamic-capacity table
  creates the occurrences the callee stored into (kb/Work PB2690).** §2023:14.2.3 GR8: "the activated runtime element operates
  as if the formal parameter occupies the same storage area as the argument". A formal table of a fixed (or DEPENDING)
  occurrence count opposite the argument's dynamic-capacity table is legal when the two correspond and match (§2023:8.5.1.12.1,
  §2023:8.5.1.12.2; §2023:8.5.1.12.3 sentence 3: the dynamic-capacity table "is considered to be the same length as the
  corresponding table"), and the callee may subscript it up to its own count. A store to an occurrence past the
  argument's current capacity is therefore a receiving reference to the dynamic-capacity table whose subscript "exceeds
  the current capacity of the table", and §2023:8.5.1.9.3 says what that does: "a new element is automatically created and the
  capacity of the table is increased to the value given by the subscript" (intermediate occurrences too). Until
  kb/Work PB2690 the store was dropped silently and the argument kept its capacity. The occurrences it creates are
  initialized: §2023:8.5.1.9.5, with INITIALIZED, "any elementary items not referenced as receiving operands in a statement
  that creates new elements in that table are first initialized as though they had been the subject of a statement of the
  form INITIALIZE ... WITH FILLER ALL TO VALUE THEN TO DEFAULT", so the intermediate occurrences and every item of a
  created one the callee did not store hold the element's VALUE, exactly as the same store made in the caller leaves them
  (until the second PB2690 landing they came back space filled). **Determination (the latitude the standard leaves):** a
  copy of the argument crosses the boundary and only the finished view comes back, so (i) the formal VIEWS an occurrence
  the argument lacks, when the table writes INITIALIZED, as the occurrence the table creates, the §2023:8.5.1.9.5 NOTE's
  "functionally identical to one where the intermediate occurrences are physically allocated and, if specified,
  initialized": the INITIALIZED recipe, the seed a receiving subscript past the capacity grows the table with
  (`CobolDynTable.CreatedAs`); without the phrase, whose created contents §2023:8.5.1.9.5 leaves "undefined", the view shows
  the space fill, chosen because no numeric store matches it, so a store of the element's own VALUE (a zero) is still
  seen; and (ii) a store past the capacity is the callee's RECEIVING REFERENCE, recorded where it is made: the formal's
  fresh area records the window of every store through a view of it (`StorageCell.Received`, emitted after each store
  through a LINKAGE or BASED view), from the moment the area is filled (`ManagedPointer.Watch`), and the store back
  (`ManagedPointer.OverStorage` → `CobolVarGroup.Overlay`) raises the capacity to the highest occurrence the callee
  referenced as a receiving item, whatever it stored there: the INITIALIZED default, SPACES, the element's VALUE. A callee
  that only reads such an occurrence creates nothing (§2023:8.5.1.9.3 acts on a receiving reference only). A store that reaches
  the area only through a further activation's boundary copy of the whole group (the copy covers every occurrence, so it
  records none, whatever the group's shape and whether the further activation is a CALL, a typed INVOKE or a universal
  one: `CallEmitter.BoundaryCopyWrite`) is recognised by its effect, an occurrence whose image differs from the
  one the view showed. The creation travels on the argument's
  carrier only for a BY REFERENCE crossing (`CobolVarGroup.Created`, stated by `AsVarImage(__byReference: true)` and
  `StorageCell.VarGroupAt(byReference: true)`): a BY CONTENT formal is a record "allocated by the activating runtime element"
  that does "not occupy the same storage area as the argument" (§2023:14.2.3 GR9), filled as by a MOVE, so past the sending
  capacity its fixed table holds the MOVE's space fill (§2023:14.6.9.2 2)) and nothing returns. The same law holds one level
  down for a dynamic-capacity table nested in an element (a created element is the table's created element, its own inner
  table at its creation, with the formal's element stored over it, whichever of the pair's outer tables is the variable
  one), including a fixed table nested in an occurrence of the formal's OWN dynamic-capacity table, whose stores are
  recorded by their subscript path, and across an INVOKE, the universal lane included
  (`CobolVarGroup.Reshape` / `Overlay`'s `OverlaidTable` / `GrownCount` / `OverlaidOccurrences`, shared by the CALL and
  INVOKE boundaries). A fixed-length ARGUMENT cannot grow (it has no more occurrences). The ISO text decides the arm that
  matters, so no vendor behavior was consulted (CLAUDE.md rule 1). Witnesses:
  `conformance:cases/_unassigned/pb2690_fixed_table_formal_grows_dynamic_argument_call`,
  `conformance:cases/_unassigned/pb2690_fixed_table_formal_grows_vlg_element_table_call`,
  `conformance:cases/_unassigned/pb2690_fixed_table_formal_grows_dynamic_argument_invoke`, `conformance:cases/14.2-Procedure-division-structure/pb965_vlg_into_fixed_invoke`,
  `conformance:cases/_unassigned/pb2690_initialized_created_occurrences_call`, `conformance:cases/_unassigned/pb2690_initialized_created_occurrences_invoke`,
  `conformance:cases/_unassigned/pb2690_store_of_shown_image_grows_capacity_call`,
  `conformance:cases/_unassigned/pb2690_store_of_shown_image_grows_capacity_invoke`,
  `conformance:cases/_unassigned/pb2690-fixed-table-formal-grows-dynamic-argument-below-2014`,
  `conformance:cases/_unassigned/pb2690-initialized-created-occurrences-below-2014`,
  `conformance:cases/_unassigned/pb2690-store-of-shown-image-grows-capacity-below-2014`.
