using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Text;
using System.Web.Security;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ZhClass;
using System.Data;
using System.Data.SqlClient;
using System.Web;
using System.ServiceModel.Channels;
using Amazon.S3;
using Amazon.S3.IO;
using Amazon.S3.Model;
using System.IO;
using System.Drawing;
using iTextSharp.text.pdf;
using iTextSharp.text;
using System.Drawing.Imaging;

namespace MorrisonServer.Controllers.api
{
    [BasicAuthentication.Filters.BasicAuthentication] // Enable authentication
    [Authorize]

    public class AngusController : ApiController
    {
        string resultCode = "10";
        string errStr = "";
        StringBuilder strSql = new StringBuilder(200);
        string clientIp = "";
        string operStatusId2 = "10";

        #region T0_Sample 範例
        [HttpPost]
        public HttpResponseMessage FirstAPI(Models.Angus.FirstAPI actRow)
        {
            var resp = new HttpResponseMessage();
            Models.ZhWebClass_AngusV1.UserData userData = new Models.ZhWebClass_AngusV1.UserData();
            clientIp = GetClientIp();
            JObject jo = new JObject();

            try
            {

                #region 取得 User FormsAuthenticationTicket
                actRow.ticket = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.ticket).ToString();
                userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserDataApp(actRow.ticket.ToString());
                #endregion


                #region 開始製作你的API
                jo.Add("test", "APIOK");
                #endregion

            }
            catch (Exception ex)
            {
                resultCode = "01";
                errStr = ex.Message;
            }

            jo.Add("resultCode", resultCode);
            jo.Add("error", errStr);

            #region operLog 
            if (userData != null)
            {
                DataTable tbl_OperLog = userData.Get_tbl_operLogV1();
                DataRow operDr = tbl_OperLog.NewRow();
                operDr["actSerial"] = userData.actSerial;
                operDr["logDate"] = DateTime.UtcNow;
                operDr["sysUserId"] = userData.sysUserId;
                operDr["clientIp"] = clientIp;
                operDr["controllerName"] = this.ControllerContext.RouteData.Values["controller"].ToString();
                operDr["actionName"] = this.ControllerContext.RouteData.Values["action"].ToString();
                operDr["statusId"] = "20";  //10:登入系統、20:查詢資料、30:新增、40:修改、50:刪除、90:登出系統
                operDr["statusId"] = operStatusId2;  //10:登入系統、20:查詢資料、30:新增、40:修改、50:刪除、90:登出系統
                operDr["resultCode"] = resultCode;
                operDr["errMsg"] = errStr;
                //operDr["strSql"] = strSql.ToString();

                tbl_OperLog.Rows.Add(operDr);
                errStr = Models.ZhWebClass_AngusV1.Log.SaveOperLog(tbl_OperLog);
            }
            #endregion

            resp.StatusCode = HttpStatusCode.OK;
            resp.Content = new StringContent(JsonConvert.SerializeObject(jo), System.Text.Encoding.UTF8, "application/json");
            return resp;
        }
        #endregion


