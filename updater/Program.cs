using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;

namespace FileUpdater
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "candymc updater"; // Set the title

            UpdateFiles();
            DeleteObsoleteFiles();

            // Set the working directory for the process
            string gameDir = Path.GetFullPath("game");
            Directory.SetCurrentDirectory(gameDir);

            // Launch the executable file with the specified arguments
            System.Diagnostics.Process.Start("./main.exe", "-alpo123");
        }

        static void UpdateFiles()
        {
            // Load the file list from files.json
            string filesUrl = "http://files.candiedapple.tk/files/files.json";
            string filesJson;
            using (WebClient client = new WebClient())
            {
                filesJson = client.DownloadString(filesUrl);
            }

            dynamic files = Newtonsoft.Json.JsonConvert.DeserializeObject(filesJson);

            // Download/update each file
            foreach (dynamic file in files)
            {
                string filename = file.filename;
                string expectedHash = file.hash;
                string url = "http://files.candiedapple.tk/files/" + filename;

                // Construct the full path to the file, including the "game" subfolder
                string fullPath = Path.Combine("game", filename);

                // Check if the file already exists
                if (File.Exists(fullPath))
                {
                    // Calculate the hash of the existing file
                    using (FileStream fs = File.OpenRead(fullPath))
                    {
                        using (SHA256 sha256 = SHA256.Create())
                        {
                            byte[] fileHashBytes = sha256.ComputeHash(fs);
                            string fileHash = BitConverter.ToString(fileHashBytes).Replace("-", "").ToLower();

                            // Compare the expected hash to the existing file hash
                            if (fileHash == expectedHash)
                            {
                                Console.WriteLine($"File {filename} is up to date.");
                                continue; // Skip download if the hashes match
                            }
                        }
                    }
                }

                // Check if the file's directory exists, and create it if it doesn't
                string directory = Path.GetDirectoryName(Path.GetFullPath(fullPath));
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                Console.WriteLine($"Downloading file {filename}...");

                using (WebClient client = new WebClient())
                {
                    client.DownloadProgressChanged += (sender, e) =>
                    {
                        // Update the progress bar
                        Console.Write($"\rProgress: {e.ProgressPercentage}% [{new string('#', e.ProgressPercentage / 5)}{new string(' ', (100 - e.ProgressPercentage) / 5)}]");
                    };

                    // Download the file
                    client.DownloadFileAsync(new Uri(url), fullPath);

                    // Wait for the download to complete
                    while (client.IsBusy)
                    {
                        System.Threading.Thread.Sleep(100);
                    }
                }

                Console.WriteLine(); // Print a new line after the progress bar

                // Calculate the hash of the downloaded file
                using (FileStream fs = File.OpenRead(fullPath))
                {
                    using (SHA256 sha256 = SHA256.Create())
                    {
                        byte[] fileHashBytes = sha256.ComputeHash(fs);
                        string fileHash = BitConverter.ToString(fileHashBytes).Replace("-", "").ToLower();

                        // Compare the expected hash to the calculated hash
                        if (fileHash != expectedHash)
                        {
                            Console.WriteLine($"Hash mismatch for file {filename}. Updating file...");
                            // Update the file
                            using (WebClient client = new WebClient())
                            {
                                client.DownloadFile(url, fullPath);
                            }
                        }
                        else
                        {
                            Console.WriteLine($"File {filename} is up to date.");
                        }
                    }
                }
            }
        }

        static void DeleteObsoleteFiles()
        {
            // Load the file list from files.json
            string filesUrl = "http://files.candiedapple.tk/files/files.json";
            string filesJson;
            using (WebClient client = new WebClient())
            {
                filesJson = client.DownloadString(filesUrl);
            }

            dynamic files = Newtonsoft.Json.JsonConvert.DeserializeObject(filesJson);

            // Get the list of existing files
            string[] existingFiles = Directory.GetFiles("game", "*", SearchOption.AllDirectories);

            // Check for obsolete files and delete them
            foreach (string filePath in existingFiles)
            {
                string relativePath = Path.GetRelativePath("game", filePath).Replace('\\', '/');

                bool fileExistsInJson = false;

                foreach (dynamic file in files)
                {
                    string filename = file.filename;
                    if (filename == relativePath)
                    {
                        fileExistsInJson = true;
                        break;
                    }
                }

                if (!fileExistsInJson)
                {
                    Console.WriteLine($"Deleting obsolete file: {relativePath}");
                    File.Delete(filePath);
                }
            }

            // Remove empty directories
            string[] emptyDirectories = Directory.GetDirectories("game", "*", SearchOption.AllDirectories);
            Array.Reverse(emptyDirectories); // Start from the deepest directories

            foreach (string directoryPath in emptyDirectories)
            {
                if (Directory.GetFiles(directoryPath).Length == 0 && Directory.GetDirectories(directoryPath).Length == 0)
                {
                    Console.WriteLine($"Deleting empty directory: {directoryPath}");
                    Directory.Delete(directoryPath);
                }
            }
        }
    }
}
