using System.Text;
using Fido2NetLib;
using Fido2NetLib.Objects;
using MediatR;
using SmallPhotos.Data;
using SmallPhotos.Model;
using SmallPhotos.Web.Handlers.Models;

namespace SmallPhotos.Web.Handlers;

public class SigninRequestHandler(ILogger<SigninRequestHandler> logger,
    IFido2 fido2, IUserAccountRepository userAccountRepository)
    : IRequestHandler<SigninRequest, SigninResponse>
{
    public async Task<SigninResponse> Handle(SigninRequest request, CancellationToken cancellationToken)
    {
        UserAccount? user;
        string options;
        if ((user = await userAccountRepository.GetUserAccountByEmailAsync(request.Email)) != null)
        {
            logger.LogTrace("Found existing user account with email [{RequestEmail}], creating assertion options", request.Email);
            options = fido2.GetAssertionOptions(
                await userAccountRepository
                    .GetUserAccountCredentialsAsync(user)
                    .Select(uac => new PublicKeyCredentialDescriptor(uac.CredentialId))
                    .ToArrayAsync(cancellationToken: cancellationToken),
                UserVerificationRequirement.Discouraged
            ).ToJson();
        }
        else
        {
            logger.LogTrace("Found no user account with email [{RequestEmail}], creating request new creds options", request.Email);
            options = fido2.RequestNewCredential(
                new Fido2User() { Id = Encoding.UTF8.GetBytes(request.Email), Name = request.Email, DisplayName = request.Email },
                [],
                AuthenticatorSelection.Default,
                AttestationConveyancePreference.None
            ).ToJson();
        }

        logger.LogTrace("Created sign in options: {Options}", options);

        return new(user != null, options);
    }
}