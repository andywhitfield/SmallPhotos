namespace SmallPhotos.Web.Handlers.Models;

public class SigninResponse(bool isReturningUser, string verifyOptions)
{
    public bool IsReturningUser { get; } = isReturningUser;
    public string VerifyOptions { get; } = verifyOptions;
}
