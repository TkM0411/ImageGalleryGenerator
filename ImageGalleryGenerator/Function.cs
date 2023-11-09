using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using ImageGalleryGenerator.Core;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace ImageGalleryGenerator;

public class Function
{
    private readonly string S3BucketName;
    public Function()
    {
        S3BucketName = Environment.GetEnvironmentVariable("S3BucketName");
    }

    /// <summary>
    /// A simple function that takes a string and does a ToUpper
    /// </summary>
    /// <param name="input"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public void FunctionHandler(ILambdaContext context)
    {
        try
        {
            using (HTMLGenerator htmlGenerator = new HTMLGenerator(S3BucketName))
            {
                htmlGenerator.UpdateGalleryHTML();
            }
        }
        catch (Exception ex)
        {
            context.Logger.LogError(ex.Message);
            context.Logger.LogError(ex.StackTrace);
        }
    }
}