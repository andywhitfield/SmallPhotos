using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fido2NetLib;
using Fido2NetLib.Objects;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmallPhotos.Web.Handlers.Models;

namespace SmallPhotos.Web.Handlers;

public class SigninRequestHandler(ILogger<SigninRequestHandler> logger, IFido2 fido2, IConfiguration configuration)
    : IRequestHandler<SigninRequest, SigninResponse>
{
    public Task<SigninResponse> Handle(SigninRequest request, CancellationToken cancellationToken)
    {
        // TODO: for now, assume registering new user...
        var options = fido2.RequestNewCredential(
            new Fido2User() { Id = Encoding.UTF8.GetBytes(request.Email), Name = request.Email, DisplayName = request.Email },
            [],
            AuthenticatorSelection.Default,
            AttestationConveyancePreference.None,
            new()
            {
                Extensions = true,
                UserVerificationMethod = true,
                AppID = configuration.GetValue<string>("FidoOrigins")
            }
        );

        logger.LogTrace($"Created sign in options: {JsonSerializer.Serialize(options)}");

        return Task.FromResult(new SigninResponse(false, options.ToJson()));
    }
}