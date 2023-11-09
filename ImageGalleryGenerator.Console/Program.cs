using ImageGalleryGenerator.Core;

namespace ImageGalleryGenerator.Console
{
    internal class Program
    {
        private const string S3BucketName = "rnr-shutterworks";

        static void Main(string[] args)
        {
            try
            {
                using (HTMLGenerator generator = new HTMLGenerator(S3BucketName))
                {
                    generator.UpdateGalleryHTML();
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex.Message);
            }
        }
    }
}