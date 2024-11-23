using MediatR;

namespace SmallPhotos.Web.Handlers.Models;

public class SigninRequest(string email)
    : IRequest<SigninResponse>
{
    public string Email { get; } = email;
}