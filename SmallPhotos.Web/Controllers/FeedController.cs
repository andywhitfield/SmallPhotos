using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmallPhotos.Data;
using SmallPhotos.Feed;

namespace SmallPhotos.Web.Controllers;

[Route("api/[controller]")]
[Produces(MediaTypeNames.Application.Xml)]
[AllowAnonymous]
public class FeedController(
    ILogger<FeedController> logger,
    TimeProvider timeProvider,
    IUserAccountRepository userAccountRepository,
    IUserFeedRepository userFeedRepository,
    IFeedGenerator feedGenerator
) : ControllerBase
{
    private readonly TimeSpan _feedRefreshPeriod = TimeSpan.FromDays(1);

    [HttpGet("{feedIdentifier}")]
    public async Task<IActionResult> Index([FromRoute, Required] string feedIdentifier)
    {
        var userFeed = await userFeedRepository.FindByIdentifierAsync(feedIdentifier);
        if (userFeed == null)
        {
            logger.LogInformation("No user feed found with identifier: {FeedIdentifier}", feedIdentifier);
            return NotFound();
        }

        var user = await userAccountRepository.GetAsync(userFeed.UserAccountId);
        if (user == null)
        {
            logger.LogInformation("No user account found, associated with feed identifier: {FeedIdentifier}", feedIdentifier);
            return NotFound();
        }

        UserFeedDetails userFeedDetails;
        if (userFeed.LastUpdateDateTime != null && timeProvider.GetUtcNow() - userFeed.LastUpdateDateTime <= _feedRefreshPeriod)
        {
            // only refresh recently uploaded / reminisce photos at most once per day
            userFeedDetails = userFeed.FeedDetails.ToUserFeedDetails();
        }
        else
        {
            userFeedDetails = new([], []); // TODO

            await userFeedRepository.SaveAsync(userFeed);
        }

        var (content, contentType, encoding) = await feedGenerator.GenerateFeedAsync($"{Request.Scheme}://{Request.Host}", user, userFeedDetails, userFeed);
        return Content(content, contentType, encoding);
    }
}
