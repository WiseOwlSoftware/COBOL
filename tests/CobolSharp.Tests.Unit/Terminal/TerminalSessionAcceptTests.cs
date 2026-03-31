using Xunit;
using CobolSharp.Runtime.Terminal;
using CobolSharp.Compiler.Semantics.Bound;

namespace CobolSharp.Tests.Unit.Terminal;

public sealed class TerminalSessionAcceptTests
{
    [Fact]
    public void SimpleInput_ReturnsText()
    {
    }

    [Fact]
    public void SecureField_MasksCharacters()
    {
    }

    [Fact]
    public void FullField_RejectsShortInput()
    {
    }

    [Fact]
    public void RequiredField_RejectsEmpty()
    {
    }

    [Fact]
    public void Backspace_DeletesCharacter()
    {
    }

    [Fact]
    public void LeftRight_MoveCursor()
    {
    }

    [Fact]
    public void Enter_CompletesField()
    {
    }

    [Fact]
    public void FunctionKey_CompletesField()
    {
    }
}
