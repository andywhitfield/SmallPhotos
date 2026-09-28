using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmallPhotos.Data;
using SmallPhotos.Feed;
using SmallPhotos.Web.Handlers.Models;

namespace SmallPhotos.Web.Controllers;

[Route("api/[controller]")]
[Produces(MediaTypeNames.Application.Xml)]
[AllowAnonymous]
public class FeedController(
    ILogger<FeedController> logger,
    TimeProvider timeProvider,
    IUserAccountRepository userAccountRepository,
    IUserFeedRepository userFeedRepository,
    IPhotoRepository photoRepository,
    IFeedGenerator feedGenerator,
    IMediator mediator
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
            logger.LogDebug("Feed {UserFeedId} has been updated within the last day, not refreshing", userFeed.UserFeedId);
            userFeedDetails = userFeed.FeedDetails.ToUserFeedDetails();
        }
        else
        {
            logger.LogDebug("Feed {UserFeedId} has not been updated within the last day, refreshing", userFeed.UserFeedId);
            var now = timeProvider.GetUtcNow();
            var recentlyAdded = userFeed.LastUpdateDateTime != null ? await photoRepository.GetIdsByDateRangeAsync(user, userFeed.LastUpdateDateTime.Value, now.DateTime).ToArrayAsync() : [];
            var reminisceResponse = await mediator.Send(new ReminiscePageRequest(new(new ClaimsIdentity([new(ClaimTypes.Name, user.Email ?? "")]))));
            if (!reminisceResponse.IsUserValid)
                logger.LogWarning("Could not generate as reminisce response for the given user: {UserAccountId}", user.UserAccountId);

            var reminiscePhotoIds = reminisceResponse.IsUserValid ? reminisceResponse.Photos.Select(p => p.PhotoId).ToArray() : [];

            userFeedDetails = new(recentlyAdded, reminiscePhotoIds);
            logger.LogDebug("Got feed details for user {UserAccountId} feed {UserFeedId}: recently added: [{Recents}], reminisce: [{Reminisce}]",
                user.UserAccountId, userFeed.UserFeedId, string.Join(',', recentlyAdded), string.Join(',', reminiscePhotoIds));

            userFeed.FeedDetails = userFeedDetails.ToJson();
            await userFeedRepository.SaveAsync(userFeed, now.DateTime);
        }

        var (content, contentType, encoding) = await feedGenerator.GenerateFeedAsync($"{Request.Scheme}://{Request.Host}", user, userFeedDetails, userFeed);
        return Content(content, contentType, encoding);
    }
}
