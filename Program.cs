using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using FemRec2023;
using FemRec2023.Classes;
using FemRec2023.Classes.DBs;
using FemRec2023.Classes.DBs.DBClasses;

namespace FemRec2023
{
    public class Program
    {
        public static string dataDir = Path.Combine(Environment.CurrentDirectory, "Data");

        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            if (!Directory.Exists(dataDir))
            {
                Console.WriteLine($"Setting up data directory...");

                string[] foldersToCreate = new[]
                {
                    dataDir,
                    Path.Combine(dataDir, "Images"),
                    Path.Combine(dataDir, "Images", "PlayerImages"),
                    Path.Combine(dataDir, "Images", "PolaroidImages"),
                    Path.Combine(dataDir, "CDN", "DataBlobs"),
                    Path.Combine(dataDir, "CDN", "InventionBlobs"),
                    Path.Combine(dataDir, "CDN", "RoomBlobs"),
                    Path.Combine(dataDir, "Imports"),
                };

                foreach (var folder in foldersToCreate)
                {
                    Directory.CreateDirectory(folder);
                }

                Console.WriteLine($"Set up Data directory at {dataDir}");
            }

            builder.Services.AddControllers().AddJsonOptions(o =>
            {
                o.JsonSerializerOptions.PropertyNamingPolicy = null;
            });

            builder.Services.AddHttpLogging(options =>
            {
                options.LoggingFields = Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.All;
            });

            var app = builder.Build();
            
            app.UseHttpLogging();
            
            app.UseWebSockets();
            
            app.UseRouting();
            
            app.UseAuthorization();

            app.MapControllers();
            
            // RoomDB.ImportRooms(Path.Combine(dataDir, "Imports", "ImportRooms.json"));
                        
            Task.Run(() => Cloudflare.StartCloudflared());
            
            app.Run("http://localhost:2059");
        }
    }
}