using System.Reflection;

namespace Reverse1999
{
    public class Timekeeper
    {
        const byte DECRYPTION_KEY = 0x55;
        const byte VERIFICATION_KEY = 0x6E;
        const string DECRYPTED_BUNDLES_DIR = "bundles_decoded";
        const string ENCRYPTED_BUNDLES_DIR = "bundles_encoded";
        const string BUNDLES_DIR = "bundles";

        class FileDecryptor
        {
            public string DecryptedPath { get; }

            public FileDecryptor(string bundlesPath)
            {
                DecryptedPath = Path.Combine(bundlesPath, DECRYPTED_BUNDLES_DIR);

                if (!Directory.Exists(DecryptedPath))
                    Directory.CreateDirectory(DecryptedPath);
            }

            static byte[] DecryptDataChunk(byte[] chunk, byte key)
            {
                byte[] decryptedChunk = new byte[chunk.Length];

                for (int i = 0; i < chunk.Length; i++)
                    decryptedChunk[i] = (byte)(chunk[i] ^ key);

                return decryptedChunk;
            }

            public static void DecryptFile(string inputPath, string outputPath)
            {
                byte[] inputData = File.ReadAllBytes(inputPath);
                byte key = (byte)(inputData[0] ^ DECRYPTION_KEY); // generate xor key from the first byte
                Console.WriteLine($"XOR Key: {key}");

                if (key != (byte)(inputData[1] ^ VERIFICATION_KEY)) // verify key with the second byte
                    throw new Exception("Invalid key");

                byte[] decryptedData = DecryptDataChunk(inputData, key); // decrypt the entire data
                File.WriteAllBytes(outputPath, decryptedData);
            }

            public (TimeSpan Duration, int FilesDecrypted) DecryptBundles(string bundlesPath)
            {
                DateTime startTime = DateTime.Now;
                int filesDecrypted = 0;

                Parallel.ForEach(
                    Directory.EnumerateFiles(bundlesPath, "*.dat", SearchOption.AllDirectories),
                    new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                    filePath =>
                    {
                        string outputPath = Path.Combine(DecryptedPath, Path.GetFileName(filePath));
                        DecryptFile(filePath, outputPath);
                        filesDecrypted++;
                        Console.WriteLine(
                            $"Decrypted {Path.GetFileName(filePath)} to {outputPath}"
                        );
                    }
                );

                TimeSpan duration = DateTime.Now - startTime;
                return (duration, filesDecrypted);
            }
        }

        static void Main()
        {
            string? cwd =
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
            string? bundlesPath = Path.Combine(cwd, BUNDLES_DIR);

            Console.WriteLine($"Path: {bundlesPath}");

            FileDecryptor decryptor = new(bundlesPath);
            (TimeSpan duration, int filesDecrypted) = decryptor.DecryptBundles(bundlesPath);

            double rps = filesDecrypted / duration.TotalSeconds;
            Console.WriteLine($"Decryption completed in {duration}. Rate: {rps:F2} files/sec");

            Console.WriteLine("Press any key to exit");
            Console.ReadLine();
        }
    }
}
