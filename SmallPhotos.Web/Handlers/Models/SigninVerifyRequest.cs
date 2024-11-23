using MediatR;
using Microsoft.AspNetCore.Http;

namespace SmallPhotos.Web.Handlers.Models;

public class SigninVerifyRequest(HttpContext httpContext, string email, string verifyOptions, string verifyResponse)
    : IRequest<bool>
{
    public HttpContext HttpContext { get; } = httpContext;
    public string Email { get; } = email;
    public string VerifyOptions { get; } = verifyOptions;
    public string VerifyResponse { get; } = verifyResponse;
}