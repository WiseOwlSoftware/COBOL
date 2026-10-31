## Entry 2881 — 2026-10-31 14:09 PDT — kb/Work: the backend seam becomes the PB2293 campaign, owner priority; file PB4265 and PB4266

The owner asked what keeping the Roslyn code generator replaceable requires, then removed any C/C++ backend
requirement (the requirement is a direct-IL backend only, DESIGN.md §1.1) and directed: "elevate the priority to
eliminate the seam leaks asap". The register has no priority field, so the elevation uses the campaign mechanism:
PB2293 (the backend input carries the binder and the parse tree) is now a cluster lead, and every open note on its
blocked_by closure (27, including the external-repository slices PB2103 and PB2104 and the DataBinder and
ReferenceResolver extract chains) names it, with the owner's words in its body. `plan_wave.py --cluster PB2293` plans
it; PB2103, PB2417 and PB4265 are ready.

Two new notes: PB4265, the backend-abstraction design still forbids target identifiers on model nodes while the
owner-approved external-repository design made `CsNames.Allocate` the one bind-time allocator of the CLR names both
backends must emit; and PB4266, the §6 backend-contract test does not exist and `ICodeGenBackend` has one
implementation. PB2293 also records the front-end review's AST verdict reconsidered against the seam: no parser-side
AST, because the bound tree is the AST for every backend, and finishing it (no parse contexts) is this campaign.

