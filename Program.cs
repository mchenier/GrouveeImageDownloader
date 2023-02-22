
namespace GrouveeImageDownloader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.Text.RegularExpressions;

using GrouveeCollectionParser;
using IGDB;
using IGDB.Models;
using System.Net;
using Microsoft.Extensions.Configuration;



class Program
{
    struct ProgramOptions
    {
        public bool DownloadScreenshot;
        public bool DownloadCover;
        public string ShelfName;
        public bool UseIgdb;
        public bool UseGiantBomb;
        public string GrouveeExportCSV;
    }

    static ProgramOptions ProcessArgs(string[] args)
    {
        var options = new ProgramOptions();
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--help":
                    Console.WriteLine("Usage: GrouveeImageDownloader run [options] [file_path]");
                    Console.WriteLine("Options:");
                    Console.WriteLine("  --grouveeExport <name>     Path to your Grouvee export csv file.");
                    Console.WriteLine("  --screenshot               Download screenshot. (only IGDB)");
                    Console.WriteLine("  --cover                    Download game cover.");
                    Console.WriteLine("  --shelf <name>             The name of the shelf to use. Default is \"Played\".");
                    Console.WriteLine("  --igdb                     Download from IGDB. You can use both igdb and giantbomb at the same time.");
                    Console.WriteLine("  --giantbomb                Download from GiantBomb. You can use both igdb and giantbomb at the same time.");
                    Console.WriteLine("  --help                     Display this help message.");
                    options.UseGiantBomb = false;
                    options.UseIgdb = false;
                    return options;
                case "--grouveeExport":
                    i++;
                    options.GrouveeExportCSV = args[i];
                    break;
                case "--screenshot":
                    options.DownloadScreenshot = true;
                    break;
                case "--cover":
                    options.DownloadCover = true;
                    break;
                case "--shelf":
                    if (i + 1 < args.Length)
                    {
                        options.ShelfName = args[++i];
                    }
                    else
                    {
                        Console.WriteLine("Error: Shelf name not specified.");
                    }
                    break;
                case "--igdb":
                    options.UseIgdb = true;
                    break;
                case "--giantbomb":
                    options.UseGiantBomb = true;
                    break;
                default:
                    Console.WriteLine($"Error: Unknown option {args[i]}");
                    break;
            }
        }

        return options;
    }


    static async Task Main(string[] args)
    {
        var options = ProcessArgs(args);

        var config = new ConfigurationBuilder().AddIniFile("config.ini").Build();

        var securityConfig = config.GetSection("Security");
        var IGDB_CLIENT_ID = securityConfig["IGDB_CLIENT_ID"];
        var IGDB_CLIENT_SECRET = securityConfig["IGDB_CLIENT_SECRET"];
        var GIANTBOMB_API_TOKEN = securityConfig["GIANTBOMB_API_TOKEN"];

        if (options.UseIgdb)
        {
            var fetcherIGDB = new IGDBImageFetcher(IGDB_CLIENT_ID, IGDB_CLIENT_SECRET);
            await fetcherIGDB.GetImageAndScreenshot(options.GrouveeExportCSV, options.ShelfName, options.DownloadCover, options.DownloadScreenshot);
        }

        if (options.UseGiantBomb)
        {
            var fetcherGiantBomb = new GiantBombImageFetcher(GIANTBOMB_API_TOKEN);
            await fetcherGiantBomb.GetCover(options.GrouveeExportCSV, options.ShelfName);
        }
    }





}

