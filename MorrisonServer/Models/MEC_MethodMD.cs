using Amazon.S3;
using Amazon.S3.Model;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Web;

namespace MorrisonServer.Models.MEC_MethodMD
{
    public class ImageCompression
    {

        public MemoryStream MemoryStreamToMemoryStream(MemoryStream inputMs, string fileType)
        {
            MemoryStream outputMs = new MemoryStream();

            Image image = Image.FromStream(inputMs);

            ImageFormat thisFormat = image.RawFormat;

            int fixWidth = 0;
            int fixHeight = 0;

            string ConvertImgMaxWidthOrHeight = System.Web.Configuration.WebConfigurationManager.AppSettings["ConvertImgMaxWidthOrHeight"];

            //第一種縮圖用
            //宣告一個最大值
            int maxPx = Convert.ToInt16(ConvertImgMaxWidthOrHeight);

            if (image.Width > maxPx || image.Height > maxPx)
            //如果圖片的寬大於最大值或高大於最大值就往下執行
            {
                if (image.Width >= image.Height)
                //圖片的寬大於圖片的高
                {
                    fixWidth = maxPx;
                    //設定修改後的圖寬
                    fixHeight = Convert.ToInt32((Convert.ToDouble(fixWidth) / Convert.ToDouble(image.Width)) * Convert.ToDouble(image.Height));
                    //設定修改後的圖高
                }
                else
                {
                    fixHeight = maxPx;
                    //設定修改後的圖高
                    fixWidth = Convert.ToInt32((Convert.ToDouble(fixHeight) / Convert.ToDouble(image.Height)) * Convert.ToDouble(image.Width));
                    //設定修改後的圖寬
                }

            }
            else
            //圖片沒有超過設定值，不執行縮圖
            {
                fixHeight = image.Height;
                fixWidth = image.Width;
            }
            Bitmap imageOutput = new Bitmap(image, fixWidth, fixHeight);

            if (fileType.Replace(".", "").ToUpper() == "PNG")
            {
                imageOutput.Save(outputMs, ImageFormat.Png);
            }
            else
            {
                imageOutput.Save(outputMs, ImageFormat.Jpeg);
            }

            imageOutput.Dispose();
            //釋放記憶體
            image.Dispose();
            //釋放掉圖檔 

            return outputMs;
        }
    }

    public class Aws
    {
        public static bool ChkObjectIsExistInS3(string backetName, string key)
        {
            #region 確認bol是否存在於 s3
            IAmazonS3 client;
            bool isExist = false;
            using (client = Amazon.AWSClientFactory.CreateAmazonS3Client(Amazon.RegionEndpoint.USWest2))
            {
                GetObjectMetadataRequest request1 = new GetObjectMetadataRequest
                {
                    BucketName = backetName,
                    Key = key
                };

                try
                {
                    client.GetObjectMetadata(request1);
                    isExist = true;
                }
                catch (Exception)
                {
                    isExist = false;
                }
            }
            if (!isExist) throw new Exception("Can't found bol related Information，Please use take picutre or use album to upload");
            //if (!isExist) throw new Exception("找不到bol相關資料，請改用拍照或圖片的方式上傳");
            #endregion

            return isExist;
        }

        public GetObjectResponse GetObjectInS3(string backetName, string key)
        {
            IAmazonS3 client;
            GetObjectResponse getObjRespone;
            using (client = Amazon.AWSClientFactory.CreateAmazonS3Client(Amazon.RegionEndpoint.USWest2))
            {
                getObjRespone = client.GetObject(backetName, key);
            }
            return getObjRespone;
        }

        public string  GetObjectUrlInS3(string backName, string key)
        {
            string urlString = "";

            IAmazonS3 client;
            using (client = Amazon.AWSClientFactory.CreateAmazonS3Client(Amazon.RegionEndpoint.USWest2))
            {
               
                GetPreSignedUrlRequest request1 = new GetPreSignedUrlRequest
                {
                    BucketName = backName,
                    Key = key,
                    Expires = DateTime.Now.AddMinutes(5)
                };
                urlString = client.GetPreSignedURL(request1);

            }
            return urlString;

        }
    }

    public class TimeZone
    {
        public static DateTime ChangeTimeZone(DateTime dt, string localRegion)
        {
            string winZoneName = "";

            //取得需要轉換的UTC時間
            DateTime timeUtc = dt;

            //轉換為C# 對應的time zone
            winZoneName = TimeZoneConverter.TZConvert.IanaToWindows(localRegion).ToString();

            //取得轉換為C# time zone的對應資訊
            TimeZoneInfo cstZone = TimeZoneInfo.FindSystemTimeZoneById(winZoneName);

            //取得該地區對應的時間
            DateTime cstTime = TimeZoneInfo.ConvertTimeFromUtc(timeUtc, cstZone);

            return cstTime;
        }
    }
}