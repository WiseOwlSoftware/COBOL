// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Tests.Unit.Architecture;

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
            "Bound.BoundDebugSubject.SubjectName", "Bound.BoundDeclarative.SectionName", "Bound.BoundExceptionMatch.OpenMode",
            "Bound.BoundMethod.CobolName", "Bound.BoundNumLiteral.FoldedFunction", "Bound.BoundNumericLiteral.FoldedFunction",
            "Bound.BoundParagraph.CobolName", "Bound.BoundProgramAddress.Prototype", "Bound.BoundRangeMembership.Alphabet",
            "Bound.BoundSetSwitches.Switches", "Bound.BoundSwitchCondition.ImplementorName",
            "Model.AlternateKeyClause.Name", "Model.AlternateKeyClause.Qualifiers",
            "Model.BoundUnit.ExternalizedName", "Model.BoundUnit.Name",
            "Model.CollatingKeyOperand.Name", "Model.CollatingKeyOperand.Qualifiers",
            "Model.Condition88.Alphabet", "Model.Condition88.Name",
            "Model.DataItem.CobolName", "Model.DataItem.ExternalizedAs", "Model.DataItem.IndexNames",
            "Model.DataItem.RedefinesTargetName", "Model.DataItem.SameAsName", "Model.DataItem.SameAsQualifiers",
            "Model.DataItem.TypeName", "Model.DataItem.TypeRefName", "Model.DataItem.WrittenTypeName",
            "Model.DynamicLengthStructure.Name", "Model.DynamicLengthStructure.PhysicalStructureName",
            "Model.FileModel.AssignUsingName", "Model.FileModel.AssignUsingQualifiers", "Model.FileModel.CobolName",
            "Model.FileModel.FileLevelCollating", "Model.FileModel.FileStatusName", "Model.FileModel.FileStatusQualifiers",
            "Model.FileModel.RecordDelimiter", "Model.FileModel.RecordKeyName", "Model.FileModel.RecordKeyQualifiers",
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
        ]),
        new(NeutralKind.ClrName,
        [
            "Model.BoundUnit.ClassName", "Model.DataItem.StructName", "Model.DataItem.ProfileName",
            "Model.CsNameScope._claimed", "Model.CsNameScope._reserved",
            "Model.DataItem.Sanitize()", "Model.DataItem.IdentifierCharacters()", "Model.DataItem.WordIdentifier()",
        ]),
        new(NeutralKind.LiteralValue,
        [
            "Bound.BoundAllLiteral.Literal", "Bound.BoundBoolAll.Bits", "Bound.BoundBoolLiteral.Bits",
            "Bound.BoundCallProgram.LiteralName", "Bound.BoundCancel.Targets", "Bound.BoundInvokeArg.NumericLiteral",
            "Bound.BoundInvokeUniversal.MethodLiteral", "Bound.BoundNumLiteral.Text", "Bound.BoundNumericLiteral.Image",
            "Bound.BoundNumericLiteral.Text", "Bound.BoundProgramAddress.NameLiteral", "Bound.BoundSetEntry.NameLiteral",
            "Bound.BoundSetFunctionAddress.NameLiteral", "Bound.BoundStopLiteral.Text", "Bound.BoundStringLiteral.Value",
            "Bound.BoundUserClassCondition.Members",
            "Model.Condition88.FalseValue", "Model.Condition88.Values",
            "Model.DataItem.PictureText", "Model.DataItem.RawValue", "Model.DataItem.ValueAt()",
            "Model.KeySuppression.Value", "Model.LocaleEditSpec.Picture",
            "Model.PicInfo.CurrencyString", "Model.PicInfo.EditMask",
            "Model.PictureClauseIdentity.CharacterString", "Model.PictureClauseIdentity.CurrencyString",
            "Model.PictureClauseIdentity.Editing", "Model.PictureClauseIdentity.EditingText()",
            "Model.PictureClauseIdentity.Spelled()",
            "Model.SuppressWhenOperand.Characters", "Model.TableValuePhrase.Literals", "Model.TableValuePlan.LiteralAt()",
            "Model.TableValueSpec.FromText", "Model.TableValueSpec.FromWritten", "Model.TableValueSpec.Literals",
            "Model.TableValueSpec.ToText", "Model.TableValueSpec.ToWritten",
        ]),
        new(NeutralKind.RuntimeDatum,
        [
            "Bound.BoundDeclarative.EcEntries", "Bound.BoundIntrinsicCall.OrderingTable",
            "Bound.BoundRaise.EcName", "Bound.BoundRaise.Location",
            "Bound.BoundRaising.EcName", "Bound.BoundRaising.Location", "Bound.BoundRaising.PdRaising",
            "Bound.BoundRaising.StatementName", "Bound.BoundWhenOperand.Ec",
            "Bound.EcStatementInfo.Enabled", "Bound.EcStatementInfo.Location", "Bound.EcStatementInfo.StatementName",
            "Model.BoundUnit.RegistryKey", "Model.FileModel.AssignTarget", "Model.FileModel.ExternalName",
            "Model.LocaleRef.Tag", "Model.LocaleSymbol.External", "Model.LocaleSymbol.Tag",
            "Model.ObjectRefDescriptor.Spelled",
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
            "Model.FileModel.EntryFace", "Model.FileModel.OrganizationFace", "Model.OccursKey.Written",
            "Model.PictureClauseIdentity.Phrases()", "Model.RaisingTarget.Spelled", "Model.RecordKey.Face",
            "Model.RedefinesClass.RejectReason", "Model.SameClause.Written", "Model.SuppressWhenOperand.Written",
        ]),
        new(NeutralKind.IdentityKey,
        [
            "Model.DataItem.PointerRestrictionIdentity", "Model.PictureClauseIdentity.Key",
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
        ]),
        // L7: C# access text built from names.
        new("L7", ["PB4320"],
        [
            "Model.RootFieldSegment.CsField", "Model.MemberSegment.CsMember", "Model.CallBridge.Field", "Model.CallBridge.Path",
            "Bound.BoundTableSortKey.MemberPath", "Model.BoundUnit.AnchorOf()", "Model.BoundUnit.ClassRef",
        ]),
        // L8: the names of C# method locals.
        new("L8", ["PB4321"],
        [
            "Model.PositionLocal.CsName", "Model.LocalVector.CsName", "Bound.BoundReportVaryingRef.CsName",
            "Bound.CorrespondingHoist.Local", "Bound.InitializeLoop.Var", "Bound.TableElementMove.Var",
        ]),
        // L9: C# source text, runtime type and member spellings, and C# operators on the model and the bound tree.
        new("L9", ["PB3252"],
        [
            "Model.PicInfo.ClrType", "Model.PicInfo.DefaultInitializer", "Model.PicInfo.ProfileInitializer()",
            "Model.PicInfo.SignKind", "Model.PicInfo.SignKindFor()", "Model.ObjectRefDescriptor.ClrTypeName",
            "Model.OmittedFormalGuard.Presence", "Model.OmittedFormalGuard.PresenceMember()",
            "Model.OmittedFormalGuard.CarrierPrefix", "Model.OmittedProbe+Carrier.CarrierField",
            "Model.OmittedProbe+MethodFlag.FlagParam", "Model.DataItem.ElementType", "Model.DataItem.FieldType",
        ]),
        new("L9", [],
        [
            "Bound.BoundRelational.Op", "Bound.BoundLogical.Op", "Bound.BoundKeyedStart.Op",
            "Bound.BoundCodedSetClassCondition.Kind",
        ]),
    ]);
}
