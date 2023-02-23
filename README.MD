# Description 

Console application that reads your Grouvee export csv and fetch image from GiantBomb Api and IGDB Api.
The cover from GiantBomb is more reliable since with your Grouvee export it includes the ID of each game. 
For IGDB there is a search involed to find the best match with the game name and the release date.
There maybe some mismatch with that algorythm but you should see the logs from the console and verify by yourself for the rare case it occurs.
You can only get screenshot from IGDB since GiantBomb doesn't tag the image as screenshot.

# Usage

## Edit config.ini

Get yourself a IGDB clientId and secret needed for the IGDB Api. (https://api-docs.igdb.com/#getting-started)
Get yourself a GiantBomb token for the GiantBomb Api. (https://www.giantbomb.com/api/)

config.ini should look like this.

    [Security]
    IGDB_CLIENT_ID=<yourKey>
    IGDB_CLIENT_SECRET=<yourSecret>
    GIANTBOMB_API_TOKEN=<yourToken>


## Launch the app

  Usage: GrouveeImageDownloader run [options] [file_path]
  Options:
    --grouveeExport <name>     Path to your Grouvee export csv file.
    --screenshot               Download screenshot. (only IGDB)
    --cover                    Download game cover.
    --shelf <name>             The name of the shelf to use. Default is "Played".
    --igdb                     Download from IGDB. You can use both igdb and giantbomb at the same time.
    --giantbomb                Download from GiantBomb. You can use both igdb and giantbomb at the same time.
    --help                     Display this help message.

  Example:
  
    GrouveeImageDownloader.exe --grouveeExport test.csv --cover --screenshot --shelf Played --giantbomb --igdb