        #region UploadS3Test 範例
        [HttpGet]
        public HttpResponseMessage UploadS3Test()
        {
            var resp = new HttpResponseMessage();
            Models.ZhWebClass_AngusV1.UserData userData = new Models.ZhWebClass_AngusV1.UserData();
            clientIp = GetClientIp();
            JObject jo = new JObject();

            try
            {
                string _bucketName = "cfs-epod";
                string fileName2 = "test.jpg";
                string localPath = System.Web.HttpContext.Current.Server.MapPath("\\PDF\\" + fileName2);

                IAmazonS3 client;
                using (client = Amazon.AWSClientFactory.CreateAmazonS3Client(Amazon.RegionEndpoint.USWest2))
                {
                    var request = new PutObjectRequest()
                    {
                        BucketName = _bucketName,
                        Key = fileName2,
                        FilePath = localPath.Replace(fileName2, fileName2)//SEND THE FILE STREAM
                    };

                    PutObjectResponse response = client.PutObject(request);

                    GetPreSignedUrlRequest request1 = new GetPreSignedUrlRequest
                    {
                        BucketName = _bucketName,
                        Key = fileName2,
                        Expires = DateTime.Now.AddMinutes(5)
                    };
                    string urlString = client.GetPreSignedURL(request1);

                    jo.Add("url", urlString);
                }

                #region 開始製作你的API
                jo.Add("test", "APIOK");
                #endregion

            }
            catch (Exception ex)
            {
                resultCode = "01";
                errStr = ex.Message;
            }

            jo.Add("resultCode", resultCode);
            jo.Add("error", errStr);

            #region operLog 
            if (userData != null)
            {
                DataTable tbl_OperLog = userData.Get_tbl_operLogV1();
                DataRow operDr = tbl_OperLog.NewRow();
                operDr["actSerial"] = userData.actSerial;
                operDr["logDate"] = DateTime.UtcNow;
                operDr["sysUserId"] = userData.sysUserId;
                operDr["clientIp"] = clientIp;
                operDr["controllerName"] = this.ControllerContext.RouteData.Values["controller"].ToString();
                operDr["actionName"] = this.ControllerContext.RouteData.Values["action"].ToString();
                operDr["statusId"] = "20";  //10:登入系統、20:查詢資料、30:新增、40:修改、50:刪除、90:登出系統
                operDr["statusId"] = operStatusId2;  //10:登入系統、20:查詢資料、30:新增、40:修改、50:刪除、90:登出系統
                operDr["resultCode"] = resultCode;
                operDr["errMsg"] = errStr;
                //operDr["strSql"] = strSql.ToString();

                tbl_OperLog.Rows.Add(operDr);
                errStr = Models.ZhWebClass_AngusV1.Log.SaveOperLog(tbl_OperLog);
            }
            #endregion

            resp.StatusCode = HttpStatusCode.OK;
            resp.Content = new StringContent(JsonConvert.SerializeObject(jo), System.Text.Encoding.UTF8, "application/json");
            return resp;
        }
        #endregion

        #region UploadS3Test 範例
        [HttpGet]
        public HttpResponseMessage GetS3File(string filePath)
        {
            var resp = new HttpResponseMessage();
            Models.ZhWebClass_AngusV1.UserData userData = new Models.ZhWebClass_AngusV1.UserData();
            clientIp = GetClientIp();
            JObject jo = new JObject();

            try
            {
                string _bucketName = "cfs-epod";
                string fileName = filePath;

                IAmazonS3 client;
                using (client = Amazon.AWSClientFactory.CreateAmazonS3Client(Amazon.RegionEndpoint.USWest2))
                {

                    GetPreSignedUrlRequest request1 = new GetPreSignedUrlRequest
                    {
                        BucketName = _bucketName,
                        Key = fileName,
                        Expires = DateTime.Now.AddMinutes(5)
                    };
                    string urlString = client.GetPreSignedURL(request1);

                    jo.Add("url", urlString);
                }

                #region 開始製作你的API
                jo.Add("test", "APIOK");
                #endregion

            }
            catch (Exception ex)
            {
                resultCode = "01";
                errStr = ex.Message;
            }

            jo.Add("resultCode", resultCode);
            jo.Add("error", errStr);

            #region operLog 
            //if (userData != null)
            //{
            //    DataTable tbl_OperLog = userData.Get_tbl_operLogV1();
            //    DataRow operDr = tbl_OperLog.NewRow();
            //    operDr["actSerial"] = userData.actSerial;
            //    operDr["logDate"] = DateTime.UtcNow;
            //    operDr["sysUserId"] = userData.sysUserId;
            //    operDr["clientIp"] = clientIp;
            //    operDr["controllerName"] = this.ControllerContext.RouteData.Values["controller"].ToString();
            //    operDr["actionName"] = this.ControllerContext.RouteData.Values["action"].ToString();
            //    operDr["statusId"] = "20";  //10:登入系統、20:查詢資料、30:新增、40:修改、50:刪除、90:登出系統
            //    operDr["statusId"] = operStatusId2;  //10:登入系統、20:查詢資料、30:新增、40:修改、50:刪除、90:登出系統
            //    operDr["resultCode"] = resultCode;
            //    operDr["errMsg"] = errStr;
            //    //operDr["strSql"] = strSql.ToString();

            //    tbl_OperLog.Rows.Add(operDr);
            //    errStr = Models.ZhWebClass_AngusV1.Log.SaveOperLog(tbl_OperLog);
            //}
            #endregion

            resp.StatusCode = HttpStatusCode.OK;
            resp.Content = new StringContent(JsonConvert.SerializeObject(jo), System.Text.Encoding.UTF8, "application/json");
            return resp;
        }
        #endregion

