using CobolSharp.Runtime.IO;
using Xunit;

namespace CobolSharp.Tests.Unit.Runtime;

public class SequentialFileTests : IDisposable
{
    private readonly string _tempDir;

    public SequentialFileTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"CobolSharp_FileTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public void WriteAndReadBack_FixedLength()
    {
        string filePath = Path.Combine(_tempDir, "test.dat");
        int recLen = 20;

        // Write two records
        using (var handler = new SequentialFileHandler(filePath, recLen))
        {
            Assert.Equal(FileStatus.Success, handler.Open(FileOpenMode.Output));

            byte[] rec1 = PadRecord("Record One", recLen);
            Assert.Equal(FileStatus.Success, handler.Write(rec1));

            byte[] rec2 = PadRecord("Record Two", recLen);
            Assert.Equal(FileStatus.Success, handler.Write(rec2));

            Assert.Equal(FileStatus.Success, handler.Close());
        }

        // Read back
        using (var handler = new SequentialFileHandler(filePath, recLen))
        {
            Assert.Equal(FileStatus.Success, handler.Open(FileOpenMode.Input));

            byte[] buffer = new byte[recLen];

            Assert.Equal(FileStatus.Success, handler.ReadNext(buffer));
            Assert.Equal("Record One", GetTrimmedRecord(buffer));

            Assert.Equal(FileStatus.Success, handler.ReadNext(buffer));
            Assert.Equal("Record Two", GetTrimmedRecord(buffer));

            Assert.Equal(FileStatus.AtEnd, handler.ReadNext(buffer));

            Assert.Equal(FileStatus.Success, handler.Close());
        }
    }

    [Fact]
    public void WriteAndReadBack_LineSequential()
    {
        string filePath = Path.Combine(_tempDir, "test.txt");
        int recLen = 30;

        using (var handler = new SequentialFileHandler(filePath, recLen, lineSequential: true))
        {
            Assert.Equal(FileStatus.Success, handler.Open(FileOpenMode.Output));
            Assert.Equal(FileStatus.Success, handler.Write(PadRecord("Line 1", recLen)));
            Assert.Equal(FileStatus.Success, handler.Write(PadRecord("Line 2", recLen)));
            Assert.Equal(FileStatus.Success, handler.Close());
        }

        using (var handler = new SequentialFileHandler(filePath, recLen, lineSequential: true))
        {
            Assert.Equal(FileStatus.Success, handler.Open(FileOpenMode.Input));
            byte[] buffer = new byte[recLen];

            Assert.Equal(FileStatus.Success, handler.ReadNext(buffer));
            Assert.Equal("Line 1", GetTrimmedRecord(buffer));

            Assert.Equal(FileStatus.Success, handler.ReadNext(buffer));
            Assert.Equal("Line 2", GetTrimmedRecord(buffer));

            Assert.Equal(FileStatus.AtEnd, handler.ReadNext(buffer));
            Assert.Equal(FileStatus.Success, handler.Close());
        }
    }

    [Fact]
    public void Open_NonExistentFile_ReturnsFileNotFound()
    {
        string filePath = Path.Combine(_tempDir, "nonexistent.dat");
        using var handler = new SequentialFileHandler(filePath, 20);
        Assert.Equal(FileStatus.FileNotFound, handler.Open(FileOpenMode.Input));
    }

    [Fact]
    public void Open_AlreadyOpen_ReturnsError()
    {
        string filePath = Path.Combine(_tempDir, "test.dat");
        File.WriteAllBytes(filePath, new byte[20]);

        using var handler = new SequentialFileHandler(filePath, 20);
        Assert.Equal(FileStatus.Success, handler.Open(FileOpenMode.Input));
        Assert.Equal(FileStatus.FileAlreadyOpen, handler.Open(FileOpenMode.Input));
    }

    [Fact]
    public void Extend_AppendsToExistingFile()
    {
        string filePath = Path.Combine(_tempDir, "test.txt");
        int recLen = 20;

        // Write initial record
        using (var handler = new SequentialFileHandler(filePath, recLen, lineSequential: true))
        {
            handler.Open(FileOpenMode.Output);
            handler.Write(PadRecord("First", recLen));
            handler.Close();
        }

        // Extend with another record
        using (var handler = new SequentialFileHandler(filePath, recLen, lineSequential: true))
        {
            handler.Open(FileOpenMode.Extend);
            handler.Write(PadRecord("Second", recLen));
            handler.Close();
        }

        // Read both
        using (var handler = new SequentialFileHandler(filePath, recLen, lineSequential: true))
        {
            handler.Open(FileOpenMode.Input);
            byte[] buffer = new byte[recLen];

            Assert.Equal(FileStatus.Success, handler.ReadNext(buffer));
            Assert.Equal("First", GetTrimmedRecord(buffer));

            Assert.Equal(FileStatus.Success, handler.ReadNext(buffer));
            Assert.Equal("Second", GetTrimmedRecord(buffer));

            handler.Close();
        }
    }

    [Fact]
    public void FileManager_RegisterAndAccess()
    {
        string filePath = Path.Combine(_tempDir, "managed.dat");
        using var manager = new CobolFileManager();
        manager.RegisterFile("MY-FILE", new SequentialFileHandler(filePath, 10));

        Assert.Equal(FileStatus.Success, manager.Open("MY-FILE", FileOpenMode.Output));
        Assert.Equal(FileStatus.Success, manager.Write("MY-FILE", PadRecord("Test", 10)));
        Assert.Equal(FileStatus.Success, manager.Close("MY-FILE"));
    }

    private static byte[] PadRecord(string text, int length)
    {
        byte[] record = new byte[length];
        Array.Fill(record, (byte)' ');
        byte[] textBytes = System.Text.Encoding.ASCII.GetBytes(text);
        Array.Copy(textBytes, record, Math.Min(textBytes.Length, length));
        return record;
    }

    private static string GetTrimmedRecord(byte[] record) =>
        System.Text.Encoding.ASCII.GetString(record).TrimEnd();
}
