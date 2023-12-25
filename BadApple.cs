namespace Reverse1999
{
    internal class BadApple
    {
        const string LAST_OPEN_FILE = "last_open.txt";

        internal static void SaveLastOpenedFile(string filePath)
        {
            try
            {
                // Write the last opened directory to a text file
                using StreamWriter writer = new(LAST_OPEN_FILE);
                writer.WriteLine($"last_opened={filePath}");
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        internal static string GetLastOpenedFile()
        {
            string lastOpenedDirectory = string.Empty;

            if (!File.Exists(LAST_OPEN_FILE))
                return lastOpenedDirectory;

            try
            {
                // Read the last opened directory from the text file
                using StreamReader reader = new(LAST_OPEN_FILE);
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.StartsWith("last_opened="))
                    {
                        lastOpenedDirectory = line["last_opened=".Length..];
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return lastOpenedDirectory;
        }
    }
}
