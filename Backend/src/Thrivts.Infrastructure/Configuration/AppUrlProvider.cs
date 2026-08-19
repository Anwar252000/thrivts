using Microsoft.Extensions.Configuration;
using Thrivts.Application.Common.Interfaces;

namespace Thrivts.Infrastructure.Configuration;

public class AppUrlProvider : IAppUrlProvider
{
    public string FrontendBaseUrl { get; }

    public AppUrlProvider(IConfiguration configuration)
    {
        FrontendBaseUrl = configuration["App:FrontendBaseUrl"]
            ?? throw new InvalidOperationException("App:FrontendBaseUrl is not configured.");
    }
}
