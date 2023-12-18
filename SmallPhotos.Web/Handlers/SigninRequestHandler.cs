using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fido2NetLib;
using Fido2NetLib.Objects;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmallPhotos.Data;
using SmallPhotos.Model;
using SmallPhotos.Web.Handlers.Models;

namespace SmallPhotos.Web.Handlers;

public class SigninRequestHandler(ILogger<SigninRequestHandler> logger, IConfiguration configuration,
    IFido2 fido2, IUserAccountRepository userAccountRepository)
    : IRequestHandler<SigninRequest, SigninResponse>
{
    public async Task<SigninResponse> Handle(SigninRequest request, CancellationToken cancellationToken)
    {
        UserAccount? user;
        string options;
        if ((user = await userAccountRepository.GetUserAccountByEmailAsync(request.Email)) != null)
        {
            logger.LogTrace($"Found existing user account with email [{request.Email}], creating assertion options");
            options = fido2.GetAssertionOptions(
                await userAccountRepository
                    .GetUserAccountCredentialsAsync(user)
                    .Select(uac => new PublicKeyCredentialDescriptor(uac.CredentialId))
                    .ToArrayAsync(cancellationToken: cancellationToken),
                UserVerificationRequirement.Discouraged,
                new AuthenticationExtensionsClientInputs()
                {
                    Extensions = true,
                    UserVerificationMethod = true,
                    AppID = configuration.GetValue<string>("FidoOrigins")
                }
            ).ToJson();
        }
        else
        {
            logger.LogTrace($"Found no user account with email [{request.Email}], creating request new creds options");
            options = fido2.RequestNewCredential(
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
            ).ToJson();
        }

        logger.LogTrace($"Created sign in options: {options}");

        return new(user != null, options);
    }
}