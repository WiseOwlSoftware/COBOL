// Generated from e:/CobolSharp/src/CobolSharp.Compiler/Grammar/CobolParserCore.g4 by ANTLR 4.13.1
import org.antlr.v4.runtime.tree.ParseTreeListener;

/**
 * This interface defines a complete listener for a parse tree produced by
 * {@link CobolParserCore}.
 */
public interface CobolParserCoreListener extends ParseTreeListener {
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#compilationUnit}.
	 * @param ctx the parse tree
	 */
	void enterCompilationUnit(CobolParserCore.CompilationUnitContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#compilationUnit}.
	 * @param ctx the parse tree
	 */
	void exitCompilationUnit(CobolParserCore.CompilationUnitContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#compilationGroup}.
	 * @param ctx the parse tree
	 */
	void enterCompilationGroup(CobolParserCore.CompilationGroupContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#compilationGroup}.
	 * @param ctx the parse tree
	 */
	void exitCompilationGroup(CobolParserCore.CompilationGroupContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#programUnit}.
	 * @param ctx the parse tree
	 */
	void enterProgramUnit(CobolParserCore.ProgramUnitContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#programUnit}.
	 * @param ctx the parse tree
	 */
	void exitProgramUnit(CobolParserCore.ProgramUnitContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#identificationDivision}.
	 * @param ctx the parse tree
	 */
	void enterIdentificationDivision(CobolParserCore.IdentificationDivisionContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#identificationDivision}.
	 * @param ctx the parse tree
	 */
	void exitIdentificationDivision(CobolParserCore.IdentificationDivisionContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#identificationBody}.
	 * @param ctx the parse tree
	 */
	void enterIdentificationBody(CobolParserCore.IdentificationBodyContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#identificationBody}.
	 * @param ctx the parse tree
	 */
	void exitIdentificationBody(CobolParserCore.IdentificationBodyContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#programIdParagraph}.
	 * @param ctx the parse tree
	 */
	void enterProgramIdParagraph(CobolParserCore.ProgramIdParagraphContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#programIdParagraph}.
	 * @param ctx the parse tree
	 */
	void exitProgramIdParagraph(CobolParserCore.ProgramIdParagraphContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#programName}.
	 * @param ctx the parse tree
	 */
	void enterProgramName(CobolParserCore.ProgramNameContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#programName}.
	 * @param ctx the parse tree
	 */
	void exitProgramName(CobolParserCore.ProgramNameContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#programIdAttributes}.
	 * @param ctx the parse tree
	 */
	void enterProgramIdAttributes(CobolParserCore.ProgramIdAttributesContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#programIdAttributes}.
	 * @param ctx the parse tree
	 */
	void exitProgramIdAttributes(CobolParserCore.ProgramIdAttributesContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#programIdAttribute}.
	 * @param ctx the parse tree
	 */
	void enterProgramIdAttribute(CobolParserCore.ProgramIdAttributeContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#programIdAttribute}.
	 * @param ctx the parse tree
	 */
	void exitProgramIdAttribute(CobolParserCore.ProgramIdAttributeContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#commonProgramAttribute}.
	 * @param ctx the parse tree
	 */
	void enterCommonProgramAttribute(CobolParserCore.CommonProgramAttributeContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#commonProgramAttribute}.
	 * @param ctx the parse tree
	 */
	void exitCommonProgramAttribute(CobolParserCore.CommonProgramAttributeContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#literalAttribute}.
	 * @param ctx the parse tree
	 */
	void enterLiteralAttribute(CobolParserCore.LiteralAttributeContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#literalAttribute}.
	 * @param ctx the parse tree
	 */
	void exitLiteralAttribute(CobolParserCore.LiteralAttributeContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#dataReferenceAttribute}.
	 * @param ctx the parse tree
	 */
	void enterDataReferenceAttribute(CobolParserCore.DataReferenceAttributeContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#dataReferenceAttribute}.
	 * @param ctx the parse tree
	 */
	void exitDataReferenceAttribute(CobolParserCore.DataReferenceAttributeContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#identificationParagraph}.
	 * @param ctx the parse tree
	 */
	void enterIdentificationParagraph(CobolParserCore.IdentificationParagraphContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#identificationParagraph}.
	 * @param ctx the parse tree
	 */
	void exitIdentificationParagraph(CobolParserCore.IdentificationParagraphContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#authorParagraph}.
	 * @param ctx the parse tree
	 */
	void enterAuthorParagraph(CobolParserCore.AuthorParagraphContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#authorParagraph}.
	 * @param ctx the parse tree
	 */
	void exitAuthorParagraph(CobolParserCore.AuthorParagraphContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#authorContent}.
	 * @param ctx the parse tree
	 */
	void enterAuthorContent(CobolParserCore.AuthorContentContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#authorContent}.
	 * @param ctx the parse tree
	 */
	void exitAuthorContent(CobolParserCore.AuthorContentContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#installationParagraph}.
	 * @param ctx the parse tree
	 */
	void enterInstallationParagraph(CobolParserCore.InstallationParagraphContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#installationParagraph}.
	 * @param ctx the parse tree
	 */
	void exitInstallationParagraph(CobolParserCore.InstallationParagraphContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#installationContent}.
	 * @param ctx the parse tree
	 */
	void enterInstallationContent(CobolParserCore.InstallationContentContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#installationContent}.
	 * @param ctx the parse tree
	 */
	void exitInstallationContent(CobolParserCore.InstallationContentContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#dateWrittenParagraph}.
	 * @param ctx the parse tree
	 */
	void enterDateWrittenParagraph(CobolParserCore.DateWrittenParagraphContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#dateWrittenParagraph}.
	 * @param ctx the parse tree
	 */
	void exitDateWrittenParagraph(CobolParserCore.DateWrittenParagraphContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#dateWrittenContent}.
	 * @param ctx the parse tree
	 */
	void enterDateWrittenContent(CobolParserCore.DateWrittenContentContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#dateWrittenContent}.
	 * @param ctx the parse tree
	 */
	void exitDateWrittenContent(CobolParserCore.DateWrittenContentContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#dateCompiledParagraph}.
	 * @param ctx the parse tree
	 */
	void enterDateCompiledParagraph(CobolParserCore.DateCompiledParagraphContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#dateCompiledParagraph}.
	 * @param ctx the parse tree
	 */
	void exitDateCompiledParagraph(CobolParserCore.DateCompiledParagraphContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#dateCompiledContent}.
	 * @param ctx the parse tree
	 */
	void enterDateCompiledContent(CobolParserCore.DateCompiledContentContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#dateCompiledContent}.
	 * @param ctx the parse tree
	 */
	void exitDateCompiledContent(CobolParserCore.DateCompiledContentContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#securityParagraph}.
	 * @param ctx the parse tree
	 */
	void enterSecurityParagraph(CobolParserCore.SecurityParagraphContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#securityParagraph}.
	 * @param ctx the parse tree
	 */
	void exitSecurityParagraph(CobolParserCore.SecurityParagraphContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#securityContent}.
	 * @param ctx the parse tree
	 */
	void enterSecurityContent(CobolParserCore.SecurityContentContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#securityContent}.
	 * @param ctx the parse tree
	 */
	void exitSecurityContent(CobolParserCore.SecurityContentContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#remarksParagraph}.
	 * @param ctx the parse tree
	 */
	void enterRemarksParagraph(CobolParserCore.RemarksParagraphContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#remarksParagraph}.
	 * @param ctx the parse tree
	 */
	void exitRemarksParagraph(CobolParserCore.RemarksParagraphContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#remarksContent}.
	 * @param ctx the parse tree
	 */
	void enterRemarksContent(CobolParserCore.RemarksContentContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#remarksContent}.
	 * @param ctx the parse tree
	 */
	void exitRemarksContent(CobolParserCore.RemarksContentContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#genericIdentificationParagraph}.
	 * @param ctx the parse tree
	 */
	void enterGenericIdentificationParagraph(CobolParserCore.GenericIdentificationParagraphContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#genericIdentificationParagraph}.
	 * @param ctx the parse tree
	 */
	void exitGenericIdentificationParagraph(CobolParserCore.GenericIdentificationParagraphContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#environmentDivision}.
	 * @param ctx the parse tree
	 */
	void enterEnvironmentDivision(CobolParserCore.EnvironmentDivisionContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#environmentDivision}.
	 * @param ctx the parse tree
	 */
	void exitEnvironmentDivision(CobolParserCore.EnvironmentDivisionContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#configurationSection}.
	 * @param ctx the parse tree
	 */
	void enterConfigurationSection(CobolParserCore.ConfigurationSectionContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#configurationSection}.
	 * @param ctx the parse tree
	 */
	void exitConfigurationSection(CobolParserCore.ConfigurationSectionContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#configurationParagraph}.
	 * @param ctx the parse tree
	 */
	void enterConfigurationParagraph(CobolParserCore.ConfigurationParagraphContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#configurationParagraph}.
	 * @param ctx the parse tree
	 */
	void exitConfigurationParagraph(CobolParserCore.ConfigurationParagraphContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#sourceComputerParagraph}.
	 * @param ctx the parse tree
	 */
	void enterSourceComputerParagraph(CobolParserCore.SourceComputerParagraphContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#sourceComputerParagraph}.
	 * @param ctx the parse tree
	 */
	void exitSourceComputerParagraph(CobolParserCore.SourceComputerParagraphContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#objectComputerParagraph}.
	 * @param ctx the parse tree
	 */
	void enterObjectComputerParagraph(CobolParserCore.ObjectComputerParagraphContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#objectComputerParagraph}.
	 * @param ctx the parse tree
	 */
	void exitObjectComputerParagraph(CobolParserCore.ObjectComputerParagraphContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#computerName}.
	 * @param ctx the parse tree
	 */
	void enterComputerName(CobolParserCore.ComputerNameContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#computerName}.
	 * @param ctx the parse tree
	 */
	void exitComputerName(CobolParserCore.ComputerNameContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#computerAttributes}.
	 * @param ctx the parse tree
	 */
	void enterComputerAttributes(CobolParserCore.ComputerAttributesContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#computerAttributes}.
	 * @param ctx the parse tree
	 */
	void exitComputerAttributes(CobolParserCore.ComputerAttributesContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#specialNamesParagraph}.
	 * @param ctx the parse tree
	 */
	void enterSpecialNamesParagraph(CobolParserCore.SpecialNamesParagraphContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#specialNamesParagraph}.
	 * @param ctx the parse tree
	 */
	void exitSpecialNamesParagraph(CobolParserCore.SpecialNamesParagraphContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#specialNameEntry}.
	 * @param ctx the parse tree
	 */
	void enterSpecialNameEntry(CobolParserCore.SpecialNameEntryContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#specialNameEntry}.
	 * @param ctx the parse tree
	 */
	void exitSpecialNameEntry(CobolParserCore.SpecialNameEntryContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#implementorSwitchEntry}.
	 * @param ctx the parse tree
	 */
	void enterImplementorSwitchEntry(CobolParserCore.ImplementorSwitchEntryContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#implementorSwitchEntry}.
	 * @param ctx the parse tree
	 */
	void exitImplementorSwitchEntry(CobolParserCore.ImplementorSwitchEntryContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#currencySignClause}.
	 * @param ctx the parse tree
	 */
	void enterCurrencySignClause(CobolParserCore.CurrencySignClauseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#currencySignClause}.
	 * @param ctx the parse tree
	 */
	void exitCurrencySignClause(CobolParserCore.CurrencySignClauseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#decimalPointClause}.
	 * @param ctx the parse tree
	 */
	void enterDecimalPointClause(CobolParserCore.DecimalPointClauseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#decimalPointClause}.
	 * @param ctx the parse tree
	 */
	void exitDecimalPointClause(CobolParserCore.DecimalPointClauseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#classDefinitionClause}.
	 * @param ctx the parse tree
	 */
	void enterClassDefinitionClause(CobolParserCore.ClassDefinitionClauseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#classDefinitionClause}.
	 * @param ctx the parse tree
	 */
	void exitClassDefinitionClause(CobolParserCore.ClassDefinitionClauseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#classValueSet}.
	 * @param ctx the parse tree
	 */
	void enterClassValueSet(CobolParserCore.ClassValueSetContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#classValueSet}.
	 * @param ctx the parse tree
	 */
	void exitClassValueSet(CobolParserCore.ClassValueSetContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#classValueItem}.
	 * @param ctx the parse tree
	 */
	void enterClassValueItem(CobolParserCore.ClassValueItemContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#classValueItem}.
	 * @param ctx the parse tree
	 */
	void exitClassValueItem(CobolParserCore.ClassValueItemContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#symbolicCharactersClause}.
	 * @param ctx the parse tree
	 */
	void enterSymbolicCharactersClause(CobolParserCore.SymbolicCharactersClauseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#symbolicCharactersClause}.
	 * @param ctx the parse tree
	 */
	void exitSymbolicCharactersClause(CobolParserCore.SymbolicCharactersClauseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#symbolicCharacterEntry}.
	 * @param ctx the parse tree
	 */
	void enterSymbolicCharacterEntry(CobolParserCore.SymbolicCharacterEntryContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#symbolicCharacterEntry}.
	 * @param ctx the parse tree
	 */
	void exitSymbolicCharacterEntry(CobolParserCore.SymbolicCharacterEntryContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#alphabetClause}.
	 * @param ctx the parse tree
	 */
	void enterAlphabetClause(CobolParserCore.AlphabetClauseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#alphabetClause}.
	 * @param ctx the parse tree
	 */
	void exitAlphabetClause(CobolParserCore.AlphabetClauseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#alphabetDefinition}.
	 * @param ctx the parse tree
	 */
	void enterAlphabetDefinition(CobolParserCore.AlphabetDefinitionContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#alphabetDefinition}.
	 * @param ctx the parse tree
	 */
	void exitAlphabetDefinition(CobolParserCore.AlphabetDefinitionContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#crtStatusClause}.
	 * @param ctx the parse tree
	 */
	void enterCrtStatusClause(CobolParserCore.CrtStatusClauseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#crtStatusClause}.
	 * @param ctx the parse tree
	 */
	void exitCrtStatusClause(CobolParserCore.CrtStatusClauseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#cursorClause}.
	 * @param ctx the parse tree
	 */
	void enterCursorClause(CobolParserCore.CursorClauseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#cursorClause}.
	 * @param ctx the parse tree
	 */
	void exitCursorClause(CobolParserCore.CursorClauseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#channelClause}.
	 * @param ctx the parse tree
	 */
	void enterChannelClause(CobolParserCore.ChannelClauseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#channelClause}.
	 * @param ctx the parse tree
	 */
	void exitChannelClause(CobolParserCore.ChannelClauseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#reserveClause}.
	 * @param ctx the parse tree
	 */
	void enterReserveClause(CobolParserCore.ReserveClauseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#reserveClause}.
	 * @param ctx the parse tree
	 */
	void exitReserveClause(CobolParserCore.ReserveClauseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#vendorConfigurationParagraph}.
	 * @param ctx the parse tree
	 */
	void enterVendorConfigurationParagraph(CobolParserCore.VendorConfigurationParagraphContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#vendorConfigurationParagraph}.
	 * @param ctx the parse tree
	 */
	void exitVendorConfigurationParagraph(CobolParserCore.VendorConfigurationParagraphContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#inputOutputSection}.
	 * @param ctx the parse tree
	 */
	void enterInputOutputSection(CobolParserCore.InputOutputSectionContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#inputOutputSection}.
	 * @param ctx the parse tree
	 */
	void exitInputOutputSection(CobolParserCore.InputOutputSectionContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#fileControlParagraph}.
	 * @param ctx the parse tree
	 */
	void enterFileControlParagraph(CobolParserCore.FileControlParagraphContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#fileControlParagraph}.
	 * @param ctx the parse tree
	 */
	void exitFileControlParagraph(CobolParserCore.FileControlParagraphContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#fileControlClauseGroup}.
	 * @param ctx the parse tree
	 */
	void enterFileControlClauseGroup(CobolParserCore.FileControlClauseGroupContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#fileControlClauseGroup}.
	 * @param ctx the parse tree
	 */
	void exitFileControlClauseGroup(CobolParserCore.FileControlClauseGroupContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#assignTarget}.
	 * @param ctx the parse tree
	 */
	void enterAssignTarget(CobolParserCore.AssignTargetContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#assignTarget}.
	 * @param ctx the parse tree
	 */
	void exitAssignTarget(CobolParserCore.AssignTargetContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#fileControlClauses}.
	 * @param ctx the parse tree
	 */
	void enterFileControlClauses(CobolParserCore.FileControlClausesContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#fileControlClauses}.
	 * @param ctx the parse tree
	 */
	void exitFileControlClauses(CobolParserCore.FileControlClausesContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#organizationClause}.
	 * @param ctx the parse tree
	 */
	void enterOrganizationClause(CobolParserCore.OrganizationClauseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#organizationClause}.
	 * @param ctx the parse tree
	 */
	void exitOrganizationClause(CobolParserCore.OrganizationClauseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#organizationType}.
	 * @param ctx the parse tree
	 */
	void enterOrganizationType(CobolParserCore.OrganizationTypeContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#organizationType}.
	 * @param ctx the parse tree
	 */
	void exitOrganizationType(CobolParserCore.OrganizationTypeContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#accessModeClause}.
	 * @param ctx the parse tree
	 */
	void enterAccessModeClause(CobolParserCore.AccessModeClauseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#accessModeClause}.
	 * @param ctx the parse tree
	 */
	void exitAccessModeClause(CobolParserCore.AccessModeClauseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#accessMode}.
	 * @param ctx the parse tree
	 */
	void enterAccessMode(CobolParserCore.AccessModeContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#accessMode}.
	 * @param ctx the parse tree
	 */
	void exitAccessMode(CobolParserCore.AccessModeContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#recordKeyClause}.
	 * @param ctx the parse tree
	 */
	void enterRecordKeyClause(CobolParserCore.RecordKeyClauseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#recordKeyClause}.
	 * @param ctx the parse tree
	 */
	void exitRecordKeyClause(CobolParserCore.RecordKeyClauseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#alternateKeyClause}.
	 * @param ctx the parse tree
	 */
	void enterAlternateKeyClause(CobolParserCore.AlternateKeyClauseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#alternateKeyClause}.
	 * @param ctx the parse tree
	 */
	void exitAlternateKeyClause(CobolParserCore.AlternateKeyClauseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#fileStatusClause}.
	 * @param ctx the parse tree
	 */
	void enterFileStatusClause(CobolParserCore.FileStatusClauseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#fileStatusClause}.
	 * @param ctx the parse tree
	 */
	void exitFileStatusClause(CobolParserCore.FileStatusClauseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#vendorFileControlClause}.
	 * @param ctx the parse tree
	 */
	void enterVendorFileControlClause(CobolParserCore.VendorFileControlClauseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#vendorFileControlClause}.
	 * @param ctx the parse tree
	 */
	void exitVendorFileControlClause(CobolParserCore.VendorFileControlClauseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#ioControlParagraph}.
	 * @param ctx the parse tree
	 */
	void enterIoControlParagraph(CobolParserCore.IoControlParagraphContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#ioControlParagraph}.
	 * @param ctx the parse tree
	 */
	void exitIoControlParagraph(CobolParserCore.IoControlParagraphContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#ioControlEntry}.
	 * @param ctx the parse tree
	 */
	void enterIoControlEntry(CobolParserCore.IoControlEntryContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#ioControlEntry}.
	 * @param ctx the parse tree
	 */
	void exitIoControlEntry(CobolParserCore.IoControlEntryContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#dataDivision}.
	 * @param ctx the parse tree
	 */
	void enterDataDivision(CobolParserCore.DataDivisionContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#dataDivision}.
	 * @param ctx the parse tree
	 */
	void exitDataDivision(CobolParserCore.DataDivisionContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#fileSection}.
	 * @param ctx the parse tree
	 */
	void enterFileSection(CobolParserCore.FileSectionContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#fileSection}.
	 * @param ctx the parse tree
	 */
	void exitFileSection(CobolParserCore.FileSectionContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#fileDescriptionEntry}.
	 * @param ctx the parse tree
	 */
	void enterFileDescriptionEntry(CobolParserCore.FileDescriptionEntryContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#fileDescriptionEntry}.
	 * @param ctx the parse tree
	 */
	void exitFileDescriptionEntry(CobolParserCore.FileDescriptionEntryContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#reportSection}.
	 * @param ctx the parse tree
	 */
	void enterReportSection(CobolParserCore.ReportSectionContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#reportSection}.
	 * @param ctx the parse tree
	 */
	void exitReportSection(CobolParserCore.ReportSectionContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#reportDescriptionEntry}.
	 * @param ctx the parse tree
	 */
	void enterReportDescriptionEntry(CobolParserCore.ReportDescriptionEntryContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#reportDescriptionEntry}.
	 * @param ctx the parse tree
	 */
	void exitReportDescriptionEntry(CobolParserCore.ReportDescriptionEntryContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#reportName}.
	 * @param ctx the parse tree
	 */
	void enterReportName(CobolParserCore.ReportNameContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#reportName}.
	 * @param ctx the parse tree
	 */
	void exitReportName(CobolParserCore.ReportNameContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#reportDescriptionClauses}.
	 * @param ctx the parse tree
	 */
	void enterReportDescriptionClauses(CobolParserCore.ReportDescriptionClausesContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#reportDescriptionClauses}.
	 * @param ctx the parse tree
	 */
	void exitReportDescriptionClauses(CobolParserCore.ReportDescriptionClausesContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#reportDescriptionClause}.
	 * @param ctx the parse tree
	 */
	void enterReportDescriptionClause(CobolParserCore.ReportDescriptionClauseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#reportDescriptionClause}.
	 * @param ctx the parse tree
	 */
	void exitReportDescriptionClause(CobolParserCore.ReportDescriptionClauseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#reportGroupEntry}.
	 * @param ctx the parse tree
	 */
	void enterReportGroupEntry(CobolParserCore.ReportGroupEntryContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#reportGroupEntry}.
	 * @param ctx the parse tree
	 */
	void exitReportGroupEntry(CobolParserCore.ReportGroupEntryContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#reportGroupName}.
	 * @param ctx the parse tree
	 */
	void enterReportGroupName(CobolParserCore.ReportGroupNameContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#reportGroupName}.
	 * @param ctx the parse tree
	 */
	void exitReportGroupName(CobolParserCore.ReportGroupNameContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#reportGroupBody}.
	 * @param ctx the parse tree
	 */
	void enterReportGroupBody(CobolParserCore.ReportGroupBodyContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#reportGroupBody}.
	 * @param ctx the parse tree
	 */
	void exitReportGroupBody(CobolParserCore.ReportGroupBodyContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#reportGroupClause}.
	 * @param ctx the parse tree
	 */
	void enterReportGroupClause(CobolParserCore.ReportGroupClauseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#reportGroupClause}.
	 * @param ctx the parse tree
	 */
	void exitReportGroupClause(CobolParserCore.ReportGroupClauseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#reportTypeClause}.
	 * @param ctx the parse tree
	 */
	void enterReportTypeClause(CobolParserCore.ReportTypeClauseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#reportTypeClause}.
	 * @param ctx the parse tree
	 */
	void exitReportTypeClause(CobolParserCore.ReportTypeClauseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#reportSumClause}.
	 * @param ctx the parse tree
	 */
	void enterReportSumClause(CobolParserCore.ReportSumClauseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#reportSumClause}.
	 * @param ctx the parse tree
	 */
	void exitReportSumClause(CobolParserCore.ReportSumClauseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#sumItem}.
	 * @param ctx the parse tree
	 */
	void enterSumItem(CobolParserCore.SumItemContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#sumItem}.
	 * @param ctx the parse tree
	 */
	void exitSumItem(CobolParserCore.SumItemContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#genericReportGroupClause}.
	 * @param ctx the parse tree
	 */
	void enterGenericReportGroupClause(CobolParserCore.GenericReportGroupClauseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#genericReportGroupClause}.
	 * @param ctx the parse tree
	 */
	void exitGenericReportGroupClause(CobolParserCore.GenericReportGroupClauseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#fileDescriptionClauses}.
	 * @param ctx the parse tree
	 */
	void enterFileDescriptionClauses(CobolParserCore.FileDescriptionClausesContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#fileDescriptionClauses}.
	 * @param ctx the parse tree
	 */
	void exitFileDescriptionClauses(CobolParserCore.FileDescriptionClausesContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#fileDescriptionClause}.
	 * @param ctx the parse tree
	 */
	void enterFileDescriptionClause(CobolParserCore.FileDescriptionClauseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#fileDescriptionClause}.
	 * @param ctx the parse tree
	 */
	void exitFileDescriptionClause(CobolParserCore.FileDescriptionClauseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#dataRecordsClause}.
	 * @param ctx the parse tree
	 */
	void enterDataRecordsClause(CobolParserCore.DataRecordsClauseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#dataRecordsClause}.
	 * @param ctx the parse tree
	 */
	void exitDataRecordsClause(CobolParserCore.DataRecordsClauseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#genericFileDescriptionClause}.
	 * @param ctx the parse tree
	 */
	void enterGenericFileDescriptionClause(CobolParserCore.GenericFileDescriptionClauseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#genericFileDescriptionClause}.
	 * @param ctx the parse tree
	 */
	void exitGenericFileDescriptionClause(CobolParserCore.GenericFileDescriptionClauseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#workingStorageSection}.
	 * @param ctx the parse tree
	 */
	void enterWorkingStorageSection(CobolParserCore.WorkingStorageSectionContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#workingStorageSection}.
	 * @param ctx the parse tree
	 */
	void exitWorkingStorageSection(CobolParserCore.WorkingStorageSectionContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#localStorageSection}.
	 * @param ctx the parse tree
	 */
	void enterLocalStorageSection(CobolParserCore.LocalStorageSectionContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#localStorageSection}.
	 * @param ctx the parse tree
	 */
	void exitLocalStorageSection(CobolParserCore.LocalStorageSectionContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#linkageSection}.
	 * @param ctx the parse tree
	 */
	void enterLinkageSection(CobolParserCore.LinkageSectionContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#linkageSection}.
	 * @param ctx the parse tree
	 */
	void exitLinkageSection(CobolParserCore.LinkageSectionContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#linkageEntry}.
	 * @param ctx the parse tree
	 */
	void enterLinkageEntry(CobolParserCore.LinkageEntryContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#linkageEntry}.
	 * @param ctx the parse tree
	 */
	void exitLinkageEntry(CobolParserCore.LinkageEntryContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#linkageProcedureParameter}.
	 * @param ctx the parse tree
	 */
	void enterLinkageProcedureParameter(CobolParserCore.LinkageProcedureParameterContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#linkageProcedureParameter}.
	 * @param ctx the parse tree
	 */
	void exitLinkageProcedureParameter(CobolParserCore.LinkageProcedureParameterContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#parameterDescriptionBody}.
	 * @param ctx the parse tree
	 */
	void enterParameterDescriptionBody(CobolParserCore.ParameterDescriptionBodyContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#parameterDescriptionBody}.
	 * @param ctx the parse tree
	 */
	void exitParameterDescriptionBody(CobolParserCore.ParameterDescriptionBodyContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#parameterPassingClause}.
	 * @param ctx the parse tree
	 */
	void enterParameterPassingClause(CobolParserCore.ParameterPassingClauseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#parameterPassingClause}.
	 * @param ctx the parse tree
	 */
	void exitParameterPassingClause(CobolParserCore.ParameterPassingClauseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#dataDescriptionEntry}.
	 * @param ctx the parse tree
	 */
	void enterDataDescriptionEntry(CobolParserCore.DataDescriptionEntryContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#dataDescriptionEntry}.
	 * @param ctx the parse tree
	 */
	void exitDataDescriptionEntry(CobolParserCore.DataDescriptionEntryContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#levelNumber}.
	 * @param ctx the parse tree
	 */
	void enterLevelNumber(CobolParserCore.LevelNumberContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#levelNumber}.
	 * @param ctx the parse tree
	 */
	void exitLevelNumber(CobolParserCore.LevelNumberContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#dataName}.
	 * @param ctx the parse tree
	 */
	void enterDataName(CobolParserCore.DataNameContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#dataName}.
	 * @param ctx the parse tree
	 */
	void exitDataName(CobolParserCore.DataNameContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#dataDescriptionBody}.
	 * @param ctx the parse tree
	 */
	void enterDataDescriptionBody(CobolParserCore.DataDescriptionBodyContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#dataDescriptionBody}.
	 * @param ctx the parse tree
	 */
	void exitDataDescriptionBody(CobolParserCore.DataDescriptionBodyContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#dataDescriptionClauses}.
	 * @param ctx the parse tree
	 */
	void enterDataDescriptionClauses(CobolParserCore.DataDescriptionClausesContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#dataDescriptionClauses}.
	 * @param ctx the parse tree
	 */
	void exitDataDescriptionClauses(CobolParserCore.DataDescriptionClausesContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#dataDescriptionClause}.
	 * @param ctx the parse tree
	 */
	void enterDataDescriptionClause(CobolParserCore.DataDescriptionClauseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#dataDescriptionClause}.
	 * @param ctx the parse tree
	 */
	void exitDataDescriptionClause(CobolParserCore.DataDescriptionClauseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#typeClause}.
	 * @param ctx the parse tree
	 */
	void enterTypeClause(CobolParserCore.TypeClauseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#typeClause}.
	 * @param ctx the parse tree
	 */
	void exitTypeClause(CobolParserCore.TypeClauseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#genericDataClause}.
	 * @param ctx the parse tree
	 */
	void enterGenericDataClause(CobolParserCore.GenericDataClauseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#genericDataClause}.
	 * @param ctx the parse tree
	 */
	void exitGenericDataClause(CobolParserCore.GenericDataClauseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#pictureClause}.
	 * @param ctx the parse tree
	 */
	void enterPictureClause(CobolParserCore.PictureClauseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#pictureClause}.
	 * @param ctx the parse tree
	 */
	void exitPictureClause(CobolParserCore.PictureClauseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#usageClause}.
	 * @param ctx the parse tree
	 */
	void enterUsageClause(CobolParserCore.UsageClauseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#usageClause}.
	 * @param ctx the parse tree
	 */
	void exitUsageClause(CobolParserCore.UsageClauseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#usageKeyword}.
	 * @param ctx the parse tree
	 */
	void enterUsageKeyword(CobolParserCore.UsageKeywordContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#usageKeyword}.
	 * @param ctx the parse tree
	 */
	void exitUsageKeyword(CobolParserCore.UsageKeywordContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#occursClause}.
	 * @param ctx the parse tree
	 */
	void enterOccursClause(CobolParserCore.OccursClauseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#occursClause}.
	 * @param ctx the parse tree
	 */
	void exitOccursClause(CobolParserCore.OccursClauseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#occursKeyClause}.
	 * @param ctx the parse tree
	 */
	void enterOccursKeyClause(CobolParserCore.OccursKeyClauseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#occursKeyClause}.
	 * @param ctx the parse tree
	 */
	void exitOccursKeyClause(CobolParserCore.OccursKeyClauseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#timesKeyword}.
	 * @param ctx the parse tree
	 */
	void enterTimesKeyword(CobolParserCore.TimesKeywordContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#timesKeyword}.
	 * @param ctx the parse tree
	 */
	void exitTimesKeyword(CobolParserCore.TimesKeywordContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#integerLiteral}.
	 * @param ctx the parse tree
	 */
	void enterIntegerLiteral(CobolParserCore.IntegerLiteralContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#integerLiteral}.
	 * @param ctx the parse tree
	 */
	void exitIntegerLiteral(CobolParserCore.IntegerLiteralContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#redefinesClause}.
	 * @param ctx the parse tree
	 */
	void enterRedefinesClause(CobolParserCore.RedefinesClauseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#redefinesClause}.
	 * @param ctx the parse tree
	 */
	void exitRedefinesClause(CobolParserCore.RedefinesClauseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#renamesClause}.
	 * @param ctx the parse tree
	 */
	void enterRenamesClause(CobolParserCore.RenamesClauseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#renamesClause}.
	 * @param ctx the parse tree
	 */
	void exitRenamesClause(CobolParserCore.RenamesClauseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#valueClause}.
	 * @param ctx the parse tree
	 */
	void enterValueClause(CobolParserCore.ValueClauseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#valueClause}.
	 * @param ctx the parse tree
	 */
	void exitValueClause(CobolParserCore.ValueClauseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#valueItem}.
	 * @param ctx the parse tree
	 */
	void enterValueItem(CobolParserCore.ValueItemContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#valueItem}.
	 * @param ctx the parse tree
	 */
	void exitValueItem(CobolParserCore.ValueItemContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#signClause}.
	 * @param ctx the parse tree
	 */
	void enterSignClause(CobolParserCore.SignClauseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#signClause}.
	 * @param ctx the parse tree
	 */
	void exitSignClause(CobolParserCore.SignClauseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#justifiedClause}.
	 * @param ctx the parse tree
	 */
	void enterJustifiedClause(CobolParserCore.JustifiedClauseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#justifiedClause}.
	 * @param ctx the parse tree
	 */
	void exitJustifiedClause(CobolParserCore.JustifiedClauseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#syncClause}.
	 * @param ctx the parse tree
	 */
	void enterSyncClause(CobolParserCore.SyncClauseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#syncClause}.
	 * @param ctx the parse tree
	 */
	void exitSyncClause(CobolParserCore.SyncClauseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#blankWhenZeroClause}.
	 * @param ctx the parse tree
	 */
	void enterBlankWhenZeroClause(CobolParserCore.BlankWhenZeroClauseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#blankWhenZeroClause}.
	 * @param ctx the parse tree
	 */
	void exitBlankWhenZeroClause(CobolParserCore.BlankWhenZeroClauseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#procedureDivision}.
	 * @param ctx the parse tree
	 */
	void enterProcedureDivision(CobolParserCore.ProcedureDivisionContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#procedureDivision}.
	 * @param ctx the parse tree
	 */
	void exitProcedureDivision(CobolParserCore.ProcedureDivisionContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#usingClause}.
	 * @param ctx the parse tree
	 */
	void enterUsingClause(CobolParserCore.UsingClauseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#usingClause}.
	 * @param ctx the parse tree
	 */
	void exitUsingClause(CobolParserCore.UsingClauseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#returningClause}.
	 * @param ctx the parse tree
	 */
	void enterReturningClause(CobolParserCore.ReturningClauseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#returningClause}.
	 * @param ctx the parse tree
	 */
	void exitReturningClause(CobolParserCore.ReturningClauseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#dataReferenceList}.
	 * @param ctx the parse tree
	 */
	void enterDataReferenceList(CobolParserCore.DataReferenceListContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#dataReferenceList}.
	 * @param ctx the parse tree
	 */
	void exitDataReferenceList(CobolParserCore.DataReferenceListContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#dataReference}.
	 * @param ctx the parse tree
	 */
	void enterDataReference(CobolParserCore.DataReferenceContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#dataReference}.
	 * @param ctx the parse tree
	 */
	void exitDataReference(CobolParserCore.DataReferenceContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#dataReferenceSuffix}.
	 * @param ctx the parse tree
	 */
	void enterDataReferenceSuffix(CobolParserCore.DataReferenceSuffixContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#dataReferenceSuffix}.
	 * @param ctx the parse tree
	 */
	void exitDataReferenceSuffix(CobolParserCore.DataReferenceSuffixContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#qualification}.
	 * @param ctx the parse tree
	 */
	void enterQualification(CobolParserCore.QualificationContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#qualification}.
	 * @param ctx the parse tree
	 */
	void exitQualification(CobolParserCore.QualificationContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#subscriptPart}.
	 * @param ctx the parse tree
	 */
	void enterSubscriptPart(CobolParserCore.SubscriptPartContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#subscriptPart}.
	 * @param ctx the parse tree
	 */
	void exitSubscriptPart(CobolParserCore.SubscriptPartContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#refModPart}.
	 * @param ctx the parse tree
	 */
	void enterRefModPart(CobolParserCore.RefModPartContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#refModPart}.
	 * @param ctx the parse tree
	 */
	void exitRefModPart(CobolParserCore.RefModPartContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#refModSpec}.
	 * @param ctx the parse tree
	 */
	void enterRefModSpec(CobolParserCore.RefModSpecContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#refModSpec}.
	 * @param ctx the parse tree
	 */
	void exitRefModSpec(CobolParserCore.RefModSpecContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#subscriptList}.
	 * @param ctx the parse tree
	 */
	void enterSubscriptList(CobolParserCore.SubscriptListContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#subscriptList}.
	 * @param ctx the parse tree
	 */
	void exitSubscriptList(CobolParserCore.SubscriptListContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#fileName}.
	 * @param ctx the parse tree
	 */
	void enterFileName(CobolParserCore.FileNameContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#fileName}.
	 * @param ctx the parse tree
	 */
	void exitFileName(CobolParserCore.FileNameContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#declarativePart}.
	 * @param ctx the parse tree
	 */
	void enterDeclarativePart(CobolParserCore.DeclarativePartContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#declarativePart}.
	 * @param ctx the parse tree
	 */
	void exitDeclarativePart(CobolParserCore.DeclarativePartContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#declarativeSection}.
	 * @param ctx the parse tree
	 */
	void enterDeclarativeSection(CobolParserCore.DeclarativeSectionContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#declarativeSection}.
	 * @param ctx the parse tree
	 */
	void exitDeclarativeSection(CobolParserCore.DeclarativeSectionContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#declarativeParagraph}.
	 * @param ctx the parse tree
	 */
	void enterDeclarativeParagraph(CobolParserCore.DeclarativeParagraphContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#declarativeParagraph}.
	 * @param ctx the parse tree
	 */
	void exitDeclarativeParagraph(CobolParserCore.DeclarativeParagraphContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#sentence}.
	 * @param ctx the parse tree
	 */
	void enterSentence(CobolParserCore.SentenceContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#sentence}.
	 * @param ctx the parse tree
	 */
	void exitSentence(CobolParserCore.SentenceContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#procedureUnit}.
	 * @param ctx the parse tree
	 */
	void enterProcedureUnit(CobolParserCore.ProcedureUnitContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#procedureUnit}.
	 * @param ctx the parse tree
	 */
	void exitProcedureUnit(CobolParserCore.ProcedureUnitContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#sectionDefinition}.
	 * @param ctx the parse tree
	 */
	void enterSectionDefinition(CobolParserCore.SectionDefinitionContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#sectionDefinition}.
	 * @param ctx the parse tree
	 */
	void exitSectionDefinition(CobolParserCore.SectionDefinitionContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#sectionName}.
	 * @param ctx the parse tree
	 */
	void enterSectionName(CobolParserCore.SectionNameContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#sectionName}.
	 * @param ctx the parse tree
	 */
	void exitSectionName(CobolParserCore.SectionNameContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#paragraphDefinition}.
	 * @param ctx the parse tree
	 */
	void enterParagraphDefinition(CobolParserCore.ParagraphDefinitionContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#paragraphDefinition}.
	 * @param ctx the parse tree
	 */
	void exitParagraphDefinition(CobolParserCore.ParagraphDefinitionContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#paragraphName}.
	 * @param ctx the parse tree
	 */
	void enterParagraphName(CobolParserCore.ParagraphNameContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#paragraphName}.
	 * @param ctx the parse tree
	 */
	void exitParagraphName(CobolParserCore.ParagraphNameContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#statement}.
	 * @param ctx the parse tree
	 */
	void enterStatement(CobolParserCore.StatementContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#statement}.
	 * @param ctx the parse tree
	 */
	void exitStatement(CobolParserCore.StatementContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#statementBlock}.
	 * @param ctx the parse tree
	 */
	void enterStatementBlock(CobolParserCore.StatementBlockContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#statementBlock}.
	 * @param ctx the parse tree
	 */
	void exitStatementBlock(CobolParserCore.StatementBlockContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#alterStatement}.
	 * @param ctx the parse tree
	 */
	void enterAlterStatement(CobolParserCore.AlterStatementContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#alterStatement}.
	 * @param ctx the parse tree
	 */
	void exitAlterStatement(CobolParserCore.AlterStatementContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#alterEntry}.
	 * @param ctx the parse tree
	 */
	void enterAlterEntry(CobolParserCore.AlterEntryContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#alterEntry}.
	 * @param ctx the parse tree
	 */
	void exitAlterEntry(CobolParserCore.AlterEntryContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#useStatement}.
	 * @param ctx the parse tree
	 */
	void enterUseStatement(CobolParserCore.UseStatementContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#useStatement}.
	 * @param ctx the parse tree
	 */
	void exitUseStatement(CobolParserCore.UseStatementContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#readStatement}.
	 * @param ctx the parse tree
	 */
	void enterReadStatement(CobolParserCore.ReadStatementContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#readStatement}.
	 * @param ctx the parse tree
	 */
	void exitReadStatement(CobolParserCore.ReadStatementContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#readDirection}.
	 * @param ctx the parse tree
	 */
	void enterReadDirection(CobolParserCore.ReadDirectionContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#readDirection}.
	 * @param ctx the parse tree
	 */
	void exitReadDirection(CobolParserCore.ReadDirectionContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#readInto}.
	 * @param ctx the parse tree
	 */
	void enterReadInto(CobolParserCore.ReadIntoContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#readInto}.
	 * @param ctx the parse tree
	 */
	void exitReadInto(CobolParserCore.ReadIntoContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#readKey}.
	 * @param ctx the parse tree
	 */
	void enterReadKey(CobolParserCore.ReadKeyContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#readKey}.
	 * @param ctx the parse tree
	 */
	void exitReadKey(CobolParserCore.ReadKeyContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#readAtEnd}.
	 * @param ctx the parse tree
	 */
	void enterReadAtEnd(CobolParserCore.ReadAtEndContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#readAtEnd}.
	 * @param ctx the parse tree
	 */
	void exitReadAtEnd(CobolParserCore.ReadAtEndContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#readInvalidKey}.
	 * @param ctx the parse tree
	 */
	void enterReadInvalidKey(CobolParserCore.ReadInvalidKeyContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#readInvalidKey}.
	 * @param ctx the parse tree
	 */
	void exitReadInvalidKey(CobolParserCore.ReadInvalidKeyContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#writeStatement}.
	 * @param ctx the parse tree
	 */
	void enterWriteStatement(CobolParserCore.WriteStatementContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#writeStatement}.
	 * @param ctx the parse tree
	 */
	void exitWriteStatement(CobolParserCore.WriteStatementContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#writeFrom}.
	 * @param ctx the parse tree
	 */
	void enterWriteFrom(CobolParserCore.WriteFromContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#writeFrom}.
	 * @param ctx the parse tree
	 */
	void exitWriteFrom(CobolParserCore.WriteFromContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#writeBeforeAfter}.
	 * @param ctx the parse tree
	 */
	void enterWriteBeforeAfter(CobolParserCore.WriteBeforeAfterContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#writeBeforeAfter}.
	 * @param ctx the parse tree
	 */
	void exitWriteBeforeAfter(CobolParserCore.WriteBeforeAfterContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#writeInvalidKey}.
	 * @param ctx the parse tree
	 */
	void enterWriteInvalidKey(CobolParserCore.WriteInvalidKeyContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#writeInvalidKey}.
	 * @param ctx the parse tree
	 */
	void exitWriteInvalidKey(CobolParserCore.WriteInvalidKeyContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#openStatement}.
	 * @param ctx the parse tree
	 */
	void enterOpenStatement(CobolParserCore.OpenStatementContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#openStatement}.
	 * @param ctx the parse tree
	 */
	void exitOpenStatement(CobolParserCore.OpenStatementContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#openClause}.
	 * @param ctx the parse tree
	 */
	void enterOpenClause(CobolParserCore.OpenClauseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#openClause}.
	 * @param ctx the parse tree
	 */
	void exitOpenClause(CobolParserCore.OpenClauseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#openMode}.
	 * @param ctx the parse tree
	 */
	void enterOpenMode(CobolParserCore.OpenModeContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#openMode}.
	 * @param ctx the parse tree
	 */
	void exitOpenMode(CobolParserCore.OpenModeContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#closeStatement}.
	 * @param ctx the parse tree
	 */
	void enterCloseStatement(CobolParserCore.CloseStatementContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#closeStatement}.
	 * @param ctx the parse tree
	 */
	void exitCloseStatement(CobolParserCore.CloseStatementContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#ifStatement}.
	 * @param ctx the parse tree
	 */
	void enterIfStatement(CobolParserCore.IfStatementContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#ifStatement}.
	 * @param ctx the parse tree
	 */
	void exitIfStatement(CobolParserCore.IfStatementContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#performStatement}.
	 * @param ctx the parse tree
	 */
	void enterPerformStatement(CobolParserCore.PerformStatementContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#performStatement}.
	 * @param ctx the parse tree
	 */
	void exitPerformStatement(CobolParserCore.PerformStatementContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#performTarget}.
	 * @param ctx the parse tree
	 */
	void enterPerformTarget(CobolParserCore.PerformTargetContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#performTarget}.
	 * @param ctx the parse tree
	 */
	void exitPerformTarget(CobolParserCore.PerformTargetContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#procedureName}.
	 * @param ctx the parse tree
	 */
	void enterProcedureName(CobolParserCore.ProcedureNameContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#procedureName}.
	 * @param ctx the parse tree
	 */
	void exitProcedureName(CobolParserCore.ProcedureNameContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#performOptions}.
	 * @param ctx the parse tree
	 */
	void enterPerformOptions(CobolParserCore.PerformOptionsContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#performOptions}.
	 * @param ctx the parse tree
	 */
	void exitPerformOptions(CobolParserCore.PerformOptionsContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#performTimes}.
	 * @param ctx the parse tree
	 */
	void enterPerformTimes(CobolParserCore.PerformTimesContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#performTimes}.
	 * @param ctx the parse tree
	 */
	void exitPerformTimes(CobolParserCore.PerformTimesContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#performUntil}.
	 * @param ctx the parse tree
	 */
	void enterPerformUntil(CobolParserCore.PerformUntilContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#performUntil}.
	 * @param ctx the parse tree
	 */
	void exitPerformUntil(CobolParserCore.PerformUntilContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#performVarying}.
	 * @param ctx the parse tree
	 */
	void enterPerformVarying(CobolParserCore.PerformVaryingContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#performVarying}.
	 * @param ctx the parse tree
	 */
	void exitPerformVarying(CobolParserCore.PerformVaryingContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#performVaryingAfter}.
	 * @param ctx the parse tree
	 */
	void enterPerformVaryingAfter(CobolParserCore.PerformVaryingAfterContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#performVaryingAfter}.
	 * @param ctx the parse tree
	 */
	void exitPerformVaryingAfter(CobolParserCore.PerformVaryingAfterContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#evaluateStatement}.
	 * @param ctx the parse tree
	 */
	void enterEvaluateStatement(CobolParserCore.EvaluateStatementContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#evaluateStatement}.
	 * @param ctx the parse tree
	 */
	void exitEvaluateStatement(CobolParserCore.EvaluateStatementContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#evaluateSubject}.
	 * @param ctx the parse tree
	 */
	void enterEvaluateSubject(CobolParserCore.EvaluateSubjectContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#evaluateSubject}.
	 * @param ctx the parse tree
	 */
	void exitEvaluateSubject(CobolParserCore.EvaluateSubjectContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#classCondition}.
	 * @param ctx the parse tree
	 */
	void enterClassCondition(CobolParserCore.ClassConditionContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#classCondition}.
	 * @param ctx the parse tree
	 */
	void exitClassCondition(CobolParserCore.ClassConditionContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#evaluateWhenClause}.
	 * @param ctx the parse tree
	 */
	void enterEvaluateWhenClause(CobolParserCore.EvaluateWhenClauseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#evaluateWhenClause}.
	 * @param ctx the parse tree
	 */
	void exitEvaluateWhenClause(CobolParserCore.EvaluateWhenClauseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#evaluateWhenGroup}.
	 * @param ctx the parse tree
	 */
	void enterEvaluateWhenGroup(CobolParserCore.EvaluateWhenGroupContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#evaluateWhenGroup}.
	 * @param ctx the parse tree
	 */
	void exitEvaluateWhenGroup(CobolParserCore.EvaluateWhenGroupContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#evaluateWhenItem}.
	 * @param ctx the parse tree
	 */
	void enterEvaluateWhenItem(CobolParserCore.EvaluateWhenItemContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#evaluateWhenItem}.
	 * @param ctx the parse tree
	 */
	void exitEvaluateWhenItem(CobolParserCore.EvaluateWhenItemContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#computeStatement}.
	 * @param ctx the parse tree
	 */
	void enterComputeStatement(CobolParserCore.ComputeStatementContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#computeStatement}.
	 * @param ctx the parse tree
	 */
	void exitComputeStatement(CobolParserCore.ComputeStatementContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#computeStore}.
	 * @param ctx the parse tree
	 */
	void enterComputeStore(CobolParserCore.ComputeStoreContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#computeStore}.
	 * @param ctx the parse tree
	 */
	void exitComputeStore(CobolParserCore.ComputeStoreContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#computeOnSizeError}.
	 * @param ctx the parse tree
	 */
	void enterComputeOnSizeError(CobolParserCore.ComputeOnSizeErrorContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#computeOnSizeError}.
	 * @param ctx the parse tree
	 */
	void exitComputeOnSizeError(CobolParserCore.ComputeOnSizeErrorContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#continueStatement}.
	 * @param ctx the parse tree
	 */
	void enterContinueStatement(CobolParserCore.ContinueStatementContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#continueStatement}.
	 * @param ctx the parse tree
	 */
	void exitContinueStatement(CobolParserCore.ContinueStatementContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#nextSentenceStatement}.
	 * @param ctx the parse tree
	 */
	void enterNextSentenceStatement(CobolParserCore.NextSentenceStatementContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#nextSentenceStatement}.
	 * @param ctx the parse tree
	 */
	void exitNextSentenceStatement(CobolParserCore.NextSentenceStatementContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#inlineMethodInvocationStatement}.
	 * @param ctx the parse tree
	 */
	void enterInlineMethodInvocationStatement(CobolParserCore.InlineMethodInvocationStatementContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#inlineMethodInvocationStatement}.
	 * @param ctx the parse tree
	 */
	void exitInlineMethodInvocationStatement(CobolParserCore.InlineMethodInvocationStatementContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#argumentList}.
	 * @param ctx the parse tree
	 */
	void enterArgumentList(CobolParserCore.ArgumentListContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#argumentList}.
	 * @param ctx the parse tree
	 */
	void exitArgumentList(CobolParserCore.ArgumentListContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#argument}.
	 * @param ctx the parse tree
	 */
	void enterArgument(CobolParserCore.ArgumentContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#argument}.
	 * @param ctx the parse tree
	 */
	void exitArgument(CobolParserCore.ArgumentContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#receivingOperand}.
	 * @param ctx the parse tree
	 */
	void enterReceivingOperand(CobolParserCore.ReceivingOperandContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#receivingOperand}.
	 * @param ctx the parse tree
	 */
	void exitReceivingOperand(CobolParserCore.ReceivingOperandContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#receivingArithmeticOperand}.
	 * @param ctx the parse tree
	 */
	void enterReceivingArithmeticOperand(CobolParserCore.ReceivingArithmeticOperandContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#receivingArithmeticOperand}.
	 * @param ctx the parse tree
	 */
	void exitReceivingArithmeticOperand(CobolParserCore.ReceivingArithmeticOperandContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#arithmeticOnSizeError}.
	 * @param ctx the parse tree
	 */
	void enterArithmeticOnSizeError(CobolParserCore.ArithmeticOnSizeErrorContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#arithmeticOnSizeError}.
	 * @param ctx the parse tree
	 */
	void exitArithmeticOnSizeError(CobolParserCore.ArithmeticOnSizeErrorContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#addStatement}.
	 * @param ctx the parse tree
	 */
	void enterAddStatement(CobolParserCore.AddStatementContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#addStatement}.
	 * @param ctx the parse tree
	 */
	void exitAddStatement(CobolParserCore.AddStatementContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#addOperandList}.
	 * @param ctx the parse tree
	 */
	void enterAddOperandList(CobolParserCore.AddOperandListContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#addOperandList}.
	 * @param ctx the parse tree
	 */
	void exitAddOperandList(CobolParserCore.AddOperandListContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#addOperand}.
	 * @param ctx the parse tree
	 */
	void enterAddOperand(CobolParserCore.AddOperandContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#addOperand}.
	 * @param ctx the parse tree
	 */
	void exitAddOperand(CobolParserCore.AddOperandContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#addToPhrase}.
	 * @param ctx the parse tree
	 */
	void enterAddToPhrase(CobolParserCore.AddToPhraseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#addToPhrase}.
	 * @param ctx the parse tree
	 */
	void exitAddToPhrase(CobolParserCore.AddToPhraseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#addGivingPhrase}.
	 * @param ctx the parse tree
	 */
	void enterAddGivingPhrase(CobolParserCore.AddGivingPhraseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#addGivingPhrase}.
	 * @param ctx the parse tree
	 */
	void exitAddGivingPhrase(CobolParserCore.AddGivingPhraseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#subtractStatement}.
	 * @param ctx the parse tree
	 */
	void enterSubtractStatement(CobolParserCore.SubtractStatementContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#subtractStatement}.
	 * @param ctx the parse tree
	 */
	void exitSubtractStatement(CobolParserCore.SubtractStatementContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#subtractOperandList}.
	 * @param ctx the parse tree
	 */
	void enterSubtractOperandList(CobolParserCore.SubtractOperandListContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#subtractOperandList}.
	 * @param ctx the parse tree
	 */
	void exitSubtractOperandList(CobolParserCore.SubtractOperandListContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#subtractOperand}.
	 * @param ctx the parse tree
	 */
	void enterSubtractOperand(CobolParserCore.SubtractOperandContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#subtractOperand}.
	 * @param ctx the parse tree
	 */
	void exitSubtractOperand(CobolParserCore.SubtractOperandContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#subtractFromPhrase}.
	 * @param ctx the parse tree
	 */
	void enterSubtractFromPhrase(CobolParserCore.SubtractFromPhraseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#subtractFromPhrase}.
	 * @param ctx the parse tree
	 */
	void exitSubtractFromPhrase(CobolParserCore.SubtractFromPhraseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#subtractFromOperand}.
	 * @param ctx the parse tree
	 */
	void enterSubtractFromOperand(CobolParserCore.SubtractFromOperandContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#subtractFromOperand}.
	 * @param ctx the parse tree
	 */
	void exitSubtractFromOperand(CobolParserCore.SubtractFromOperandContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#subtractGivingPhrase}.
	 * @param ctx the parse tree
	 */
	void enterSubtractGivingPhrase(CobolParserCore.SubtractGivingPhraseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#subtractGivingPhrase}.
	 * @param ctx the parse tree
	 */
	void exitSubtractGivingPhrase(CobolParserCore.SubtractGivingPhraseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#multiplyStatement}.
	 * @param ctx the parse tree
	 */
	void enterMultiplyStatement(CobolParserCore.MultiplyStatementContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#multiplyStatement}.
	 * @param ctx the parse tree
	 */
	void exitMultiplyStatement(CobolParserCore.MultiplyStatementContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#multiplyOperand}.
	 * @param ctx the parse tree
	 */
	void enterMultiplyOperand(CobolParserCore.MultiplyOperandContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#multiplyOperand}.
	 * @param ctx the parse tree
	 */
	void exitMultiplyOperand(CobolParserCore.MultiplyOperandContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#multiplyByOperand}.
	 * @param ctx the parse tree
	 */
	void enterMultiplyByOperand(CobolParserCore.MultiplyByOperandContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#multiplyByOperand}.
	 * @param ctx the parse tree
	 */
	void exitMultiplyByOperand(CobolParserCore.MultiplyByOperandContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#multiplyGivingPhrase}.
	 * @param ctx the parse tree
	 */
	void enterMultiplyGivingPhrase(CobolParserCore.MultiplyGivingPhraseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#multiplyGivingPhrase}.
	 * @param ctx the parse tree
	 */
	void exitMultiplyGivingPhrase(CobolParserCore.MultiplyGivingPhraseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#divideStatement}.
	 * @param ctx the parse tree
	 */
	void enterDivideStatement(CobolParserCore.DivideStatementContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#divideStatement}.
	 * @param ctx the parse tree
	 */
	void exitDivideStatement(CobolParserCore.DivideStatementContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#divideOperand}.
	 * @param ctx the parse tree
	 */
	void enterDivideOperand(CobolParserCore.DivideOperandContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#divideOperand}.
	 * @param ctx the parse tree
	 */
	void exitDivideOperand(CobolParserCore.DivideOperandContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#divideIntoPhrase}.
	 * @param ctx the parse tree
	 */
	void enterDivideIntoPhrase(CobolParserCore.DivideIntoPhraseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#divideIntoPhrase}.
	 * @param ctx the parse tree
	 */
	void exitDivideIntoPhrase(CobolParserCore.DivideIntoPhraseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#divideIntoOperand}.
	 * @param ctx the parse tree
	 */
	void enterDivideIntoOperand(CobolParserCore.DivideIntoOperandContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#divideIntoOperand}.
	 * @param ctx the parse tree
	 */
	void exitDivideIntoOperand(CobolParserCore.DivideIntoOperandContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#divideByPhrase}.
	 * @param ctx the parse tree
	 */
	void enterDivideByPhrase(CobolParserCore.DivideByPhraseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#divideByPhrase}.
	 * @param ctx the parse tree
	 */
	void exitDivideByPhrase(CobolParserCore.DivideByPhraseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#divideGivingPhrase}.
	 * @param ctx the parse tree
	 */
	void enterDivideGivingPhrase(CobolParserCore.DivideGivingPhraseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#divideGivingPhrase}.
	 * @param ctx the parse tree
	 */
	void exitDivideGivingPhrase(CobolParserCore.DivideGivingPhraseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#divideRemainderPhrase}.
	 * @param ctx the parse tree
	 */
	void enterDivideRemainderPhrase(CobolParserCore.DivideRemainderPhraseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#divideRemainderPhrase}.
	 * @param ctx the parse tree
	 */
	void exitDivideRemainderPhrase(CobolParserCore.DivideRemainderPhraseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#moveStatement}.
	 * @param ctx the parse tree
	 */
	void enterMoveStatement(CobolParserCore.MoveStatementContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#moveStatement}.
	 * @param ctx the parse tree
	 */
	void exitMoveStatement(CobolParserCore.MoveStatementContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#moveSendingOperand}.
	 * @param ctx the parse tree
	 */
	void enterMoveSendingOperand(CobolParserCore.MoveSendingOperandContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#moveSendingOperand}.
	 * @param ctx the parse tree
	 */
	void exitMoveSendingOperand(CobolParserCore.MoveSendingOperandContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#moveReceivingPhrase}.
	 * @param ctx the parse tree
	 */
	void enterMoveReceivingPhrase(CobolParserCore.MoveReceivingPhraseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#moveReceivingPhrase}.
	 * @param ctx the parse tree
	 */
	void exitMoveReceivingPhrase(CobolParserCore.MoveReceivingPhraseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#stringStatement}.
	 * @param ctx the parse tree
	 */
	void enterStringStatement(CobolParserCore.StringStatementContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#stringStatement}.
	 * @param ctx the parse tree
	 */
	void exitStringStatement(CobolParserCore.StringStatementContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#stringSendingPhrase}.
	 * @param ctx the parse tree
	 */
	void enterStringSendingPhrase(CobolParserCore.StringSendingPhraseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#stringSendingPhrase}.
	 * @param ctx the parse tree
	 */
	void exitStringSendingPhrase(CobolParserCore.StringSendingPhraseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#delimitedByPhrase}.
	 * @param ctx the parse tree
	 */
	void enterDelimitedByPhrase(CobolParserCore.DelimitedByPhraseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#delimitedByPhrase}.
	 * @param ctx the parse tree
	 */
	void exitDelimitedByPhrase(CobolParserCore.DelimitedByPhraseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#stringIntoPhrase}.
	 * @param ctx the parse tree
	 */
	void enterStringIntoPhrase(CobolParserCore.StringIntoPhraseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#stringIntoPhrase}.
	 * @param ctx the parse tree
	 */
	void exitStringIntoPhrase(CobolParserCore.StringIntoPhraseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#stringWithPointer}.
	 * @param ctx the parse tree
	 */
	void enterStringWithPointer(CobolParserCore.StringWithPointerContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#stringWithPointer}.
	 * @param ctx the parse tree
	 */
	void exitStringWithPointer(CobolParserCore.StringWithPointerContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#stringOnOverflow}.
	 * @param ctx the parse tree
	 */
	void enterStringOnOverflow(CobolParserCore.StringOnOverflowContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#stringOnOverflow}.
	 * @param ctx the parse tree
	 */
	void exitStringOnOverflow(CobolParserCore.StringOnOverflowContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#unstringStatement}.
	 * @param ctx the parse tree
	 */
	void enterUnstringStatement(CobolParserCore.UnstringStatementContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#unstringStatement}.
	 * @param ctx the parse tree
	 */
	void exitUnstringStatement(CobolParserCore.UnstringStatementContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#unstringDelimiterPhrase}.
	 * @param ctx the parse tree
	 */
	void enterUnstringDelimiterPhrase(CobolParserCore.UnstringDelimiterPhraseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#unstringDelimiterPhrase}.
	 * @param ctx the parse tree
	 */
	void exitUnstringDelimiterPhrase(CobolParserCore.UnstringDelimiterPhraseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#unstringIntoPhrase}.
	 * @param ctx the parse tree
	 */
	void enterUnstringIntoPhrase(CobolParserCore.UnstringIntoPhraseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#unstringIntoPhrase}.
	 * @param ctx the parse tree
	 */
	void exitUnstringIntoPhrase(CobolParserCore.UnstringIntoPhraseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#unstringWithPointer}.
	 * @param ctx the parse tree
	 */
	void enterUnstringWithPointer(CobolParserCore.UnstringWithPointerContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#unstringWithPointer}.
	 * @param ctx the parse tree
	 */
	void exitUnstringWithPointer(CobolParserCore.UnstringWithPointerContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#unstringTallying}.
	 * @param ctx the parse tree
	 */
	void enterUnstringTallying(CobolParserCore.UnstringTallyingContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#unstringTallying}.
	 * @param ctx the parse tree
	 */
	void exitUnstringTallying(CobolParserCore.UnstringTallyingContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#unstringOnOverflow}.
	 * @param ctx the parse tree
	 */
	void enterUnstringOnOverflow(CobolParserCore.UnstringOnOverflowContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#unstringOnOverflow}.
	 * @param ctx the parse tree
	 */
	void exitUnstringOnOverflow(CobolParserCore.UnstringOnOverflowContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#callStatement}.
	 * @param ctx the parse tree
	 */
	void enterCallStatement(CobolParserCore.CallStatementContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#callStatement}.
	 * @param ctx the parse tree
	 */
	void exitCallStatement(CobolParserCore.CallStatementContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#callTarget}.
	 * @param ctx the parse tree
	 */
	void enterCallTarget(CobolParserCore.CallTargetContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#callTarget}.
	 * @param ctx the parse tree
	 */
	void exitCallTarget(CobolParserCore.CallTargetContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#callUsingPhrase}.
	 * @param ctx the parse tree
	 */
	void enterCallUsingPhrase(CobolParserCore.CallUsingPhraseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#callUsingPhrase}.
	 * @param ctx the parse tree
	 */
	void exitCallUsingPhrase(CobolParserCore.CallUsingPhraseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#callArgument}.
	 * @param ctx the parse tree
	 */
	void enterCallArgument(CobolParserCore.CallArgumentContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#callArgument}.
	 * @param ctx the parse tree
	 */
	void exitCallArgument(CobolParserCore.CallArgumentContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#callByReference}.
	 * @param ctx the parse tree
	 */
	void enterCallByReference(CobolParserCore.CallByReferenceContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#callByReference}.
	 * @param ctx the parse tree
	 */
	void exitCallByReference(CobolParserCore.CallByReferenceContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#callByValue}.
	 * @param ctx the parse tree
	 */
	void enterCallByValue(CobolParserCore.CallByValueContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#callByValue}.
	 * @param ctx the parse tree
	 */
	void exitCallByValue(CobolParserCore.CallByValueContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#callByContent}.
	 * @param ctx the parse tree
	 */
	void enterCallByContent(CobolParserCore.CallByContentContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#callByContent}.
	 * @param ctx the parse tree
	 */
	void exitCallByContent(CobolParserCore.CallByContentContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#callReturningPhrase}.
	 * @param ctx the parse tree
	 */
	void enterCallReturningPhrase(CobolParserCore.CallReturningPhraseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#callReturningPhrase}.
	 * @param ctx the parse tree
	 */
	void exitCallReturningPhrase(CobolParserCore.CallReturningPhraseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#callOnExceptionPhrase}.
	 * @param ctx the parse tree
	 */
	void enterCallOnExceptionPhrase(CobolParserCore.CallOnExceptionPhraseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#callOnExceptionPhrase}.
	 * @param ctx the parse tree
	 */
	void exitCallOnExceptionPhrase(CobolParserCore.CallOnExceptionPhraseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#cancelStatement}.
	 * @param ctx the parse tree
	 */
	void enterCancelStatement(CobolParserCore.CancelStatementContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#cancelStatement}.
	 * @param ctx the parse tree
	 */
	void exitCancelStatement(CobolParserCore.CancelStatementContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#setStatement}.
	 * @param ctx the parse tree
	 */
	void enterSetStatement(CobolParserCore.SetStatementContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#setStatement}.
	 * @param ctx the parse tree
	 */
	void exitSetStatement(CobolParserCore.SetStatementContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#setToValueStatement}.
	 * @param ctx the parse tree
	 */
	void enterSetToValueStatement(CobolParserCore.SetToValueStatementContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#setToValueStatement}.
	 * @param ctx the parse tree
	 */
	void exitSetToValueStatement(CobolParserCore.SetToValueStatementContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#setBooleanStatement}.
	 * @param ctx the parse tree
	 */
	void enterSetBooleanStatement(CobolParserCore.SetBooleanStatementContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#setBooleanStatement}.
	 * @param ctx the parse tree
	 */
	void exitSetBooleanStatement(CobolParserCore.SetBooleanStatementContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#setAddressStatement}.
	 * @param ctx the parse tree
	 */
	void enterSetAddressStatement(CobolParserCore.SetAddressStatementContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#setAddressStatement}.
	 * @param ctx the parse tree
	 */
	void exitSetAddressStatement(CobolParserCore.SetAddressStatementContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#setObjectReferenceStatement}.
	 * @param ctx the parse tree
	 */
	void enterSetObjectReferenceStatement(CobolParserCore.SetObjectReferenceStatementContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#setObjectReferenceStatement}.
	 * @param ctx the parse tree
	 */
	void exitSetObjectReferenceStatement(CobolParserCore.SetObjectReferenceStatementContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#objectReference}.
	 * @param ctx the parse tree
	 */
	void enterObjectReference(CobolParserCore.ObjectReferenceContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#objectReference}.
	 * @param ctx the parse tree
	 */
	void exitObjectReference(CobolParserCore.ObjectReferenceContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#setIndexStatement}.
	 * @param ctx the parse tree
	 */
	void enterSetIndexStatement(CobolParserCore.SetIndexStatementContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#setIndexStatement}.
	 * @param ctx the parse tree
	 */
	void exitSetIndexStatement(CobolParserCore.SetIndexStatementContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#sortStatement}.
	 * @param ctx the parse tree
	 */
	void enterSortStatement(CobolParserCore.SortStatementContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#sortStatement}.
	 * @param ctx the parse tree
	 */
	void exitSortStatement(CobolParserCore.SortStatementContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#sortFileName}.
	 * @param ctx the parse tree
	 */
	void enterSortFileName(CobolParserCore.SortFileNameContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#sortFileName}.
	 * @param ctx the parse tree
	 */
	void exitSortFileName(CobolParserCore.SortFileNameContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#sortKeyPhrase}.
	 * @param ctx the parse tree
	 */
	void enterSortKeyPhrase(CobolParserCore.SortKeyPhraseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#sortKeyPhrase}.
	 * @param ctx the parse tree
	 */
	void exitSortKeyPhrase(CobolParserCore.SortKeyPhraseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#sortUsingPhrase}.
	 * @param ctx the parse tree
	 */
	void enterSortUsingPhrase(CobolParserCore.SortUsingPhraseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#sortUsingPhrase}.
	 * @param ctx the parse tree
	 */
	void exitSortUsingPhrase(CobolParserCore.SortUsingPhraseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#sortGivingPhrase}.
	 * @param ctx the parse tree
	 */
	void enterSortGivingPhrase(CobolParserCore.SortGivingPhraseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#sortGivingPhrase}.
	 * @param ctx the parse tree
	 */
	void exitSortGivingPhrase(CobolParserCore.SortGivingPhraseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#sortInputProcedurePhrase}.
	 * @param ctx the parse tree
	 */
	void enterSortInputProcedurePhrase(CobolParserCore.SortInputProcedurePhraseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#sortInputProcedurePhrase}.
	 * @param ctx the parse tree
	 */
	void exitSortInputProcedurePhrase(CobolParserCore.SortInputProcedurePhraseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#sortOutputProcedurePhrase}.
	 * @param ctx the parse tree
	 */
	void enterSortOutputProcedurePhrase(CobolParserCore.SortOutputProcedurePhraseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#sortOutputProcedurePhrase}.
	 * @param ctx the parse tree
	 */
	void exitSortOutputProcedurePhrase(CobolParserCore.SortOutputProcedurePhraseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#mergeStatement}.
	 * @param ctx the parse tree
	 */
	void enterMergeStatement(CobolParserCore.MergeStatementContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#mergeStatement}.
	 * @param ctx the parse tree
	 */
	void exitMergeStatement(CobolParserCore.MergeStatementContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#mergeFileName}.
	 * @param ctx the parse tree
	 */
	void enterMergeFileName(CobolParserCore.MergeFileNameContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#mergeFileName}.
	 * @param ctx the parse tree
	 */
	void exitMergeFileName(CobolParserCore.MergeFileNameContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#mergeKeyPhrase}.
	 * @param ctx the parse tree
	 */
	void enterMergeKeyPhrase(CobolParserCore.MergeKeyPhraseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#mergeKeyPhrase}.
	 * @param ctx the parse tree
	 */
	void exitMergeKeyPhrase(CobolParserCore.MergeKeyPhraseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#mergeUsingPhrase}.
	 * @param ctx the parse tree
	 */
	void enterMergeUsingPhrase(CobolParserCore.MergeUsingPhraseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#mergeUsingPhrase}.
	 * @param ctx the parse tree
	 */
	void exitMergeUsingPhrase(CobolParserCore.MergeUsingPhraseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#mergeGivingPhrase}.
	 * @param ctx the parse tree
	 */
	void enterMergeGivingPhrase(CobolParserCore.MergeGivingPhraseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#mergeGivingPhrase}.
	 * @param ctx the parse tree
	 */
	void exitMergeGivingPhrase(CobolParserCore.MergeGivingPhraseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#mergeOutputProcedurePhrase}.
	 * @param ctx the parse tree
	 */
	void enterMergeOutputProcedurePhrase(CobolParserCore.MergeOutputProcedurePhraseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#mergeOutputProcedurePhrase}.
	 * @param ctx the parse tree
	 */
	void exitMergeOutputProcedurePhrase(CobolParserCore.MergeOutputProcedurePhraseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#returnStatement}.
	 * @param ctx the parse tree
	 */
	void enterReturnStatement(CobolParserCore.ReturnStatementContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#returnStatement}.
	 * @param ctx the parse tree
	 */
	void exitReturnStatement(CobolParserCore.ReturnStatementContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#returnAtEndPhrase}.
	 * @param ctx the parse tree
	 */
	void enterReturnAtEndPhrase(CobolParserCore.ReturnAtEndPhraseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#returnAtEndPhrase}.
	 * @param ctx the parse tree
	 */
	void exitReturnAtEndPhrase(CobolParserCore.ReturnAtEndPhraseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#releaseStatement}.
	 * @param ctx the parse tree
	 */
	void enterReleaseStatement(CobolParserCore.ReleaseStatementContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#releaseStatement}.
	 * @param ctx the parse tree
	 */
	void exitReleaseStatement(CobolParserCore.ReleaseStatementContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#rewriteStatement}.
	 * @param ctx the parse tree
	 */
	void enterRewriteStatement(CobolParserCore.RewriteStatementContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#rewriteStatement}.
	 * @param ctx the parse tree
	 */
	void exitRewriteStatement(CobolParserCore.RewriteStatementContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#recordName}.
	 * @param ctx the parse tree
	 */
	void enterRecordName(CobolParserCore.RecordNameContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#recordName}.
	 * @param ctx the parse tree
	 */
	void exitRecordName(CobolParserCore.RecordNameContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#rewriteInvalidKeyPhrase}.
	 * @param ctx the parse tree
	 */
	void enterRewriteInvalidKeyPhrase(CobolParserCore.RewriteInvalidKeyPhraseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#rewriteInvalidKeyPhrase}.
	 * @param ctx the parse tree
	 */
	void exitRewriteInvalidKeyPhrase(CobolParserCore.RewriteInvalidKeyPhraseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#deleteFileStatement}.
	 * @param ctx the parse tree
	 */
	void enterDeleteFileStatement(CobolParserCore.DeleteFileStatementContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#deleteFileStatement}.
	 * @param ctx the parse tree
	 */
	void exitDeleteFileStatement(CobolParserCore.DeleteFileStatementContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#deleteFileOnException}.
	 * @param ctx the parse tree
	 */
	void enterDeleteFileOnException(CobolParserCore.DeleteFileOnExceptionContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#deleteFileOnException}.
	 * @param ctx the parse tree
	 */
	void exitDeleteFileOnException(CobolParserCore.DeleteFileOnExceptionContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#deleteStatement}.
	 * @param ctx the parse tree
	 */
	void enterDeleteStatement(CobolParserCore.DeleteStatementContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#deleteStatement}.
	 * @param ctx the parse tree
	 */
	void exitDeleteStatement(CobolParserCore.DeleteStatementContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#deleteInvalidKeyPhrase}.
	 * @param ctx the parse tree
	 */
	void enterDeleteInvalidKeyPhrase(CobolParserCore.DeleteInvalidKeyPhraseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#deleteInvalidKeyPhrase}.
	 * @param ctx the parse tree
	 */
	void exitDeleteInvalidKeyPhrase(CobolParserCore.DeleteInvalidKeyPhraseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#exceptionPhrase}.
	 * @param ctx the parse tree
	 */
	void enterExceptionPhrase(CobolParserCore.ExceptionPhraseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#exceptionPhrase}.
	 * @param ctx the parse tree
	 */
	void exitExceptionPhrase(CobolParserCore.ExceptionPhraseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#onExceptionPhrase}.
	 * @param ctx the parse tree
	 */
	void enterOnExceptionPhrase(CobolParserCore.OnExceptionPhraseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#onExceptionPhrase}.
	 * @param ctx the parse tree
	 */
	void exitOnExceptionPhrase(CobolParserCore.OnExceptionPhraseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#notOnExceptionPhrase}.
	 * @param ctx the parse tree
	 */
	void enterNotOnExceptionPhrase(CobolParserCore.NotOnExceptionPhraseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#notOnExceptionPhrase}.
	 * @param ctx the parse tree
	 */
	void exitNotOnExceptionPhrase(CobolParserCore.NotOnExceptionPhraseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#stopStatement}.
	 * @param ctx the parse tree
	 */
	void enterStopStatement(CobolParserCore.StopStatementContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#stopStatement}.
	 * @param ctx the parse tree
	 */
	void exitStopStatement(CobolParserCore.StopStatementContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#gobackStatement}.
	 * @param ctx the parse tree
	 */
	void enterGobackStatement(CobolParserCore.GobackStatementContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#gobackStatement}.
	 * @param ctx the parse tree
	 */
	void exitGobackStatement(CobolParserCore.GobackStatementContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#exitStatement}.
	 * @param ctx the parse tree
	 */
	void enterExitStatement(CobolParserCore.ExitStatementContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#exitStatement}.
	 * @param ctx the parse tree
	 */
	void exitExitStatement(CobolParserCore.ExitStatementContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#startStatement}.
	 * @param ctx the parse tree
	 */
	void enterStartStatement(CobolParserCore.StartStatementContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#startStatement}.
	 * @param ctx the parse tree
	 */
	void exitStartStatement(CobolParserCore.StartStatementContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#startKeyPhrase}.
	 * @param ctx the parse tree
	 */
	void enterStartKeyPhrase(CobolParserCore.StartKeyPhraseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#startKeyPhrase}.
	 * @param ctx the parse tree
	 */
	void exitStartKeyPhrase(CobolParserCore.StartKeyPhraseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#startInvalidKeyPhrase}.
	 * @param ctx the parse tree
	 */
	void enterStartInvalidKeyPhrase(CobolParserCore.StartInvalidKeyPhraseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#startInvalidKeyPhrase}.
	 * @param ctx the parse tree
	 */
	void exitStartInvalidKeyPhrase(CobolParserCore.StartInvalidKeyPhraseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#goToStatement}.
	 * @param ctx the parse tree
	 */
	void enterGoToStatement(CobolParserCore.GoToStatementContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#goToStatement}.
	 * @param ctx the parse tree
	 */
	void exitGoToStatement(CobolParserCore.GoToStatementContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#acceptStatement}.
	 * @param ctx the parse tree
	 */
	void enterAcceptStatement(CobolParserCore.AcceptStatementContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#acceptStatement}.
	 * @param ctx the parse tree
	 */
	void exitAcceptStatement(CobolParserCore.AcceptStatementContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#acceptSource}.
	 * @param ctx the parse tree
	 */
	void enterAcceptSource(CobolParserCore.AcceptSourceContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#acceptSource}.
	 * @param ctx the parse tree
	 */
	void exitAcceptSource(CobolParserCore.AcceptSourceContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#displayStatement}.
	 * @param ctx the parse tree
	 */
	void enterDisplayStatement(CobolParserCore.DisplayStatementContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#displayStatement}.
	 * @param ctx the parse tree
	 */
	void exitDisplayStatement(CobolParserCore.DisplayStatementContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#initializeStatement}.
	 * @param ctx the parse tree
	 */
	void enterInitializeStatement(CobolParserCore.InitializeStatementContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#initializeStatement}.
	 * @param ctx the parse tree
	 */
	void exitInitializeStatement(CobolParserCore.InitializeStatementContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#initializeReplacingPhrase}.
	 * @param ctx the parse tree
	 */
	void enterInitializeReplacingPhrase(CobolParserCore.InitializeReplacingPhraseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#initializeReplacingPhrase}.
	 * @param ctx the parse tree
	 */
	void exitInitializeReplacingPhrase(CobolParserCore.InitializeReplacingPhraseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#initializeReplacingItem}.
	 * @param ctx the parse tree
	 */
	void enterInitializeReplacingItem(CobolParserCore.InitializeReplacingItemContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#initializeReplacingItem}.
	 * @param ctx the parse tree
	 */
	void exitInitializeReplacingItem(CobolParserCore.InitializeReplacingItemContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#inspectStatement}.
	 * @param ctx the parse tree
	 */
	void enterInspectStatement(CobolParserCore.InspectStatementContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#inspectStatement}.
	 * @param ctx the parse tree
	 */
	void exitInspectStatement(CobolParserCore.InspectStatementContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#inspectTallyingPhrase}.
	 * @param ctx the parse tree
	 */
	void enterInspectTallyingPhrase(CobolParserCore.InspectTallyingPhraseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#inspectTallyingPhrase}.
	 * @param ctx the parse tree
	 */
	void exitInspectTallyingPhrase(CobolParserCore.InspectTallyingPhraseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#inspectTallyingItem}.
	 * @param ctx the parse tree
	 */
	void enterInspectTallyingItem(CobolParserCore.InspectTallyingItemContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#inspectTallyingItem}.
	 * @param ctx the parse tree
	 */
	void exitInspectTallyingItem(CobolParserCore.InspectTallyingItemContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#inspectForClause}.
	 * @param ctx the parse tree
	 */
	void enterInspectForClause(CobolParserCore.InspectForClauseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#inspectForClause}.
	 * @param ctx the parse tree
	 */
	void exitInspectForClause(CobolParserCore.InspectForClauseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#inspectCountPhrase}.
	 * @param ctx the parse tree
	 */
	void enterInspectCountPhrase(CobolParserCore.InspectCountPhraseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#inspectCountPhrase}.
	 * @param ctx the parse tree
	 */
	void exitInspectCountPhrase(CobolParserCore.InspectCountPhraseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#inspectChar}.
	 * @param ctx the parse tree
	 */
	void enterInspectChar(CobolParserCore.InspectCharContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#inspectChar}.
	 * @param ctx the parse tree
	 */
	void exitInspectChar(CobolParserCore.InspectCharContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#inspectReplacingPhrase}.
	 * @param ctx the parse tree
	 */
	void enterInspectReplacingPhrase(CobolParserCore.InspectReplacingPhraseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#inspectReplacingPhrase}.
	 * @param ctx the parse tree
	 */
	void exitInspectReplacingPhrase(CobolParserCore.InspectReplacingPhraseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#inspectReplacingItem}.
	 * @param ctx the parse tree
	 */
	void enterInspectReplacingItem(CobolParserCore.InspectReplacingItemContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#inspectReplacingItem}.
	 * @param ctx the parse tree
	 */
	void exitInspectReplacingItem(CobolParserCore.InspectReplacingItemContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#inspectConvertingPhrase}.
	 * @param ctx the parse tree
	 */
	void enterInspectConvertingPhrase(CobolParserCore.InspectConvertingPhraseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#inspectConvertingPhrase}.
	 * @param ctx the parse tree
	 */
	void exitInspectConvertingPhrase(CobolParserCore.InspectConvertingPhraseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#inspectBeforeAfterPhrase}.
	 * @param ctx the parse tree
	 */
	void enterInspectBeforeAfterPhrase(CobolParserCore.InspectBeforeAfterPhraseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#inspectBeforeAfterPhrase}.
	 * @param ctx the parse tree
	 */
	void exitInspectBeforeAfterPhrase(CobolParserCore.InspectBeforeAfterPhraseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#inspectDelimiters}.
	 * @param ctx the parse tree
	 */
	void enterInspectDelimiters(CobolParserCore.InspectDelimitersContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#inspectDelimiters}.
	 * @param ctx the parse tree
	 */
	void exitInspectDelimiters(CobolParserCore.InspectDelimitersContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#searchStatement}.
	 * @param ctx the parse tree
	 */
	void enterSearchStatement(CobolParserCore.SearchStatementContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#searchStatement}.
	 * @param ctx the parse tree
	 */
	void exitSearchStatement(CobolParserCore.SearchStatementContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#searchWhenClause}.
	 * @param ctx the parse tree
	 */
	void enterSearchWhenClause(CobolParserCore.SearchWhenClauseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#searchWhenClause}.
	 * @param ctx the parse tree
	 */
	void exitSearchWhenClause(CobolParserCore.SearchWhenClauseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#searchAtEndClause}.
	 * @param ctx the parse tree
	 */
	void enterSearchAtEndClause(CobolParserCore.SearchAtEndClauseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#searchAtEndClause}.
	 * @param ctx the parse tree
	 */
	void exitSearchAtEndClause(CobolParserCore.SearchAtEndClauseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#searchAllStatement}.
	 * @param ctx the parse tree
	 */
	void enterSearchAllStatement(CobolParserCore.SearchAllStatementContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#searchAllStatement}.
	 * @param ctx the parse tree
	 */
	void exitSearchAllStatement(CobolParserCore.SearchAllStatementContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#searchAllKeyPhrase}.
	 * @param ctx the parse tree
	 */
	void enterSearchAllKeyPhrase(CobolParserCore.SearchAllKeyPhraseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#searchAllKeyPhrase}.
	 * @param ctx the parse tree
	 */
	void exitSearchAllKeyPhrase(CobolParserCore.SearchAllKeyPhraseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#searchAllWhenClause}.
	 * @param ctx the parse tree
	 */
	void enterSearchAllWhenClause(CobolParserCore.SearchAllWhenClauseContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#searchAllWhenClause}.
	 * @param ctx the parse tree
	 */
	void exitSearchAllWhenClause(CobolParserCore.SearchAllWhenClauseContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#jsonStatement}.
	 * @param ctx the parse tree
	 */
	void enterJsonStatement(CobolParserCore.JsonStatementContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#jsonStatement}.
	 * @param ctx the parse tree
	 */
	void exitJsonStatement(CobolParserCore.JsonStatementContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#xmlStatement}.
	 * @param ctx the parse tree
	 */
	void enterXmlStatement(CobolParserCore.XmlStatementContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#xmlStatement}.
	 * @param ctx the parse tree
	 */
	void exitXmlStatement(CobolParserCore.XmlStatementContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#invokeStatement}.
	 * @param ctx the parse tree
	 */
	void enterInvokeStatement(CobolParserCore.InvokeStatementContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#invokeStatement}.
	 * @param ctx the parse tree
	 */
	void exitInvokeStatement(CobolParserCore.InvokeStatementContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#valueOperand}.
	 * @param ctx the parse tree
	 */
	void enterValueOperand(CobolParserCore.ValueOperandContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#valueOperand}.
	 * @param ctx the parse tree
	 */
	void exitValueOperand(CobolParserCore.ValueOperandContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#valueRange}.
	 * @param ctx the parse tree
	 */
	void enterValueRange(CobolParserCore.ValueRangeContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#valueRange}.
	 * @param ctx the parse tree
	 */
	void exitValueRange(CobolParserCore.ValueRangeContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#booleanLiteral}.
	 * @param ctx the parse tree
	 */
	void enterBooleanLiteral(CobolParserCore.BooleanLiteralContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#booleanLiteral}.
	 * @param ctx the parse tree
	 */
	void exitBooleanLiteral(CobolParserCore.BooleanLiteralContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#signCondition}.
	 * @param ctx the parse tree
	 */
	void enterSignCondition(CobolParserCore.SignConditionContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#signCondition}.
	 * @param ctx the parse tree
	 */
	void exitSignCondition(CobolParserCore.SignConditionContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#condition}.
	 * @param ctx the parse tree
	 */
	void enterCondition(CobolParserCore.ConditionContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#condition}.
	 * @param ctx the parse tree
	 */
	void exitCondition(CobolParserCore.ConditionContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#logicalOrExpression}.
	 * @param ctx the parse tree
	 */
	void enterLogicalOrExpression(CobolParserCore.LogicalOrExpressionContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#logicalOrExpression}.
	 * @param ctx the parse tree
	 */
	void exitLogicalOrExpression(CobolParserCore.LogicalOrExpressionContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#logicalAndExpression}.
	 * @param ctx the parse tree
	 */
	void enterLogicalAndExpression(CobolParserCore.LogicalAndExpressionContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#logicalAndExpression}.
	 * @param ctx the parse tree
	 */
	void exitLogicalAndExpression(CobolParserCore.LogicalAndExpressionContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#unaryLogicalExpression}.
	 * @param ctx the parse tree
	 */
	void enterUnaryLogicalExpression(CobolParserCore.UnaryLogicalExpressionContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#unaryLogicalExpression}.
	 * @param ctx the parse tree
	 */
	void exitUnaryLogicalExpression(CobolParserCore.UnaryLogicalExpressionContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#primaryCondition}.
	 * @param ctx the parse tree
	 */
	void enterPrimaryCondition(CobolParserCore.PrimaryConditionContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#primaryCondition}.
	 * @param ctx the parse tree
	 */
	void exitPrimaryCondition(CobolParserCore.PrimaryConditionContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#comparisonOperand}.
	 * @param ctx the parse tree
	 */
	void enterComparisonOperand(CobolParserCore.ComparisonOperandContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#comparisonOperand}.
	 * @param ctx the parse tree
	 */
	void exitComparisonOperand(CobolParserCore.ComparisonOperandContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#comparisonExpression}.
	 * @param ctx the parse tree
	 */
	void enterComparisonExpression(CobolParserCore.ComparisonExpressionContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#comparisonExpression}.
	 * @param ctx the parse tree
	 */
	void exitComparisonExpression(CobolParserCore.ComparisonExpressionContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#className}.
	 * @param ctx the parse tree
	 */
	void enterClassName(CobolParserCore.ClassNameContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#className}.
	 * @param ctx the parse tree
	 */
	void exitClassName(CobolParserCore.ClassNameContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#comparisonOperator}.
	 * @param ctx the parse tree
	 */
	void enterComparisonOperator(CobolParserCore.ComparisonOperatorContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#comparisonOperator}.
	 * @param ctx the parse tree
	 */
	void exitComparisonOperator(CobolParserCore.ComparisonOperatorContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#arithmeticExpression}.
	 * @param ctx the parse tree
	 */
	void enterArithmeticExpression(CobolParserCore.ArithmeticExpressionContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#arithmeticExpression}.
	 * @param ctx the parse tree
	 */
	void exitArithmeticExpression(CobolParserCore.ArithmeticExpressionContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#additiveExpression}.
	 * @param ctx the parse tree
	 */
	void enterAdditiveExpression(CobolParserCore.AdditiveExpressionContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#additiveExpression}.
	 * @param ctx the parse tree
	 */
	void exitAdditiveExpression(CobolParserCore.AdditiveExpressionContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#addOp}.
	 * @param ctx the parse tree
	 */
	void enterAddOp(CobolParserCore.AddOpContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#addOp}.
	 * @param ctx the parse tree
	 */
	void exitAddOp(CobolParserCore.AddOpContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#multiplicativeExpression}.
	 * @param ctx the parse tree
	 */
	void enterMultiplicativeExpression(CobolParserCore.MultiplicativeExpressionContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#multiplicativeExpression}.
	 * @param ctx the parse tree
	 */
	void exitMultiplicativeExpression(CobolParserCore.MultiplicativeExpressionContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#mulOp}.
	 * @param ctx the parse tree
	 */
	void enterMulOp(CobolParserCore.MulOpContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#mulOp}.
	 * @param ctx the parse tree
	 */
	void exitMulOp(CobolParserCore.MulOpContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#powerExpression}.
	 * @param ctx the parse tree
	 */
	void enterPowerExpression(CobolParserCore.PowerExpressionContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#powerExpression}.
	 * @param ctx the parse tree
	 */
	void exitPowerExpression(CobolParserCore.PowerExpressionContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#unaryExpression}.
	 * @param ctx the parse tree
	 */
	void enterUnaryExpression(CobolParserCore.UnaryExpressionContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#unaryExpression}.
	 * @param ctx the parse tree
	 */
	void exitUnaryExpression(CobolParserCore.UnaryExpressionContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#primaryExpression}.
	 * @param ctx the parse tree
	 */
	void enterPrimaryExpression(CobolParserCore.PrimaryExpressionContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#primaryExpression}.
	 * @param ctx the parse tree
	 */
	void exitPrimaryExpression(CobolParserCore.PrimaryExpressionContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#functionCall}.
	 * @param ctx the parse tree
	 */
	void enterFunctionCall(CobolParserCore.FunctionCallContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#functionCall}.
	 * @param ctx the parse tree
	 */
	void exitFunctionCall(CobolParserCore.FunctionCallContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#literal}.
	 * @param ctx the parse tree
	 */
	void enterLiteral(CobolParserCore.LiteralContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#literal}.
	 * @param ctx the parse tree
	 */
	void exitLiteral(CobolParserCore.LiteralContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#numericLiteral}.
	 * @param ctx the parse tree
	 */
	void enterNumericLiteral(CobolParserCore.NumericLiteralContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#numericLiteral}.
	 * @param ctx the parse tree
	 */
	void exitNumericLiteral(CobolParserCore.NumericLiteralContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#nonNumericLiteral}.
	 * @param ctx the parse tree
	 */
	void enterNonNumericLiteral(CobolParserCore.NonNumericLiteralContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#nonNumericLiteral}.
	 * @param ctx the parse tree
	 */
	void exitNonNumericLiteral(CobolParserCore.NonNumericLiteralContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#signedNumericLiteral}.
	 * @param ctx the parse tree
	 */
	void enterSignedNumericLiteral(CobolParserCore.SignedNumericLiteralContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#signedNumericLiteral}.
	 * @param ctx the parse tree
	 */
	void exitSignedNumericLiteral(CobolParserCore.SignedNumericLiteralContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#numericLiteralCore}.
	 * @param ctx the parse tree
	 */
	void enterNumericLiteralCore(CobolParserCore.NumericLiteralCoreContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#numericLiteralCore}.
	 * @param ctx the parse tree
	 */
	void exitNumericLiteralCore(CobolParserCore.NumericLiteralCoreContext ctx);
	/**
	 * Enter a parse tree produced by {@link CobolParserCore#figurativeConstant}.
	 * @param ctx the parse tree
	 */
	void enterFigurativeConstant(CobolParserCore.FigurativeConstantContext ctx);
	/**
	 * Exit a parse tree produced by {@link CobolParserCore#figurativeConstant}.
	 * @param ctx the parse tree
	 */
	void exitFigurativeConstant(CobolParserCore.FigurativeConstantContext ctx);
}