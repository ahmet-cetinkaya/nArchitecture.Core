namespace NArchitecture.Core.Security.Abstractions.Cryptography.Generation;

public interface ICodeGenerator
{
    string GenerateNumeric(int length, byte[]? seed = null);
    string GenerateAlphanumeric(int length, byte[]? seed = null);
    string GenerateBase64(int byteLength, byte[]? seed = null);
}
