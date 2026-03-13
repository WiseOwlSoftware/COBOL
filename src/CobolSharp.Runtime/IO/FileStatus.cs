namespace CobolSharp.Runtime.IO;

/// <summary>
/// Standard COBOL file status codes per ISO/IEC 1989:2023.
/// </summary>
public static class FileStatus
{
    public const string Success = "00";
    public const string AtEnd = "10";
    public const string KeyOutOfSequence = "21";
    public const string DuplicateKey = "22";
    public const string RecordNotFound = "23";
    public const string BoundaryViolation = "24";
    public const string PermanentError = "30";
    public const string FileNotFound = "35";
    public const string PermissionDenied = "37";
    public const string FileAlreadyOpen = "41";
    public const string FileNotOpen = "42";
    public const string NoReadPermission = "43";
    public const string NoWritePermission = "44";
    public const string RecordLengthError = "47";
}
