
namespace GrouveeImageDownloader;
using System;
using System.Linq;
using System.Threading.Tasks;

using GrouveeCollectionParser;
using GiantBomb.Api;

class GiantBombImageFetcher : ImageFetcher
{
    private readonly int apiCallDelay = 1500;
    private readonly GiantBombRestClient giantBombClient;
    public GiantBombImageFetcher(string? token)
    {
        this.giantBombClient = new GiantBombRestClient(token);
    }

    public async Task GetCover(string grouveeFile, string shelf)
    {
        var grouveeCollection = (await GrouveeCollection.ImportAsync(grouveeFile)).ToList();
        grouveeCollection = grouveeCollection.Where(x => x.Shelves.Any(y => y.Name.Equals(shelf))).ToList();

        foreach (GrouveeGame grouveeGame in grouveeCollection)
        {
            try
            {
                var game = giantBombClient.GetGame(int.Parse(grouveeGame.GiantBombId));
                var year = 0;
                if (game.OriginalReleaseDate != null)
                {
                    DateTime releaseDate = game.OriginalReleaseDate ?? new DateTime();
                    year = releaseDate.Year;
                }
                else
                {
                    year = game.ExpectedReleaseYear ?? 0;
                }

                await DownloadImageAsync("imagesGiantBomb", year + "_" + game.Name, new Uri(game.Image.SuperUrl));
                System.Threading.Thread.Sleep(apiCallDelay);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error with {grouveeGame.Name} : {e.Message} \n");
            }
        }
    }
}

