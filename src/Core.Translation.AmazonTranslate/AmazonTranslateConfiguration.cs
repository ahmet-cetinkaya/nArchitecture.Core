using Amazon;

namespace NArchitecture.Core.Translation.AmazonTranslate;

/// <summary>
/// Represents configuration settings required for Amazon Translate service integration.
/// </summary>
/// <param name="AccessKey">AWS Access Key used for authentication with Amazon Translate service.</param>
/// <param name="SecretKey">AWS Secret Key used for secure authentication with Amazon Translate service.</param>
/// <param name="RegionEndpoint">AWS Region Endpoint where the Amazon Translate service is hosted.</param>
public readonly record struct AmazonTranslateConfiguration(string AccessKey, string SecretKey, RegionEndpoint RegionEndpoint);
