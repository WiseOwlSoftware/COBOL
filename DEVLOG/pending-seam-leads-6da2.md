## kb/Work: file PB4320 and PB4321 (backend seam leaks L7, L8); answer PB4265's review leads

The PB4265 implementer found two more backend seam leaks and recorded them as open rows L7 and L8 of
DESIGN-backend-abstraction §1.3; they are now notes in the PB2293 campaign. PB4320: the binder composes C#
member-access text from CLR names (`__lnk{Uid}.Value`, `__outer.`, anchor-plus-name bridges, dotted sort-key paths).
PB4321: C# method-local names sit on model and bound nodes (`PositionLocal`, `LocalVector`, `__kv{Uid}`). The same
change answers the four low leads of PB4265's branch review: two doc comments named types that do not exist
(`ClrNames.Allocate`, `RepositoryMethod.ClrName`), REDEFINES_DESIGN still called the backing names rendering policy,
and the plan's appendix listed `BoundMethod.CsName` as a C#-path leak after the rename made it a CLR name.
