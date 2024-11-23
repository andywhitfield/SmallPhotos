using System;
using System.Collections.Generic;
using System.Linq;
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
using SmallPhotos.Model;
using SmallPhotos.Web.Handlers.Models;

namespace SmallPhotos.Web.Handlers;

public class SigninVerifyRequestHandler(ILogger<SigninVerifyRequestHandler> logger, IFido2 fido2,
    IUserAccountRepository userAccountRepository)
    : IRequestHandler<SigninVerifyRequest, bool>
{
    public async Task<bool> Handle(SigninVerifyRequest request, CancellationToken cancellationToken)
    {
        UserAccount? user;
        if ((user = await userAccountRepository.GetUserAccountByEmailAsync(request.Email)) != null)
        {
            if (!await SigninUserAsync(user, request, cancellationToken))
                return false;
        }
        else
        {
            user = await CreateNewUserAsync(request, cancellationToken);
            if (user == null)
                return false;
        }

        List<Claim> claims = [new Claim(ClaimTypes.Name, user.Email!)];
        ClaimsIdentity claimsIdentity = new(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        AuthenticationProperties authProperties = new() { IsPersistent = true };
        await request.HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

        logger.LogTrace("Signed in: {RequestEmail}", request.Email);

        return true;
    }

    private async Task<UserAccount?> CreateNewUserAsync(SigninVerifyRequest request, CancellationToken cancellationToken)
    {
        logger.LogTrace("Creating new user credientials");
        var options = CredentialCreateOptions.FromJson(request.VerifyOptions);

        AuthenticatorAttestationRawResponse? authenticatorAttestationRawResponse = JsonSerializer.Deserialize<AuthenticatorAttestationRawResponse>(request.VerifyResponse);
        if (authenticatorAttestationRawResponse == null)
        {
            logger.LogWarning("Cannot parse signin verify response: {RequestVerifyResponse}", request.VerifyResponse);
            return null;
        }

        logger.LogTrace("Successfully parsed response: {RequestVerifyResponse}", request.VerifyResponse);

        var success = await fido2.MakeNewCredentialAsync(authenticatorAttestationRawResponse, options, (_, _) => Task.FromResult(true), cancellationToken: cancellationToken);
        logger.LogInformation("got success status: {Status} error: {ErrorMessage}", success.Status, success.ErrorMessage);
        if (success.Result == null)
        {
            logger.LogWarning("Could not create new credential: {Status} - {ErrorMessage}", success.Status, success.ErrorMessage);
            return null;
        }

        logger.LogTrace("Got new credential: {Result}", JsonSerializer.Serialize(success.Result));

        return await userAccountRepository.CreateNewUserAsync(request.Email, success.Result.CredentialId,
            success.Result.PublicKey, success.Result.User.Id);
    }

    private async Task<bool> SigninUserAsync(UserAccount user, SigninVerifyRequest request, CancellationToken cancellationToken)
    {
        logger.LogTrace("Checking credientials: {VerifyResponse}", request.VerifyResponse);
        AuthenticatorAssertionRawResponse? authenticatorAssertionRawResponse = JsonSerializer.Deserialize<AuthenticatorAssertionRawResponse>(request.VerifyResponse);
        if (authenticatorAssertionRawResponse == null)
        {
            logger.LogWarning("Cannot parse signin assertion verify response: {VerifyResponse}", request.VerifyResponse);
            return false;
        }
        var options = AssertionOptions.FromJson(request.VerifyOptions);
        var userAccountCredential = await userAccountRepository.GetUserAccountCredentialsAsync(user).FirstOrDefaultAsync(uac => uac.CredentialId.SequenceEqual(authenticatorAssertionRawResponse.Id), cancellationToken);
        if (userAccountCredential == null)
        {
            logger.LogWarning("No credential id [{authenticatorAssertionRawResponseId}] for user [{UserEmail}]", Convert.ToBase64String(authenticatorAssertionRawResponse.Id), user.Email);
            return false;
        }
        
        logger.LogTrace("Making assertion for user [{UserEmail}]", user.Email);
        var res = await fido2.MakeAssertionAsync(authenticatorAssertionRawResponse, options, userAccountCredential.PublicKey, userAccountCredential.SignatureCount, VerifyExistingUserCredentialAsync, cancellationToken: cancellationToken);
        if (!string.IsNullOrEmpty(res.ErrorMessage))
        {
            logger.LogWarning("Signin assertion failed: {Status} - {ErrorMessage}", res.Status, res.ErrorMessage);
            return false;
        }

        logger.LogTrace("Signin success, got response: {Res}", JsonSerializer.Serialize(res) );
        userAccountCredential.SignatureCount = res.Counter;
        await userAccountRepository.UpdateAsync(userAccountCredential);

        return true;
    }

    private async Task<bool> VerifyExistingUserCredentialAsync(IsUserHandleOwnerOfCredentialIdParams credentialIdUserHandleParams, CancellationToken cancellationToken)
    {
        logger.LogInformation("Checking credential {CredentialId} - {UserHandle}", credentialIdUserHandleParams.CredentialId, credentialIdUserHandleParams.UserHandle);
        var userAccountCredentials = await userAccountRepository.GetUserAccountCredentialsByUserHandleAsync(credentialIdUserHandleParams.UserHandle);
        return userAccountCredentials?.CredentialId.SequenceEqual(credentialIdUserHandleParams.CredentialId) ?? false;
    }
}