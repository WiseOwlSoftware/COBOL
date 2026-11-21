# PB5940 - LANDED (wave 20262114 group A): the default listing's directory is the compilation's to make

**Re-probe.** `ListingOutcomeTests.AFailedCompilation_ListsBesideAnAssemblyInADirectoryItNeverMade` (a bind error, `-o new-directory/outcome.dll`
at `--std 85`) fails at the base tree on the assertion that no error starts "cannot write the listing" (the note's message), and passes
with the fix; the assembly is not written and the listing is.

**Fix.** `ListingWriter.Write` creates the directory of the listing's path when `ListingRequest.OutputBase` is null (the DEFAULT place,
the output assembly's path without its extension), inside the existing try, so a directory the host refuses is still one refused-write
error. An explicit `--listing-output` into a missing directory stays a refused write
(`ARefusedListingWrite_IsOneError_AndChangesNoOutcome`, unchanged and green). The note's direction named `CompilerDriver.Compile`; the
default base is decided in `ListingWriter.Write` and nowhere else, so the creation is made where the decision is, with no second copy of
the default-place rule in the driver. `docs/rearchitecture/DESIGN-listing.md` section 4.6 says so.

**Sites:** `src/WiseOwl.Cobol.Compiler/Listing/ListingWriter.cs` (`ListingWriter.Write`),
`tests/WiseOwl.Cobol.Tests.Conformance/Listing/ListingOutcomeTests.cs` (`ListingOutcomeTests.AFailedCompilation_ListsBesideAnAssemblyInADirectoryItNeverMade`).
`CompilerDriver.cs` is not changed.
