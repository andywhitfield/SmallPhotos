using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using SmallPhotos.Data;
using SmallPhotos.Web.Handlers.Models;

namespace SmallPhotos.Web.Handlers
{
    public class SignedInRequestHandler : IRequestHandler<SignedInRequest, string>
    {
        private readonly IUserAccountRepository _userAccountRepository;

        public SignedInRequestHandler(IUserAccountRepository userAccountRepository) => _userAccountRepository = userAccountRepository;

        public async Task<string> Handle(SignedInRequest request, CancellationToken cancellationToken)
        {
            if (await _userAccountRepository.GetUserAccountOrNullAsync(request.User) == null)
                await _userAccountRepository.CreateNewUserAsync(request.User);
            
            if (!string.IsNullOrEmpty(request.ReturnUrl) && Uri.TryCreate(request.ReturnUrl, UriKind.Relative, out var redirectUri))
                return redirectUri.ToString();

            return "~/";
        }
    }
}