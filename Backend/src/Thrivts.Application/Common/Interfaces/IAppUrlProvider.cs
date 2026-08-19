namespace Thrivts.Application.Common.Interfaces;

/// <summary>Resolves the frontend's public base URL for links the API emails out (e.g. the
/// password-recovery redirect) — kept out of Application as raw IConfiguration so this layer
/// doesn't take a dependency on the configuration package.</summary>
public interface IAppUrlProvider
{
    string FrontendBaseUrl { get; }
}
