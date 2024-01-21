using NativeFileDialogSharp;

namespace Reverse1999
{
    internal class Timekeeper
    {
        const string VERSION = "1.0.1";
        static readonly byte[] UNITYFS_ID = { 0x55, 0x6E, 0x69, 0x74, 0x79, 0x46, 0x53 };

        enum OperationType
        {
            Decrypt,
            Encrypt,
            None
        }

        class BundleDecryptor
        {
            /// <summary>
            /// Determine what type of operation should do with the asset bundle.
            /// </summary>
            /// <param name="data"></param>
            /// <returns></returns>
            static OperationType GetOperationType(byte[] data)
            {
                return data[0..7].SequenceEqual(UNITYFS_ID)
                    ? OperationType.Encrypt
                    : data[0..7]
                        .Select((b, i) => (byte)(b ^ GenXorKey(data[0], UNITYFS_ID[0])))
                        .SequenceEqual(UNITYFS_ID)
                        ? OperationType.Decrypt
                        : OperationType.None;
            }

            /// <summary>
            /// Generate the XOR key.
            /// The key is the XOR result of the first byte of the data with the character "U (0x55)" from "UNITYFS".
            /// </summary>
            /// <param name="dataFirstByte"></param>
            /// <returns>XOR key.</returns>
            static byte GenXorKey(byte dataFirstByte, byte key)
            {
                byte result = (byte)(dataFirstByte ^ key);
                Console.WriteLine($"XOR Key: {dataFirstByte:X2} ^ {key:X2} = {result:X2}");
                return result;
            }

            /// <summary>
            /// Xor each data chunk with the given key.
            /// </summary>
            /// <param name="chunk"></param>
            /// <param name="key"></param>
            /// <returns>Xor-ed data.</returns>
            static byte[] XorDataChunk(byte[] chunk, byte key)
            {
                byte[] xoredChunk = new byte[chunk.Length];

                for (int i = 0; i < chunk.Length; i++)
                    xoredChunk[i] = (byte)(chunk[i] ^ key);

                return xoredChunk;
            }

            static string GetOutputPath(string assetPath)
            {
                string directory = Path.GetDirectoryName(assetPath) ?? string.Empty;
                string assetFileName = Path.GetFileNameWithoutExtension(assetPath);

                if (assetFileName[^4..] == "_MOD")
                    return Path.Combine(
                        directory,
                        $"{assetFileName}_DEC{Path.GetExtension(assetPath)}"
                    );

                if (assetFileName[^4..] == "_DEC")
                    return Path.Combine(
                        directory,
                        $"{assetFileName.Replace("_DEC", "_MOD")}{Path.GetExtension(assetPath)}"
                    );

                return Path.Combine(
                    directory,
                    $"{assetFileName}_DEC{Path.GetExtension(assetPath)}"
                );
            }

            static void XorBundleFile(string inputPath, string outputPath)
            {
                try
                {
                    byte[] inputData = File.ReadAllBytes(inputPath);
                    byte key = 0;

                    switch (GetOperationType(inputData))
                    {
                        case OperationType.Encrypt:
                            Console.WriteLine("Operation: Encrypt");
                            string originalAssetBundle = Path.Combine(Path.GetDirectoryName(inputPath) ?? inputPath, $"{Path.GetFileName(inputPath).Split('_')[0]}{Path.GetExtension(inputPath)}");

                            if (!File.Exists(originalAssetBundle))
                                throw new Exception($"No original asset bundle found: {originalAssetBundle}");

                            using (
                                FileStream fileStream =
                                    new(originalAssetBundle, FileMode.Open, FileAccess.Read)
                            )
                            {
                                key = GenXorKey(inputData[0], (byte)fileStream.ReadByte());
                            }
                            break;
                        case OperationType.Decrypt:
                            Console.WriteLine("Operation: Decrypt");
                            key = GenXorKey(inputData[0], UNITYFS_ID[0]);
                            break;
                        case OperationType.None:
                            throw new Exception("Invalid asset bundle file!");
                    }

                    if (key == 0)
                        return;

                    Console.WriteLine($"Saving asset bundle as {outputPath}.");

                    byte[] xoredData = XorDataChunk(inputData, key);
                    File.WriteAllBytes(outputPath, xoredData);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error! {ex.Message} Skipping...");
                }
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
                            string outputPath = GetOutputPath(assetPath);
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

        static void PrintHelp()
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
                "Asset Bundle Usages: \n\t Decrypt: Just choose the ENCRYPTED asset bundle(s) to decrypt. The output file should have '_DEC' suffix. \n\t Encrypt: The encryption process requires you to put the ORIGINAL and DECRYPTED asset bundle(s) at the same directory. The DECRYPTED asset bundle(s) file name MUST follow this rule: \"Original Name_whatever you put there\". This tool will detect a file with the name before the UNDERSCORE."
            );
            Console.WriteLine();
        }

        static void Main(string[] args)
        {
            PrintHelp();

            string? cwd = Path.GetDirectoryName(AppContext.BaseDirectory);
            string[]? assetsPath = args;

            //! TODO: Arguments should be adjusted to perform decryption/encryption through CLI.

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