        #region 
        [HttpGet]
        public HttpResponseMessage MergePDF()
        {
            var resp = new HttpResponseMessage();
            Models.ZhWebClass_AngusV1.UserData userData = new Models.ZhWebClass_AngusV1.UserData();
            clientIp = GetClientIp();
            JObject jo = new JObject();

            try
            {
                string Pod_Name = "Angus Hsiaoa";
                string fileName = "dispatch.pdf";
                string imgName = "signin.png";



                string pdfPath = System.Web.HttpContext.Current.Server.MapPath("\\PDF\\" + fileName);
                string imgPath = System.Web.HttpContext.Current.Server.MapPath("\\PDF\\" + imgName);

                StreamReader sr = new System.IO.StreamReader(imgPath);


                using (Stream inputPdfStream = new FileStream(pdfPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (Stream outputPdfStream = new FileStream(pdfPath.Replace(fileName, "new_" + fileName), FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    iTextSharp.text.Image image = iTextSharp.text.Image.GetInstance(imgPath);

                    var reader = new PdfReader(inputPdfStream);
                    var stamper = new PdfStamper(reader, outputPdfStream);
                    var pdfContentByte = stamper.GetOverContent(1);

                    image.ScaleAbsoluteWidth(50);
                    image.ScaleAbsoluteHeight(image.Height / (image.Width / 50));
                    image.SetAbsolutePosition(340, 50);
                    pdfContentByte.AddImage(image);


                    BaseFont bf = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
                    var pod = pdfContentByte.CreateTemplate(120, 60);
                    pod.BeginText();

                    pod.SetFontAndSize(bf, 12);
                    pod.SetTextMatrix(0, 5);
                    pod.ShowText(Pod_Name);
                    pod.EndText();

                    pdfContentByte.SetColorFill(BaseColor.BLUE);
                    pdfContentByte.AddTemplate(pod, 160, 50);

                    var date = pdfContentByte.CreateTemplate(120, 60);
                    date.BeginText();

                    date.SetFontAndSize(bf, 12);
                    date.SetTextMatrix(0, 0);
                    date.ShowText(DateTime.Now.ToString("MM/dd HH:mm"));
                    date.EndText();

                    pdfContentByte.SetColorFill(BaseColor.BLUE);
                    pdfContentByte.AddTemplate(date, 480, 55);

                    inputPdfStream.Close();

                    stamper.Close();
                }
            }
            catch (Exception ex)
            {
                resultCode = "01";
                errStr = ex.Message;
            }

            jo.Add("resultCode", resultCode);
            jo.Add("error", errStr);

            resp.StatusCode = HttpStatusCode.OK;
            resp.Content = new StringContent(JsonConvert.SerializeObject(jo), System.Text.Encoding.UTF8, "application/json");
            return resp;
        }
        #endregion

        #region  圖片壓縮
        [HttpGet]
        public HttpResponseMessage ImageCompression()
        {
            var resp = new HttpResponseMessage();
            Models.ZhWebClass_AngusV1.UserData userData = new Models.ZhWebClass_AngusV1.UserData();
            clientIp = GetClientIp();
            JObject jo = new JObject();

            try
            {
                string imageName = "4K_img.jpg";
                string imgPath = System.Web.HttpContext.Current.Server.MapPath("\\PDF\\");
                
                System.Drawing.Image image = System.Drawing.Image.FromFile(imgPath + imageName);

                using (MemoryStream ms = new MemoryStream())
                {
                    image.Save(ms, image.RawFormat);
                    Models.MEC_MethodMD.ImageCompression ic = new Models.MEC_MethodMD.ImageCompression();
                    using(MemoryStream msImg = ic.MemoryStreamToMemoryStream(ms, "JPG"))
                    {
                        System.Drawing.Image imgNew = System.Drawing.Image.FromStream(msImg);
                        ImageFormat thisFormat = image.RawFormat;


                        Bitmap imageOutput = new Bitmap(imgNew);

                        //輸出一個新圖(就是修改過的圖)
                        string fixSaveName = string.Concat("new_4K_img", ".jpg");
                        //副檔名不應該這樣給，但因為此範例沒有讀取檔案的部份所以demo就直接給啦

                        imageOutput.Save(imgPath + fixSaveName, thisFormat);
                        //將修改過的圖存於設定的位子
                        imageOutput.Dispose();
                        //釋放記憶體
                        image.Dispose();
                        //釋放掉圖檔 
                    }
                };



            }
            catch (Exception ex)
            {
                resultCode = "01";
                errStr = ex.Message;
            }

            jo.Add("resultCode", resultCode);
            jo.Add("error", errStr);

            resp.StatusCode = HttpStatusCode.OK;
            resp.Content = new StringContent(JsonConvert.SerializeObject(jo), System.Text.Encoding.UTF8, "application/json");
            return resp;
        }
        #endregion

        #region UploadImage 上傳圖片測試
        [HttpPost]
        public HttpResponseMessage UploadImage()
        {
            var resp = new HttpResponseMessage();
            JObject jo = new JObject();
            try
            {
                string test = HttpContext.Current.Request.Params["test"];
                jo.Add("test", test);

                HttpPostedFile file = HttpContext.Current.Request.Files["file"];
                MemoryStream ms = new MemoryStream();
                file.InputStream.CopyTo(ms);

                using (Bitmap bmp = new Bitmap(ms))
                {
                    using (System.Drawing.Image tmpImg = System.Drawing.Image.FromStream(ms))
                    {
                        string fileName = file.FileName;
                        string fileType = fileName.Split('.')[1];
                        string path = HttpContext.Current.Server.MapPath("~/PDF/" + fileName);
                        if (fileType.ToUpper() == "PNG")
                        {
                            tmpImg.Save(path, System.Drawing.Imaging.ImageFormat.Png);
                        }
                        else
                        {
                            tmpImg.Save(path, System.Drawing.Imaging.ImageFormat.Jpeg);
                        }

                        jo.Add("message", "save Success");
                    }
                }
            }
            catch (Exception ex)
            {
                resultCode = "01";
                errStr = ex.Message;
            }

            resp.StatusCode = HttpStatusCode.OK;
            resp.Content = new StringContent(JsonConvert.SerializeObject(jo), System.Text.Encoding.UTF8, "application/json");
            return resp;
        }
        #endregion

        #region private Method
        #region GetClientIp
        private string GetClientIp(HttpRequestMessage request = null)
        {
            request = request ?? Request;

            if (request.Properties.ContainsKey("MS_HttpContext"))
            {
                return ((HttpContextWrapper)request.Properties["MS_HttpContext"]).Request.UserHostAddress;
            }
            else if (request.Properties.ContainsKey(RemoteEndpointMessageProperty.Name))
            {
                RemoteEndpointMessageProperty prop = (RemoteEndpointMessageProperty)request.Properties[RemoteEndpointMessageProperty.Name];
                return prop.Address;
            }
            else if (HttpContext.Current != null)
            {
                return HttpContext.Current.Request.UserHostAddress;
            }
            else
            {
                return null;
            }
        }
        #endregion
        #endregion

    }
}
