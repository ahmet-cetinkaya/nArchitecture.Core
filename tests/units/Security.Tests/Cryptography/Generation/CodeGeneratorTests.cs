using NArchitecture.Core.Security.Cryptography.Generation;
using Shouldly;

namespace NArchitecture.Core.Security.Tests.Cryptography.Generation;

[Trait("Category", "Security")]
[Trait("Class", "CodeGenerator")]
public class CodeGeneratorTests
{
    private readonly CodeGenerator _codeGenerator;

    public CodeGeneratorTests()
    {
        _codeGenerator = new();
    }

    [Theory(DisplayName = "GenerateNumeric should create numeric string with exact length")]
    [InlineData(6)]
    [InlineData(10)]
    [InlineData(1)]
    public void GenerateNumeric_ShouldCreateValidNumericString_WithGivenLength(int length)
    {
        // Act
        string result = _codeGenerator.GenerateNumeric(length);

        // Assert
        result.Length.ShouldBe(length);
        result.All(char.IsDigit).ShouldBeTrue();
    }

    [Theory(DisplayName = "GenerateNumeric should create different codes for different seeds")]
    [InlineData(6)]
    public void GenerateNumeric_ShouldCreateDifferentCodes_WithDifferentSeeds(int length)
    {
        // Arrange
        byte[] seed1 = [1, 2, 3];
        byte[] seed2 = [4, 5, 6];

        // Act
        string result1 = _codeGenerator.GenerateNumeric(length, seed1);
        string result2 = _codeGenerator.GenerateNumeric(length, seed2);

        // Assert
        result1.ShouldNotBe(result2);
    }

    [Theory(DisplayName = "GenerateNumeric should throw for invalid length")]
    [InlineData(0)]
    [InlineData(-1)]
    public void GenerateNumeric_ShouldThrowArgumentException_WhenLengthIsInvalid(int length)
    {
        // Act & Assert
        _ = Should.Throw<ArgumentException>(() => _codeGenerator.GenerateNumeric(length));
    }

    [Theory(DisplayName = "GenerateAlphanumeric should create valid string with exact length")]
    [InlineData(8)]
    [InlineData(12)]
    public void GenerateAlphanumeric_ShouldCreateValidString_WithGivenLength(int length)
    {
        // Act
        string result = _codeGenerator.GenerateAlphanumeric(length);

        // Assert
        result.Length.ShouldBe(length);
        result.All(char.IsLetterOrDigit).ShouldBeTrue();
        result.All(c => char.IsUpper(c) || char.IsDigit(c)).ShouldBeTrue();
    }

    [Theory(DisplayName = "GenerateAlphanumeric should create different codes for different seeds")]
    [InlineData(8)]
    public void GenerateAlphanumeric_ShouldCreateDifferentCodes_WithDifferentSeeds(int length)
    {
        // Arrange
        byte[] seed1 = [1, 2, 3];
        byte[] seed2 = [4, 5, 6];

        // Act
        string result1 = _codeGenerator.GenerateAlphanumeric(length, seed1);
        string result2 = _codeGenerator.GenerateAlphanumeric(length, seed2);

        // Assert
        result1.ShouldNotBe(result2);
    }

    [Theory(DisplayName = "GenerateBase64 should create valid base64 string")]
    [InlineData(6)]
    [InlineData(12)]
    public void GenerateBase64_ShouldCreateValidBase64String_WithGivenByteLength(int byteLength)
    {
        // Act
        string result = _codeGenerator.GenerateBase64(byteLength);

        // Assert
        _ = Should.NotThrow(() => Convert.FromBase64String(result));
        Convert.FromBase64String(result).Length.ShouldBe(byteLength);
    }

    [Theory(DisplayName = "GenerateBase64 should create consistent length output for same input length")]
    [InlineData(6)]
    [InlineData(12)]
    public void GenerateBase64_ShouldCreateConsistentLengthOutput_ForSameInputLength(int byteLength)
    {
        // Act
        string result1 = _codeGenerator.GenerateBase64(byteLength);
        string result2 = _codeGenerator.GenerateBase64(byteLength);

        // Assert
        result1.Length.ShouldBe(result2.Length);
    }

    [Theory(DisplayName = "GenerateBase64 should create different outputs with different seeds")]
    [InlineData(6)]
    public void GenerateBase64_ShouldCreateDifferentOutputs_WithDifferentSeeds(int byteLength)
    {
        // Arrange
        byte[] seed1 = [1, 2, 3];
        byte[] seed2 = [4, 5, 6];

        // Act
        string result1 = _codeGenerator.GenerateBase64(byteLength, seed1);
        string result2 = _codeGenerator.GenerateBase64(byteLength, seed2);

        // Assert
        result1.ShouldNotBe(result2);
    }

    [Theory(DisplayName = "GenerateBase64 should throw for invalid byte length")]
    [InlineData(0)]
    [InlineData(-1)]
    public void GenerateBase64_ShouldThrowArgumentException_WhenByteLengthIsInvalid(int byteLength)
    {
        // Act & Assert
        _ = Should.Throw<ArgumentException>(() => _codeGenerator.GenerateBase64(byteLength));
    }
}
