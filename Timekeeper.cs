using NativeFileDialogSharp;

namespace Reverse1999
{
    internal class Timekeeper
    {
        const string VERSION = "1.0.0";
        private static readonly byte[] UNITYFS_ID = { 0x55, 0x6E, 0x69, 0x74, 0x79, 0x46, 0x53 };
        private static readonly byte[] UNITYFS_ENCRYPTED_ID =
        {
            0xDF,
            0xE4,
            0xE3,
            0xFE,
            0xF3,
            0xCC,
            0xD9
        };

        private enum OperationType
        {
            Decrypt,
            Encrypt
        }

        private class BundleDecryptor
        {
            private static byte[] XorDataChunk(byte[] chunk, byte key)
            {
                byte[] xoredChunk = new byte[chunk.Length];

                for (int i = 0; i < chunk.Length; i++)
                    xoredChunk[i] = (byte)(chunk[i] ^ key);

                return xoredChunk;
            }

            private static bool TestHeader(byte[] originalHeader, byte[] comparisonHeader)
            {
                for (int i = 0; i < comparisonHeader.Length; i++)
                {
                    if (originalHeader[i] != comparisonHeader[i])
                        return false;
                }
                return true;
            }

            private static byte GenerateKey(byte key, OperationType operationType)
            {
                return (byte)(
                    key
                    ^ (
                        operationType == OperationType.Encrypt
                            ? UNITYFS_ENCRYPTED_ID[0]
                            : UNITYFS_ID[0]
                    )
                );
            }

            private static string GetOutputPath(string assetPath, string assetFileName)
            {
                string directory = Path.GetDirectoryName(assetPath) ?? string.Empty;

                if (assetFileName.Contains("_MOD"))
                    return Path.Combine(directory, assetFileName.Replace("_MOD", "_DEC"));

                if (assetFileName.Contains("_DEC"))
                    return Path.Combine(directory, assetFileName.Replace("_DEC", "_MOD"));

                return Path.Combine(
                    directory,
                    $"{Path.GetFileNameWithoutExtension(assetPath)}_DEC{Path.GetExtension(assetPath)}"
                );
            }

            private static void ProcessFile(
                string inputPath,
                string outputPath,
                OperationType operationType
            )
            {
                try
                {
                    byte[] inputData = File.ReadAllBytes(inputPath);
                    byte key = GenerateKey(inputData[0], operationType);

                    byte[] comparisonHeader =
                        operationType == OperationType.Encrypt ? UNITYFS_ID : UNITYFS_ENCRYPTED_ID;

                    if (!TestHeader(inputData, comparisonHeader))
                    {
                        Console.WriteLine("Invalid asset bundle file!");
                        return;
                    }

                    Console.WriteLine($"Operation: {operationType}");
                    Console.WriteLine(
                        $"Saving Xor-ed asset bundle {Path.GetFileName(inputPath)} as {outputPath}."
                    );
                    byte[] xoredData = XorDataChunk(inputData, key);
                    File.WriteAllBytes(outputPath, xoredData);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error! {ex.Message}. Skipping...");
                }
            }

            public static void XorBundleFile(string inputPath, string outputPath)
            {
                string assetFileName = Path.GetFileNameWithoutExtension(inputPath);
                OperationType operationType = assetFileName[^4..] switch
                {
                    "_DEC" => OperationType.Encrypt,
                    _ => OperationType.Decrypt,
                };
                ProcessFile(inputPath, outputPath, operationType);
            }

            public static (TimeSpan Duration, int FilesXored) XorBundleAssets(string[] assetPaths)
            {
                DateTime startTime = DateTime.Now;
                int filesXored = 0;

                Parallel.ForEach(
                    assetPaths,
                    new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                    assetPath =>
                    {
                        try
                        {
                            string assetFileName = Path.GetFileName(assetPath);
                            string outputPath = GetOutputPath(assetPath, assetFileName);

                            XorBundleFile(assetPath, outputPath);
                            filesXored++;
                        }
                        catch { }
                    }
                );

                TimeSpan duration = DateTime.Now - startTime;
                return (duration, filesXored);
            }
        }

        private static void PrintHelp()
        {
            Console.Title = "Reverse: 1999 - Anarchist";
            Console.WriteLine(
                "Reverse: 1999 - Anarchist is an asset encryptor & decryptor for Reverse: 1999 game by BLUEPOCH."
            );
            Console.WriteLine(
                "For more information, visit: https://github.com/kiraio-moe/Reverse1999-Anarchist"
            );
            Console.WriteLine($"Version: {VERSION}");
            Console.WriteLine(
                "Usage: Asset bundle WITHOUT any suffix/has '_MOD' suffix will be DECRYPTED | '_DEC' suffix will be ENCRYPTED"
            );
            Console.WriteLine();
        }

        private static void Main(string[] args)
        {
            PrintHelp();

            string? cwd = Path.GetDirectoryName(AppContext.BaseDirectory);
            string[]? assetsPath = args;

            PickFile:
            if (args.Length < 1)
            {
                Console.WriteLine(
                    "Press SPACE BAR to perform encryption/decryption operation, X to exit."
                );
                ConsoleKeyInfo keyInfo = Console.ReadKey(true);

                switch (keyInfo.Key)
                {
                    case ConsoleKey.Spacebar:
                        Console.WriteLine("Opening file dialog...");
                        DialogResult filePicker = Dialog.FileOpenMultiple(
                            "dat",
                            Path.GetDirectoryName(BadApple.GetLastOpenedFile()) ?? cwd
                        );

                        if (filePicker.IsCancelled)
                        {
                            Console.WriteLine("Canceled.");
                            goto PickFile;
                        }

                        assetsPath = filePicker.Paths.ToArray();
                        BadApple.SaveLastOpenedFile(assetsPath[0]);
                        break;

                    case ConsoleKey.X:
                        return;

                    default:
                        goto PickFile;
                }
            }

            (TimeSpan duration, int filesDecrypted) = BundleDecryptor.XorBundleAssets(assetsPath);
            double rps = filesDecrypted / duration.TotalSeconds;

            Console.WriteLine($"Xor-ing completed in {duration}. Rate: {rps:F2} files/sec");
            Console.WriteLine();
            goto PickFile;
        }
    }
}
