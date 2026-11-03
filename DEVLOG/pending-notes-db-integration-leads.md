## kb/Work PB4462-PB4467: the DataBinder integration's leads, filed

PB4462 records that semgrep parses most of DataBinder.cs, DataBinder.Reports.cs and DataBinder.Constants.cs only partially and that `verify.py` drops the `errors[]`, so the invariant rules are blind there; it explains the 225 to 229 and 225 to 226 baseline moves of the PB2352 and PB2355 branches. PB175 had proposed the `verify.py` failure for one small span; PB4462 owns it now at thousands of lines.

PB4463 records the 19 raw `Edition.Error("COBOLnnnn")` codes with no catalog descriptor and the three unrelated rules that share COBOL1512, with the sites, the fix shape (a descriptor per rule, a drift test) and the link to PB4462.

PB4464 records the suspected, unprobed wrong answer that a method's own OPTIONS FLOAT-BINARY does not reach its LOCAL-STORAGE endianness default, which `DataBinder.BindEntry` reads from the class half's model, with the probe to run.

The lead about the roughly 45 private-to-internal widenings on DataBinder and ConstantEntryBinder is appended to PB2361 (step 11), the step that gives each member its final owner; PB4465 is left unused. PB4466 files the shared-scratchpad name collision between concurrent agents (a related mechanism to PB3702), and PB4467 files that construct citation strings name C# members and reach the user's diagnostic through ConstructRegistry.
