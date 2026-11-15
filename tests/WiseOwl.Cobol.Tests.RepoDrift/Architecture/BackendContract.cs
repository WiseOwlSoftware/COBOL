// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Tests.RepoDrift.Architecture;

/// <summary>
/// The classification of every text-carrying member of the backend input (DESIGN-backend-abstraction §6;
/// <see cref="BackendContractDriftTests"/>), keyed as <see cref="BackendInput.KeyOf"/> spells it. The NEUTRAL rows are the
/// §6 "curated allow-list" of legitimately neutral strings; the LEAK rows are today's offenders, each tolerated only
/// with the §1.3 row and the kb/Work notes that remove it. The leak rows only ever shrink: a landing that removes a leak
/// deletes its member here, and the drift test refuses a row whose member is gone. This file is the enforcement data of
/// one test, never a work list: the work is the notes the rows name (CLAUDE.md rule 8).
/// </summary>
internal static class BackendContract
{
    public static readonly ContractTable Table = new(
    [
        new(NeutralKind.CobolWord,
        [
            "Bound.BoundAlterEntry.NewName", "Bound.BoundDebugSubject.SubjectName", "Bound.BoundDebugTarget.Name", "Bound.BoundDeclarative.SectionName", "Bound.BoundExceptionMatch.OpenMode",
            "Bound.BoundMethod.CobolName", "Bound.BoundNumLiteral.FoldedFunction", "Bound.BoundNumericLiteral.FoldedFunction",
            "Bound.BoundParagraph.CobolName", "Bound.BoundProgramAddress.Prototype", "Bound.BoundRangeMembership.Alphabet", "Bound.BoundSendToServer.ServerName",
            "Bound.BoundSetSwitches.Switches", "Bound.BoundSwitchCondition.ImplementorName",
            "Bound.InitializeCategories.Spelling()", "Bound.SetCapacityKinds.Text()",
            // intrinsic-function-names as their catalog rows spell them (kb/Work PB4370)
            "Bound.IntrinsicCarrierRules.CrossAligningFunctions", "Bound.IntrinsicCarrierRules.ExactParseFunctions",
            "Bound.IntrinsicCarrierRules.SdidiQuotientFunctions",
            "Model.ApplyCommitModel.Identifiers", "Model.IndexNameRegistry._byName",
            // the file-names visible in a source element, its own and its containers' GLOBAL ones (kb/Work PB2296)
            "Model.FileNameScope._inherited", "Model.FileNameScope._own",
            "Model.ItemCategory.CategoryName()", "Model.ItemCategory.Spell()", "Model.ItemCategory.Sr14PhraseOf()",
            "Model.ItemCategory.Sr4PhraseOf()", "Model.RecordLayout.CategoryOfItem()",
            "Model.StrongTypeModel.DeclaredTypeName()", "Model.StrongTypeModel+TypeRestriction.Name",
            "Model.UsageFamilies.HyphenateEnumName()", "Model.UsageFamilies.UsageWord()",
            "Model.AlternateKeyClause.Name", "Model.AlternateKeyClause.Qualifiers",
            "Model.BoundUnit.ExternalizedName", "Model.BoundUnit.Name",
            "Model.CollatingKeyOperand.Name", "Model.CollatingKeyOperand.Qualifiers",
            "Model.Condition88.Alphabet", "Model.Condition88.Name",
            "Model.CommunicationDescription.Name",
            "Model.DataItem.CobolName", "Model.DataItem.ExternalizedAs", "Model.DataItem.IndexNames",
            "Model.DataItem.RedefinesTargetName", "Model.DataItem.SameAsName", "Model.DataItem.SameAsQualifiers",
            "Model.DataItem.TypeName", "Model.DataItem.TypeRefName", "Model.DataItem.WrittenTypeName",
            "Model.DynamicLengthStructure.Name", "Model.DynamicLengthStructure.PhysicalStructureName",
            "Model.FileModel.AssignUsingName", "Model.FileModel.AssignUsingQualifiers", "Model.FileModel.CobolName",
            "Model.FileModel.FileLevelCollating", "Model.FileModel.FileStatusName", "Model.FileModel.FileStatusQualifiers",
            "Model.FileModel.RecordDelimiter", "Model.FileModel.RecordKeyName", "Model.FileModel.RecordKeyQualifiers",
            // the PADDING CHARACTER clause as written (kb/Work PB5069, PB5070)
            "Model.PaddingCharacterSpec.DataName", "Model.PaddingCharacterSpec.Qualifiers",
            // the RERUN clause's implementor-name-1 as written (kb/Work PB5198)
            "Model.RerunRecordsClause.Device",
            "Model.FileModel.RelativeKeyName", "Model.FileModel.RelativeKeyQualifiers", "Model.FileModel.SelectName",
            "Model.IndexDeclaration.Name", "Model.KeyLevelCollatingClause.Alphabet",
            "Model.KeySourceOperand.Name", "Model.KeySourceOperand.Qualifiers",
            "Model.LinageOperand.DataName", "Model.LinageOperand.Qualifiers", "Model.LocaleSymbol.Name",
            "Model.ObjectRefDescriptor.Name", "Model.OccursKey.Name", "Model.OccursKey.Qualifiers",
            "Model.OccursSpec.CapacityName", "Model.OccursSpec.DependingName", "Model.OccursSpec.DependingQualifiers",
            "Model.OmittedFormalGuard.FormalName", "Model.OoClassUnit.Name",
            "Model.PicInfo.RestrictedPrototypeName", "Model.PicInfo.RestrictedTypeName", "Model.RaisingTarget.Name",
            "Model.RecordKey.RecordKeyName", "Model.RecordKeySource.Name",
            "Model.RenamesInfo.FromName", "Model.RenamesInfo.FromQualifiers",
            "Model.RenamesInfo.ThruName", "Model.RenamesInfo.ThruQualifiers", "Model.ReportClauseName.Name",
            "Model.VaryingRecordInfo.DependingName", "Model.VaryingRecordInfo.DependingQualifiers",
            // a parameterized definition's published skeleton and an expansion's identity: externalized names and words (kb/Work PB2104)
            "Model.ExpansionIdentity.ActualIdentities", "Model.ExpansionIdentity.DefinitionIdentity",
            "Model.PublishedSkeleton.ExternalizedName", "Model.PublishedSkeleton.Name",
            "Model.ResolutionRecord.Identity", "Model.ResolutionRecord.Word", "Model.SkeletonRecord.Formals",
            // each published unit's repository record, written at bind time (kb/Work PB4319): the words the publishing element
            // wrote, the externalized names (identities, ISO §2023:8.13) and the entry convention's name, in the persisted vocabulary
            "Model.RepositoryRecord.Name", "Model.RepositoryRecord.ExternalizedName", "Model.RepositoryRecord.EntryConvention",
            "Model.RepositoryRecord.CobolEntryConvention", "Model.RaisingRecord.Identity", "Model.FormalRecord.Name",
            "Model.ClassRecord.BaseIdentity", "Model.ClassRecord.Implements", "Model.ClassRecord.FactoryImplements",
            "Model.MethodRecord.Name", "Model.MethodRecord.ExternalizedName", "Model.MethodRecord.PropertyName",
            "Model.MethodRecord.EntryConvention", "Model.PropertyRecord.Name", "Model.PropertyRecord.ExternalizedName",
            "Model.PrototypeRecord.ExternalizedName", "Model.StructureRecord.Name", "Model.StructureRecord.PhysicalStructureName",
            "Model.DescriptionRecord.Name", "Model.DescriptionRecord.RedefinesTarget", "Model.DescriptionRecord.TypeName",
            "Model.OccursRecord.Depending", "Model.OccursRecord.OutsideTheRecord", "Model.OccursRecord.Capacity",
            "Model.PicRecord.RestrictedTypeName", "Model.PicRecord.RestrictedPrototypeName", "Model.PicRecord.ObjectRefName",
            "Model.PicRecord.ObjectRefIdentity", "Model.LocaleEditRecord.LocaleName",
            // the OO symbols and a method's name scope, in compiler.model since kb/Work PB2402: the words the source wrote,
            // the externalized names (ISO §2023:8.13) and the rosters and name indexes keyed by them
            "Model.OoClassSymbol.Name", "Model.OoClassSymbol.ExternalizedName", "Model.OoClassSymbol.BaseName",
            "Model.OoClassSymbol.FormalBaseName", "Model.OoClassSymbol.Bases", "Model.OoClassSymbol._methods",
            "Model.OoClassSymbol._factoryMethods", "Model.OoInterfaceSymbol.Name", "Model.OoInterfaceSymbol.ExternalizedName",
            "Model.OoInterfaceSymbol._protos", "Model.OoInterfaceSymbol._conformingChoice", "Model.OoMethodSymbol.Name",
            "Model.OoMethodSymbol.ExternalizedName", "Model.OoMethodSymbol._externalizedName", "Model.OoMethodSymbol.PropertyName",
            "Model.OoMethodSymbol.SourceName", "Model.OoMethodDataScope.ByName", "Model.OoMethodDataScope.Conditions",
            "Model.OoMethodDataScope.CapacityRegisters", "Model.OoMethodDataScope.TypeDecls",
            // the standard class BASE and its two §2023:16.2 method-names, in compiler.model since kb/Work PB2403
            "Model.OoStandardClasses.BaseName", "Model.OoStandardClasses.NewMethodName",
            "Model.OoStandardClasses.FactoryObjectMethodName",
            // the name tables of the frozen data binding a backend reads (DataBindResult, kb/Work PB2361): data-, condition-,
            // capacity-register-, screen-, type- and file-names, and the alphabet-names of the range-collation carriers
            "Model.ForestSlice.ByName", "Model.ForestSlice._byName", "Model.ForestSlice.Conditions", "Model.ForestSlice._conditions",
            "Model.ForestSlice.CapacityRegisters", "Model.ForestSlice._capacityRegisters", "Model.ForestSlice.CapacityRegistersSoFar",
            "Model.ForestSlice.ScreenNames", "Model.ForestSlice._screenNames", "Model.ForestSlice.TypeDecls",
            "Model.ForestSlice._typeDecls", "Model.ForestSlice.TypeDeclsOf()", "Model.FileSlice.FilesByName",
            "Model.FileSlice._filesByName", "Model.DataBindResult.RangeCollations",
            "Model.CommunicationSlice.ByName", "Model.CommunicationSlice._byName",
        ]),
        new(NeutralKind.ClrName,
        [
            // where an expansion's types live and what this module calls them (kb/Work PB2104)
            "Model.ExpansionPlacement.ClrNamespace", "Model.ExpansionPlacement.FactoryTypeName", "Model.ExpansionPlacement.LocalFactoryName",
            "Model.ExpansionPlacement.LocalName", "Model.ExpansionPlacement.TypeName", "Model.ExpansionPlacements.ByName",
            "Model.BoundUnit.ClassName", "Model.DataItem.StructName", "Model.DataItem.ProfileName",
            // the factory type a published class record names, as metadata names it (kb/Work PB4319)
            "Model.ClassRecord.FactoryType",
            "Model.BitLayout.FillerClrNameStem", "Model.ModuleNamespace.RegistrarClass",
            "Model.CsNameScope._claimed", "Model.CsNameScope._reserved", "Model.CsNameScope.EveryScopeReserved",
            "Model.CsNameScope.HostEntryReserved", "Model.CsNameScope.ProgramClassReserved",
            "Model.CsNameScope.RecordStructReserved",
            "Model.CsNames.Allocate()", "Model.CsNames.DeriveImportedTypeNames()", "Model.CsNames.SimpleName()", "Model.CsNames.CSharpNamespace()",
            "Model.CsNames.CSharpReservedKeywords", "Model.CsNames.HostEntryMembers", "Model.CsNames.ImportedNamespaces",
            "Model.CsNames.ImportedTypeNames", "Model.CsNames.ProgramClassMembers", "Model.CsNames.RecordStructMembers",
            "Model.DataItem.Sanitize()", "Model.DataItem.IdentifierCharacters()", "Model.DataItem.WordIdentifier()",
            // the synthesized CLR names of the OO model (NamingConvention, the one naming of every compiler-made member and
            // type) and the names the symbols and a method's formals are emitted under, in compiler.model since kb/Work PB2402
            "Model.NamingConvention.AccessorSuffix()", "Model.NamingConvention.AddressCarrierName()",
            "Model.NamingConvention.AddressCarrierPrefix", "Model.NamingConvention.CapacityRegisterName()",
            "Model.NamingConvention.CapacityRegisterPrefix", "Model.NamingConvention.ClassNamedMethodName()",
            "Model.NamingConvention.ClassNamedMethodPrefix", "Model.NamingConvention.CobolWordDerivedPrefixes",
            "Model.NamingConvention.DebugRegisterName()", "Model.NamingConvention.DebugRegisterPrefix",
            "Model.NamingConvention.FactoryClassName()", "Model.NamingConvention.FactoryClassPrefix",
            "Model.NamingConvention.FormalParameterName()", "Model.NamingConvention.FormalParameterPrefix",
            "Model.NamingConvention.GetAccessorName()", "Model.NamingConvention.ImportedTypeName()",
            "Model.NamingConvention.InstanceFileKeyName()", "Model.NamingConvention.InstanceFileKeyPrefix",
            "Model.NamingConvention.SetAccessorName()", "Model.NamingConvention.SumCounterName()",
            "Model.NamingConvention.SumCounterPrefix", "Model.OoClassSymbol.FactoryOrInstanceClrNames",
            "Model.OoInterfaceSymbol.ImplementedClrNames", "Model.OoMethodBinding.FormalName()", "Model.OoFormal.ParamName",
            "Model.OoFormal.AreaParam", "Model.OoFormal.OmittedFlag", "Model.ImportedFrom.ClrNamespace", "Model.ImportedFrom.AssemblyName",
            "Model.ExpansionIdentity.Homes",
        ]),
        new(NeutralKind.LiteralValue,
        [
            "Bound.BoundAllLiteral.Literal", "Bound.BoundBoolAll.Bits", "Bound.BoundBoolLiteral.Bits",
            "Model.PaddingCharacterSpec.Literal",
            "Bound.BoundCallProgram.LiteralName", "Bound.BoundCancel.Targets", "Bound.BoundInvokeArg.NumericLiteral",
            "Bound.BoundInvokeUniversal.MethodLiteral", "Bound.BoundNumLiteral.Text", "Bound.BoundNumericLiteral.Image",
            "Bound.BoundNumericLiteral.Text", "Bound.BoundProgramAddress.NameLiteral", "Bound.BoundSetEntry.NameLiteral",
            "Bound.BoundSetFunctionAddress.NameLiteral", "Bound.BoundStopLiteral.Text", "Bound.BoundStringLiteral.Value",
            "Bound.BoundUserClassCondition.Members", "Bound.InitializeStoreValue.Raw",
            "Model.Condition88.FalseValue", "Model.Condition88.Values", "Model.CurrencyDefinition.Text",
            "Model.DataItem.PictureText", "Model.DataItem.RawValue", "Model.DataItem.ValueAt()",
            "Model.KeySuppression.Value", "Model.LocaleEditSpec.Picture",
            "Model.PicInfo.CurrencyString", "Model.PicInfo.EditMask",
            "Model.PictureClauseIdentity.CharacterString", "Model.PictureClauseIdentity.CurrencyString",
            "Model.PictureClauseIdentity.Editing", "Model.PictureClauseIdentity.EditingText()",
            "Model.PictureClauseIdentity.Spelled()",
            "Model.SuppressWhenOperand.Characters", "Model.TableValuePhrase.Literals", "Model.TableValuePlan.LiteralAt()",
            // a skeleton's recorded source and the directive lines of the state it was compiled under: COBOL source text, which both backends
            // write as an attribute string alike (kb/Work PB2104)
            "Model.CompileState.CobolWordsMap", "Model.CompileState.CompilationVariables", "Model.CompileState.DirectiveFolds",
            "Model.SkeletonRecord.Source", "Model.ExpansionToWrite.Text", "Model.ExpansionToWrite.ClientWritten",   // the texts of the client's tokens of that source (kb/Work PB4721)
            // a published record's PICTURE, editing and currency strings, as the publishing element wrote them (kb/Work PB4319)
            "Model.CurrencyRecord.Text", "Model.EditRuleRecord.Neg", "Model.EditRuleRecord.Pos", "Model.LocaleEditRecord.Picture",
            "Model.PicRecord.CurrencyString", "Model.PicRecord.EditMask", "Model.PictureClauseRecord.CharacterString",
            "Model.PictureClauseRecord.CurrencyString", "Model.PictureClauseRecord.Editing",
            "Model.TableValueSpec.FromText", "Model.TableValueSpec.FromWritten", "Model.TableValueSpec.Literals",
            "Model.TableValueSpec.ToText", "Model.TableValueSpec.ToWritten",
            // the OPTIONS paragraph's INITIALIZE ... TO literal (ISO §2023:11.9.10), in compiler.model since kb/Work PB2402
            "Model.OptionsInitialize.FillLiteral",
        ]),
        new(NeutralKind.RuntimeDatum,
        [
            "Bound.BoundDeclarative.EcEntries", "Bound.BoundIntrinsicCall.OrderingTable",
            "Bound.BoundRaise.EcName", "Bound.BoundRaise.Location",
            "Bound.BoundRaising.EcName", "Bound.BoundRaising.Location", "Bound.BoundRaising.PdRaising",
            "Bound.BoundRaising.StatementName", "Bound.BoundWhenOperand.Ec",
            "Bound.EcStatementInfo.Enabled", "Bound.EcStatementInfo.Location", "Bound.EcStatementInfo.StatementName",
            "Bound.BoundDebugTarget.CdKey", "Model.BoundUnit.RegistryKey", "Model.CommunicationDescription.Key", "Model.FileModel.AssignTarget", "Model.FileModel.ExternalName",
            "Model.LocalePhrase.Tag", "Model.LocaleRef.Tag", "Model.LocaleSymbol.External", "Model.LocaleSymbol.Tag",
            "Model.ObjectRefDescriptor.Spelled", "Model.LocaleEditRecord.External",
            // the assemblies an expansion is imported from or written to: file paths the backend references (kb/Work PB2104)
            "Model.ExpansionPlacement.HoldingFile", "Model.ExpansionPlacements.ReferencedAssemblies",
            // the assemblies the compilation imported a class or interface from, and an external file connector's §2023:12.4.5.3
            // GR1 identity, which the runtime compares across describers: answers the binder writes (kb/Work PB2403)
            "Model.ImportedTypes.Assemblies", "Model.FileModel.SelectFingerprint",
            // a class's run-unit keys and file-connector key bands, which the runtime recognizes, and the assembly an imported
            // class came from (kb/Work PB2402 moved them to compiler.model)
            "Model.OoClassSymbol.RunUnitKey", "Model.OoClassSymbol.HalfKey()", "Model.NamingConvention.ExternalFileBand",
            "Model.NamingConvention.InstanceFileBand", "Model.NamingConvention.FactoryFileBand", "Model.ImportedFrom.AssemblyFile",
            // the externalized names of a source element's REPOSITORY program-specifiers, which a CALL hands the program table
            // (ISO §2023:12.3.8.4 GR10; DataBindResult, kb/Work PB2361)
            "Model.ConfigurationFacts.ProgramSpecifierNames",
        ]),
        new(NeutralKind.DiagnosticText,
        [
            "Bound.BoundBoolError.Feature", "Bound.BoundConditionError.Feature", "Bound.BoundDeferredToExpansion.Construct",
            "Bound.BoundExprError.Feature", "Bound.BoundObjectView.Written", "Bound.BoundOpenFile.Unsupported",
            "Bound.BoundOperandError.Feature", "Bound.BoundRead.Unsupported", "Bound.BoundRejected.Code",
            "Bound.BoundRewrite.Unsupported", "Bound.BoundUnsupported.Feature", "Bound.BoundWrite.Unsupported",
            "Bound.ImplicitMovePhrase.Cite", "Bound.ImplicitMovePhrase.Statement", "Bound.ImplicitMovePhrase.Via()",
            "Bound.ImplicitMovePhrase.Where()", "Bound.ImplicitMovePhrase.WhereOf()",
            "Bound.InitializeErrorAction.Feature", "Bound.TableElementMove.Unsupported",
            "Bound.IntoPhraseRules.ArmB1985", "Bound.IntoPhraseRules.ArmB2023",
            "Bound.IntoPhraseRules.EntryFace", "Bound.IntoPhraseRules.Statement",
            "Bound.IntoPhraseRules.StrongReceiverCite", "Bound.IntoPhraseRules.StrongReceiverCount",
            // how a receiver names itself in the emitter's internal-error message (kb/Work PB5386)
            "Bound.PropertyReceiverKey.Face", "Bound.ReceiverBracketKey.Face", "Bound.WrittenReceiverKey.Face",
            // a rule's citation and wording as each edition numbers it, for a diagnostic (kb/Work PB5352)
            "Model.EditionCite.At()", "Model.EditionCite.Iso2023", "Model.EditionCite.Sequential1985",
            "Model.EditionCite.Relative1985", "Model.EditionCite.Indexed1985",
            "Model.DescriptionClauses.AlignedOrDynamicLengthMismatch()", "Model.DescriptionCopyAttribute.Reason",
            "Model.ItemCategory.Face()", "Model.RecordClauseFacts.FormatFace", "Model.RecordClauseFacts.LowerName",
            "Model.RecordClauseFacts.UpperName", "Model.SectionDataItem.NonSectionKind()", "Model.TierCIsland.Reason()",
            "Model.UsageFamilies.NoValueClauseRule()", "Model.UsagePhraseRule.AdmittedOn", "Model.UsagePhraseRule.Rule",
            "Model.UsagePhraseRule.Words", "Model.VariableLengthCompatibility.DynamicLengthOrVariableLengthGroup()",
            "Model.VariableLengthCompatibility.Mismatch()", "Model.VariableLengthCompatibility.PairRefusal()",
            "Model.VariableLengthCompatibility.Reason()",
            "Model.FileModel.BeyondAssign", "Model.FileModel.EntryFace", "Model.FileModel.OrganizationFace", "Model.OccursKey.Written",
            "Model.PictureClauseIdentity.Phrases()", "Model.RaisingTarget.Spelled", "Model.RecordKey.Face",
            "Model.RedefinesClass.RejectReason", "Model.SameClause.Written", "Model.SuppressWhenOperand.Written",
            // a file description clause as written, which the external connector's agreement diagnostic shows (kb/Work PB5362)
            "Model.FdClauseValue.Written",
            // how a diagnostic names an arithmetic mode's intermediate data item and its bounds (kb/Work PB194, PB2402)
            "Model.ArithmeticModes.IntermediateName()", "Model.ArithmeticModes.IntermediateExtremes()",
        ]),
        new(NeutralKind.IdentityKey,
        [
            "Model.DataItem.PointerRestrictionIdentity", "Model.PictureClauseIdentity.Key",
            // the corresponding external data item an external file's clauses name, compared across describers (kb/Work PB2418)
            "Model.DataItem.ExternalItemIdentity()",
            // an expansion's key and the labeled digests it is made of: compared for identity, written as attribute strings (kb/Work PB2104)
            "Model.ExpansionIdentity.Key", "Model.ExpansionIdentity.Parts",
            "Model.StrongTypeModel.EssentialCharacteristicsKey()", "Model.StrongTypeModel.InvariantRecordText()",
            "Model.StrongTypeModel.TypeIdentityKey()", "Model.StrongTypeModel+TypeRestriction.Key",
            "Model.StrongTypeModel.ElementaryLayoutKey()", "Model.DynamicLengthStructure.DefinitionKey",
            "Model.DynamicLengthStructure.KeyOf()", "Model.ObjectRefDescriptor.Identity",
            // a method's dispatch key: the roster's identity of a method, compared, never emitted as C# (kb/Work PB2402)
            "Model.OoMethodSymbol.DispatchKey",
            // what a file description entry specifies in the clauses an external connector's entries agree in, compared across describers at compile time (kb/Work PB5300)
            "Model.FileModel.FdClauses", "Model.FdClauseValue.Value",
        ]),
    ],
    [
        // L4: a node names a storage member (an index cell, a data-address pointer, an ALTER field, a file's instance
        // key, a factory class) by text instead of carrying the symbol whose CLR name the backend reads.
        new("L4", ["PB3251"], ["Model.FileModel.InstanceKeyField"]),
        new("L4", [],
        [
            "Bound.BoundIndexRef.IndexField", "Bound.BoundSearch.IndexField", "Bound.SetIndexTarget.IndexField",
            "Model.IndexDeclaration.Cell", "Model.PositionIndexCell.Cell",
            "Model.PositionPointerDynBase.PointerField", "Model.PositionPointerOffset.PointerField",
            "Model.RedefinesClass.BasedPointerField", "Bound.BoundOdoExtent.BasedAddress",
            "Bound.BoundAlterEntry.AlterField", "Bound.BoundGoToAlterable.AlterField",
            "Bound.BoundSetObjectRef.SourceFactoryCs",
            // the storage tier's channels, cell backings and bridges and the linkage roots the emitter does not declare, each
            // naming a storage member by text: on the backend input since the frozen data binding replaced the binder on it
            // (DataBindResult, kb/Work PB2361), which measured them where the binder had hidden them
            "Model.StorageTierSlice.StaticRootFields", "Model.StorageTierSlice._staticRootFields",
            "Model.StorageTierSlice.StaticBasedBridgeAddrs", "Model.StorageTierSlice._staticBasedBridgeAddrs",
            "Model.StorageTierSlice.StaticAddressableCells", "Model.StorageTierSlice._staticAddressableCells",
            "Model.StorageTierSlice.StaticIndexCells", "Model.StorageTierSlice._staticIndexCells",
            "Model.StorageTierSlice.AddressableBackings", "Model.StorageTierSlice._addressableBackings",
            "Model.StorageTierSlice.BasedBridges", "Model.StorageTierSlice._basedBridges",
            "Model.StorageTierSlice.AddressableCellOf", "Model.StorageTierSlice._addressableCellOf",
            "Model.LinkageFacts.SuppressedRootFields", "Model.DataBindResult.CellLifetimes", "Model.DataBindResult.CellFieldsOf()",
            // the factory singleton field every class emits, named by text (in compiler.model since kb/Work PB2402)
            "Model.NamingConvention.FactoryInstanceField",
        ]),
        // L6: the binder still declared in the bound layer (its text members and the parse tree it returns), which the
        // edges file tolerates until StatementBinder moves to Binding/Procedure.
        new("L6", ["PB2347"],
        [
            "Bound.StatementBinder.FirstToken()", "Bound.StatementBinder.ProgramRestrictionOf()",
            "Bound.StatementBinder.NestedCallables", "Bound.StatementBinder.ProgramPrototypes",
            "Bound.StatementBinder.UdfSelfName", "Bound.StatementBinder.UserFunctions",
            "Bound.StatementBinder.InputOutputStatement()", "Bound.StatementBinder.Children()",
        ]),
        // L9: C# source text, runtime type and member spellings, and C# operators on the model and the bound tree.
        new("L9", ["PB3252"],
        [
            "Model.PicInfo.ClrType", "Model.PicInfo.DefaultInitializer", "Model.PicInfo.ProfileInitializer()",
            "Model.PicInfo.SignKind", "Model.PicInfo.SignKindFor()", "Model.PicRecord.SignKind", "Model.ObjectRefDescriptor.ClrTypeName",
            "Model.OmittedFormalGuard.Presence", "Model.OmittedFormalGuard.PresenceMember()",
            "Model.OmittedProbe+Carrier.CarrierField",
            "Model.OmittedProbe+MethodFlag.FlagParam", "Model.DataItem.ElementType", "Model.DataItem.FieldType",
            "Model.HostType.CsType",
        ]),
        new("L9", [],
        [
            "Bound.BoundRelational.Op", "Bound.BoundLogical.Op", "Bound.BoundKeyedStart.Op",
            "Bound.BoundCodedSetClassCondition.Kind",
        ]),
    ]);
}
