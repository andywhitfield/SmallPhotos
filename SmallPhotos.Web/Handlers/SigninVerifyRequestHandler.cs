using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fido2NetLib;
using Fido2NetLib.Objects;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Logging;
using SmallPhotos.Data;
using SmallPhotos.Web.Handlers.Models;

namespace SmallPhotos.Web.Handlers;

public class SigninVerifyRequestHandler(ILogger<SigninVerifyRequestHandler> logger, IFido2 fido2,
    IUserAccountRepository userAccountRepository)
    : IRequestHandler<SigninVerifyRequest, bool>
{
    public async Task<bool> Handle(SigninVerifyRequest request, CancellationToken cancellationToken)
    {
        // TODO: for now, assume registering new user...
        logger.LogTrace("Creating new user credientials");
        var options = CredentialCreateOptions.FromJson(request.VerifyOptions);

        AuthenticatorAttestationRawResponse? authenticatorAttestationRawResponse = JsonSerializer.Deserialize<AuthenticatorAttestationRawResponse>(request.VerifyResponse);
        if (authenticatorAttestationRawResponse == null)
        {
            logger.LogWarning($"Cannot parse signin verify response: {request.VerifyResponse}");
            return false;
        }

        logger.LogTrace($"Successfully parsed response: {request.VerifyResponse}");

        var success = await fido2.MakeNewCredentialAsync(authenticatorAttestationRawResponse, options, VerifyNewUserCredentialAsync, cancellationToken: cancellationToken);
        logger.LogInformation($"got success status: {success.Status} error: {success.ErrorMessage}");
        if (success.Result == null)
        {
            logger.LogWarning($"Could not create new credential: {success.Status} - {success.ErrorMessage}");
            return false;
        }

        logger.LogTrace($"Got new credential: {JsonSerializer.Serialize(success.Result)}");

        await userAccountRepository.CreateNewUserAsync(request.Email, success.Result.CredentialId,
            success.Result.PublicKey, success.Result.User.Id);

        List<Claim> claims = [new Claim(ClaimTypes.Name, request.Email)];
        ClaimsIdentity claimsIdentity = new(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        AuthenticationProperties authProperties = new() { IsPersistent = true };
        await request.HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

        logger.LogTrace($"Signed in: {request.Email}");

        return true;
    }

    private Task<bool> VerifyNewUserCredentialAsync(IsCredentialIdUniqueToUserParams credentialIdUserParams, CancellationToken cancellationToken)
    {
        logger.LogInformation($"Checking credential is unique: {Convert.ToBase64String(credentialIdUserParams.CredentialId)}");
        // TODO: check no account already exists with this CredentialId
        return Task.FromResult(true);
    }
}