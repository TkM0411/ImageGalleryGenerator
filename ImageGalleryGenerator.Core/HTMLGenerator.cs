using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using System.Net;
using System.Text;

namespace ImageGalleryGenerator.Core
{
    public class HTMLGenerator : IDisposable
    {
        #region Constants
        private const string HTMLListFormat = @"<li>
	<input type=""radio"" id=""tab-{0}"" name=""gallery-group"">
	<label for=""tab-{0}"">
		<div class=""tab"">
			<img src=""{1}"" />
		</div>
	</label>
	<div class=""content"">
		<img src=""{1}"" />
	</div>
</li>";

        const string MainOuterCSS = @"<style>
body {
    padding: 0;
    margin: 0;
    box-sizing: border-box;
    --line-offset: calc((10vh + 8px) / 2);
}

.container {
    width: 100vw;
    height: 100vh;
    display: grid;
    grid-template-rows: 5fr 1fr;
    background: #021919;
}

ul {
    list-style: none;
    margin: 0;
    padding: 0;
    justify-content: center;
    display: flex;
}

.tab {
    width: calc(10vh + 8px);
    height: calc(10vh + 8px);
    position: relative;
    display: flex;
    align-items: center;
    justify-content: center;
    clip-path: polygon(0% 50%, 15% 0%, 85% 0%, 100% 50%, 85% 100%, 15% 100%);
    shape-outside: polygon(0% 50%, 15% 0%, 85% 0%, 100% 50%, 85% 100%, 15% 100%);
    z-index: 0;
    transition: width 0.5s;
}

.tab img {
    width: 10vh;
    height: 10vh;
    z-index: 10;
    cursor: pointer;
    clip-path: polygon(0% 50%, 15% 0%, 85% 0%, 100% 50%, 85% 100%, 15% 100%);
    shape-outside: polygon(0% 50%, 15% 0%, 85% 0%, 100% 50%, 85% 100%, 15% 100%);
    transition: width 0.5s;
}

[type=radio] {
    display: none;   
}

.preview-list {
    background: linear-gradient(
        #021919,
        #021919 var(--line-offset),
        #efefef var(--line-offset)
    );
}

.tab {
    background: linear-gradient(
        #efefef,
        #efefef var(--line-offset),
        #021919 var(--line-offset)
    );
}

[type=radio]:checked ~ label ~ .content{
    text-align: center;
    z-index: 8;
}


[type=radio]:checked ~ label .tab{
    width: 0;
}

.content {
    position: absolute;
    background: #021919;
    top: 1vh;
    left: 0;
    width: 100vw;
    height: 80vh;
    overflow: hidden;
    display: flex;
    align-items: center;
}

.content img {
    height: auto;
    width: 100vw;  
}
</style>";
    
  const string MainOuterHTMLFormat = @"<div class=""container"">
        <div class=""full-view""></div>
        <div class=""preview-list"">
            <ul>
                {0}
            </ul>
        </div>
    </div>";

        #endregion

        #region Fields
        private AmazonS3Client _s3Client;
        private readonly string _s3BucketName;
        #endregion

        #region Constructor
        public HTMLGenerator()
        {
            _s3Client = new AmazonS3Client(RegionEndpoint.APSouth2);
        }

        public HTMLGenerator(string s3BucketName) : this()
        {
            _s3BucketName = s3BucketName;
        }
        #endregion

        #region Public Methods
        public async void UpdateGalleryHTML()
        {
            try
            {
                List<string> imageList = GetAllImageFileNamesInS3Bucket(_s3BucketName);
                StringBuilder updatedImageInnerHTML = UpdatedImageList(imageList);
                string finalHTML = CreateFinalHTML(MainOuterHTMLFormat, updatedImageInnerHTML.ToString());
                UploadHTMLStringToS3Bucket(_s3BucketName, finalHTML);
            }
            catch (Exception)
            {
                throw;
            }
        }
        #endregion

        #region Private Methods
        private List<string> GetAllImageFileNamesInS3Bucket(string bucketName)
        {
            try
            {
                ListObjectsV2Request listObjectsRequest = new ListObjectsV2Request
                {
                    BucketName = bucketName,
                    Prefix = "Images"
                };
                ListObjectsV2Response response = _s3Client.ListObjectsV2Async(listObjectsRequest).Result;
                if (response != null && response.HttpStatusCode == HttpStatusCode.OK)
                {
                    return response.S3Objects.Where(c => c.Key.EndsWith(".JPG") || c.Key.EndsWith(".jpg") || c.Key.EndsWith(".JPEG") || c.Key.EndsWith(".jpeg")).Select(c => c.Key).ToList();
                }
            }
            catch (Exception)
            {
                throw;
            }
            return null;
        }

        private StringBuilder UpdatedImageList(List<string> imageNames)
        {
            StringBuilder sbInnerHTML = new StringBuilder();
            StringBuilder node = new StringBuilder();
            for (int i = 0; i < imageNames.Count; i++)
            {
                node.Clear();
                node.AppendFormat(HTMLListFormat, (i + 1).ToString(), imageNames[i]);
                sbInnerHTML.AppendLine(node.ToString());
            }
            return sbInnerHTML;
        }

        private string CreateFinalHTML(string htmlFormatString, string innerHTML)
        {
            string htmlOnly = string.Format(htmlFormatString, innerHTML.ToString().Trim());
            StringBuilder sb = new StringBuilder(MainOuterCSS);
            sb.AppendLine(htmlOnly);
            return sb.ToString();
        }

        private void UploadHTMLStringToS3Bucket(string s3BucketName, string fileContents)
        {
            try
            {
                byte[] byteArray = Encoding.ASCII.GetBytes(fileContents);
                using (MemoryStream stream = new MemoryStream(byteArray))
                {
                    PutObjectRequest request = new PutObjectRequest
                    {
                        BucketName = s3BucketName,
                        AutoCloseStream = true,
                        AutoResetStreamPosition = true,
                        ContentType = "text/html",
                        InputStream = stream,
                        Key = "index.html",
                        CannedACL = S3CannedACL.PublicRead
                    };
                    var response =  _s3Client.PutObjectAsync(request).Result;
                    if (response.HttpStatusCode == HttpStatusCode.OK)
                    {

                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
        #endregion

        #region IDisposable Implementation
        public void Dispose()
        {
            if (_s3Client != null)
                _s3Client.Dispose();
        }
        #endregion
    }
}