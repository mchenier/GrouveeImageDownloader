
namespace GrouveeImageDownloader;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Globalization;
using System.Text.RegularExpressions;

using GrouveeCollectionParser;
using IGDB;
using IGDB.Models;

class IGDBImageFetcher : ImageFetcher
{
    private readonly IGDBClient igdb;
    private readonly int apiCallDelay = 1500;
    public IGDBImageFetcher(string? clientId, string? secret)
    {
        this.igdb = new IGDBClient(clientId, secret);
    }

    public async Task GetImageAndScreenshot(string grouveeFile, string shelf, bool downloadCover, bool downloadScreenshot)
    {
        var grouveeCollection = (await GrouveeCollection.ImportAsync(grouveeFile)).ToList();
        grouveeCollection = grouveeCollection.Where(x => x.Shelves.Any(y => y.Name.Equals(shelf))).ToList();

        foreach (GrouveeGame grouveeGame in grouveeCollection)
        {
            var game = await FindGame(grouveeGame);
            if (game != null)
            {
                if (downloadScreenshot) await DownloadScreenshot(grouveeGame, game);
                if (downloadCover) await DownloadCover(grouveeGame, game);
            }

            System.Threading.Thread.Sleep(apiCallDelay);
        }
    }

    private async Task DownloadCover(GrouveeGame grouveeGame, Game game)
    {
        if (game.Cover != null)
        {
            var cover = (await igdb.QueryAsync<Cover>(IGDBClient.Endpoints.Covers, query: $"fields image_id; where id = {game.Cover.Id};")).FirstOrDefault();

            var coverImageId = cover?.ImageId;
            if (coverImageId != null)
            {
                var coverUrl = IGDB.ImageHelper.GetImageUrl(imageId: coverImageId, size: ImageSize.HD1080, retina: false);
                await DownloadImageAsync("imagesIGDB", GetReleaseDate(grouveeGame).Year + "_" + game.Slug, new Uri("https:" + coverUrl));
            }
            else
            {
                Console.Write($"Not found Cover {grouveeGame.Name} : {game.Name} \n");
            }
        }

    }

    private async Task DownloadScreenshot(GrouveeGame grouveeGame, Game game)
    {

        if (game.Screenshots != null)
        {
            var screenshot = (await igdb.QueryAsync<Cover>(IGDBClient.Endpoints.Screenshots, query: $"fields image_id; where id = {game.Screenshots.Ids.First()};")).FirstOrDefault();

            var screenshotImageId = screenshot?.ImageId;
            if (screenshotImageId != null)
            {
                var screenshotUrl = IGDB.ImageHelper.GetImageUrl(imageId: screenshotImageId, size: ImageSize.HD1080, retina: false);
                await DownloadImageAsync("screenshotsIGDB", GetReleaseDate(grouveeGame).Year + "_" + game.Slug, new Uri("https:" + screenshotUrl));
            }
            else
            {
                Console.Write($"Not found Screenshots {grouveeGame.Name} : {game.Name} \n");
            }

        }

    }

    private int ReleaseTimeBefore(DateTime releaseDate, int daysBefore)
    {
        TimeSpan t = releaseDate.Subtract(TimeSpan.FromDays(daysBefore)) - new DateTime(1970, 1, 1);
        return (int)t.TotalSeconds;
    }

    private int ReleaseTimeAfter(DateTime releaseDate, int daysBefore)
    {
        TimeSpan t = releaseDate.Add(TimeSpan.FromDays(daysBefore)) - new DateTime(1970, 1, 1);
        return (int)t.TotalSeconds;
    }

    private DateTime GetReleaseDate(GrouveeGame grouveeGame)
    {
        IFormatProvider culture = new CultureInfo("en-US", true);
        Regex dateRegEx = new Regex(@"(?<year>^[0-9]{4})");
        var matche = dateRegEx.Match(grouveeGame.ReleaseDate);
        var releaseYear = matche.Groups["year"].Value;
        return DateTime.ParseExact(releaseYear, "yyyy", culture, DateTimeStyles.None);
    }

    private async Task<Game?> ExactMatch(GrouveeGame grouveeGame)
    {
        try
        {
            var games = await this.igdb.QueryAsync<Game>(IGDBClient.Endpoints.Games, query: $"fields *; where name ~\"{grouveeGame.Name}\";");
            Game? game = null;
            if (games.Count() == 1)
            {
                game = games.First();
            }
            return game;
        }
        catch (Exception e)
        {
            Console.Write(e.Message);
            return null;

        }
    }

    private async Task<Game?> ExactMatchWithReleaseDate(GrouveeGame grouveeGame, DateTime releaseDate)
    {
        int releaseTimeBefore = ReleaseTimeBefore(releaseDate, 30);
        int releaseTimeAfter = ReleaseTimeAfter(releaseDate, 365 * 1);

        var command = $"fields *; where name ~\"{grouveeGame.Name}\" & release_dates.date > {releaseTimeBefore} & release_dates.date < {releaseTimeAfter};";
        var games = await this.igdb.QueryAsync<Game>(IGDBClient.Endpoints.Games, query: command);
        Game? game = null;
        if (games.Count() == 1)
        {
            game = games.First();
        }
        return game;
    }

    private async Task<Game?> RecursiveSearchExtendingReleaseYear(GrouveeGame grouveeGame, DateTime releaseDate)
    {
        int yearBuffer = 1;
        bool gameFound = false;
        Game? game = null;
        while (gameFound == false)
        {
            int releaseTimeBeforeRecursive = ReleaseTimeBefore(releaseDate, 365 * yearBuffer);
            int releaseTimeAfterRecursive = ReleaseTimeAfter(releaseDate, 365 * yearBuffer);
            var gamesRecursive = await igdb.QueryAsync<Game>(IGDBClient.Endpoints.Games, query: $"fields *; search \"{grouveeGame.Name}\"; where release_dates.date > {releaseTimeBeforeRecursive} & release_dates.date < {releaseTimeAfterRecursive};");
            var platformHandheldElectronicLCD = 411;
            game = gamesRecursive.FirstOrDefault(x => x.Category == 0 && x.Platforms.Ids.Any(y => y != platformHandheldElectronicLCD));
            if (game == null)
            {
                game = gamesRecursive.FirstOrDefault();
            }

            if (game != null)
            {
                gameFound = true;
                if (!grouveeGame.Name.Equals(game.Name, StringComparison.CurrentCultureIgnoreCase))
                {
                    Console.Write("Not equal Recursive " + yearBuffer + " year buffer " + grouveeGame.Name + ":" + grouveeGame.ReleaseDate + "<@>" + game.Name + "\n");
                }
            }
            if (yearBuffer >= 5)
            {
                Console.Write($"Not found game {grouveeGame.Name} : {grouveeGame.ReleaseDate} : {releaseTimeBeforeRecursive} : {releaseTimeAfterRecursive} \n");
                break;
            }
            ++yearBuffer;
        }

        return game;
    }

    private async Task<Game?> FindGame(GrouveeGame grouveeGame)
    {
        var releaseDate = GetReleaseDate(grouveeGame);

        var game = await ExactMatch(grouveeGame);

        if (game == null)
        {
            game = await ExactMatchWithReleaseDate(grouveeGame, releaseDate);
        }

        if (game == null)
        {
            game = await RecursiveSearchExtendingReleaseYear(grouveeGame, releaseDate);
        }

        return game;
    }

}

