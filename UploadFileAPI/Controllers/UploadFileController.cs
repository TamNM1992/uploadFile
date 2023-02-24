using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UploadFileAPI.Helper;
using UploadFileAPI.Services;

namespace UploadFilesAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]/[Action]")]
    public class UploadFileController : ControllerBase
    {
        IWebHostEnvironment _webHost;
        public readonly IUploadServices _uploadServices;
        public UploadFileController(IWebHostEnvironment webHost, IUploadServices uploadServices)
        {
            _webHost = webHost;
            _uploadServices = uploadServices;
        }

        public class ResponseData
        {
            public string status { get; set; }
            public string message { get; set; }
            public string data { get; set; }
            public string size { get; set; }

        }
        public class FileUploadInfo
        {
            public string filename { get; set; }
            public long filesize { get; set; }
            public string prettyFileSize
            {
                get
                {
                    return BytesToReadableValue(this.filesize);
                }
            }

            public string BytesToReadableValue(long number)
            {
                var suffixes = new List<string> { " B", " KB", " MB", " GB", " TB", " PB" };

                for (int i = 0; i < suffixes.Count; i++)
                {
                    long temp = number / (int)Math.Pow(1024, i + 1);

                    if (temp == 0)
                    {
                        return (number / (int)Math.Pow(1024, i)) + suffixes[i];
                    }
                }

                return number.ToString();
            }
        }

        [HttpPost]
        //[FromBody("width/path")]
        public async Task<IActionResult> UploadFileAsync(List<IFormFile> file, int width, string? Obj_Id, int type)
        {
            const bool AllowLimitSize = true;
            const bool AllowLimitFileType = true;

            var limitFileSize = 4194304; // allow upload file less 2MB = 2097152
            var listFileError = new List<FileUploadInfo>();
            var responseData = new ResponseData();
            string result = "";

            if(file.Count <= 0)
            {
                responseData.status = "ERROR";
                responseData.message = $"Please, select file to upload.";
                result = JsonConvert.SerializeObject(responseData);
                return Ok(result);
            }

            var listFileTypeAllow = "jpg|png|gif|xls|xlsx|jpeg|webp|jfif";
            // check file type upload allow
            if (AllowLimitFileType)
            {
                foreach (var formFile in file)
                {
                    var file_ext = Path.GetExtension(formFile.FileName).Replace(".", "");
                    var isAllow = listFileTypeAllow.Split('|').Any(x => x.ToLower() == file_ext.ToLower());
                    if (!isAllow)
                    {
                        listFileError.Add(new FileUploadInfo()
                        {
                            filename = formFile.FileName,
                            filesize = formFile.Length
                        });
                    }

                }
            }

            if (listFileError.Count > 0)
            {
                responseData.status = "ERROR";
                responseData.data = JsonConvert.SerializeObject(listFileError);
                responseData.message = $"File type upload only Allow Type: ({listFileTypeAllow}) \r\n {responseData.data}";
                result = JsonConvert.SerializeObject(responseData);
                return Ok(result);
            }

            // check list file less limit size
            if (AllowLimitSize)
            {
                foreach (var formFile in file)
                {
                    if (formFile.Length > limitFileSize)
                    {
                        listFileError.Add(new FileUploadInfo()
                        {
                            filename = formFile.FileName,
                            filesize = formFile.Length
                        });
                    }
                }
                
            }

            var listLinkUploaded = new List<string>();
            var listFileLenght   = new List<int>();

            if (listFileError.Count > 0)
            {
                responseData.status = "ERROR";
                responseData.data = JsonConvert.SerializeObject(listFileError);
                responseData.message = $"File upload must less 4MB ({responseData.data})";
                result = JsonConvert.SerializeObject(responseData);
                return Ok(result);
            }

            string baseUrl = $"{this.Request.Scheme}://{this.Request.Host}{this.Request.PathBase}";
 
            int i = 0;
            string directoryPath = "";
            int filelenght= 0;
            string urlpath = "";

            if (type ==0)
            {
                directoryPath = $@"{_webHost.WebRootPath}\uploads\hotel\{Obj_Id}\";
            }
            if (type ==1)
            {
                directoryPath = $@"{_webHost.WebRootPath}\uploads\room\{Obj_Id}\";
            }
            if (type == 2)
            {
                directoryPath = $@"{_webHost.WebRootPath}\uploads\tour\{Obj_Id}\";
            }
            if (type == 3)
            {
                directoryPath = $@"{_webHost.WebRootPath}\uploads\news\{Obj_Id}\";
            }

            DirectoryInfo directory = new DirectoryInfo(directoryPath);

            if (!directory.Exists)
            {
                directory.Create();
            }

            foreach (var formFile in file)
            {
                FileHelper.GeneratorFileByDay(FileStype.Log, $"Số lượng file : {file.Count}", "SaveImage");
                try
                {
                    if (formFile.Length > 0)
                    {


                        var time = DateTime.UtcNow;
                        var timestring = $"{time}";

                        string[] charsToRemove = new string[] { "@", ",", ".", ";", "'", "/", ":", " " };
                        foreach (var c in charsToRemove)
                        {
                            timestring = timestring.Replace(c, string.Empty);
                        }
                        var templateUrl = Path.GetExtension(formFile.FileName).Replace(".", $"{timestring}00{i}.");

                        //var templateUrl = file.FileName ;
                        //string filePath = Path.Combine($"{_webHost.WebRootPath}/uploads/{path}", templateUrl);
                        string filePath = Path.Combine(directoryPath, templateUrl);

                        string fileName = Path.GetFileName(filePath);

                        using (var stream = System.IO.File.Create(filePath))
                        {

                            await formFile.CopyToAsync(stream);
                            filelenght = _uploadServices.ThayDoiKichThuocAnh(filePath, fileName, width, stream);
                            FileHelper.GeneratorFileByDay(FileStype.Log, $"thành công file {fileName}", "SaveImage");
                        }
                        listLinkUploaded.Add(directoryPath + templateUrl);
                        listFileLenght.Add(filelenght);
                        i++;
                    }
                }
                catch (Exception ex)
                {
                    FileHelper.GeneratorFileByDay(FileStype.Log, $"Lỗi rồi : {ex.ToString()}", "SaveImage");
                }
            }

            responseData.status = "SUCCESS";
            responseData.data = JsonConvert.SerializeObject(listLinkUploaded);
            responseData.size = JsonConvert.SerializeObject(listFileLenght);
            responseData.message = $"uploaded {file.Count} files successful.";
            result = JsonConvert.SerializeObject(responseData);

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> UploadFilePartnerAsync(List<IFormFile> file, int width, string? Obj_Id, int type, int userId)
        {
            const bool AllowLimitSize = true;
            const bool AllowLimitFileType = true;

            var limitFileSize = 4194304; // allow upload file less 2MB = 2097152
            var listFileError = new List<FileUploadInfo>();
            var responseData = new ResponseData();
            string result = "";

            if (file.Count <= 0)
            {
                responseData.status = "ERROR";
                responseData.message = $"Please, select file to upload.";
                result = JsonConvert.SerializeObject(responseData);
                return Ok(result);
            }

            var listFileTypeAllow = "jpg|png|gif|xls|xlsx|jpeg|webp|jfif";
            // check file type upload allow
            if (AllowLimitFileType)
            {
                foreach (var formFile in file)
                {
                    var file_ext = Path.GetExtension(formFile.FileName).Replace(".", "");
                    var isAllow = listFileTypeAllow.Split('|').Any(x => x.ToLower() == file_ext.ToLower());
                    if (!isAllow)
                    {
                        listFileError.Add(new FileUploadInfo()
                        {
                            filename = formFile.FileName,
                            filesize = formFile.Length
                        });
                    }

                }
            }

            if (listFileError.Count > 0)
            {
                responseData.status = "ERROR";
                responseData.data = JsonConvert.SerializeObject(listFileError);
                responseData.message = $"File type upload only Allow Type: ({listFileTypeAllow}) \r\n {responseData.data}";
                result = JsonConvert.SerializeObject(responseData);
                return Ok(result);
            }

            // check list file less limit size
            if (AllowLimitSize)
            {
                foreach (var formFile in file)
                {
                    if (formFile.Length > limitFileSize)
                    {
                        listFileError.Add(new FileUploadInfo()
                        {
                            filename = formFile.FileName,
                            filesize = formFile.Length
                        });
                    }
                }

            }

            var listLinkUploaded = new List<string>();
            var listFileLenght = new List<int>();

            if (listFileError.Count > 0)
            {
                responseData.status = "ERROR";
                responseData.data = JsonConvert.SerializeObject(listFileError);
                responseData.message = $"File upload must less 4MB ({responseData.data})";
                result = JsonConvert.SerializeObject(responseData);
                return Ok(result);
            }

            string baseUrl = $"{this.Request.Scheme}://{this.Request.Host}{this.Request.PathBase}";

            int i = 0;
            string directoryPath = "";
            int filelenght = 0;
            string urlpath = "";

            if (type == 0)
            {
                directoryPath = $@"{_webHost.WebRootPath}\partner\hotel\{userId}\{Obj_Id}\";
            }
            if (type == 1)
            {
                directoryPath = $@"{_webHost.WebRootPath}\partner\room\{userId}\{Obj_Id}\";
            }
            if (type == 2)
            {
                directoryPath = $@"{_webHost.WebRootPath}\partner\tour\{userId}\{Obj_Id}\";
            }
            if (type == 3)
            {
                directoryPath = $@"{_webHost.WebRootPath}\partner\news\{userId}\{Obj_Id}\";
            }

            DirectoryInfo directory = new DirectoryInfo(directoryPath);

            if (!directory.Exists)
            {
                directory.Create();
            }

            foreach (var formFile in file)
            {
                FileHelper.GeneratorFileByDay(FileStype.Log, $"Số lượng file : {file.Count}", "SaveImage");
                try
                {
                    if (formFile.Length > 0)
                    {


                        var time = DateTime.UtcNow;
                        var timestring = $"{time}";

                        string[] charsToRemove = new string[] { "@", ",", ".", ";", "'", "/", ":", " " };
                        foreach (var c in charsToRemove)
                        {
                            timestring = timestring.Replace(c, string.Empty);
                        }
                        var templateUrl = Path.GetExtension(formFile.FileName).Replace(".", $"{timestring}00{i}.");

                        //var templateUrl = file.FileName ;
                        //string filePath = Path.Combine($"{_webHost.WebRootPath}/uploads/{path}", templateUrl);
                        string filePath = Path.Combine(directoryPath, templateUrl);

                        string fileName = Path.GetFileName(filePath);

                        using (var stream = System.IO.File.Create(filePath))
                        {

                            await formFile.CopyToAsync(stream);
                            filelenght = _uploadServices.ThayDoiKichThuocAnh(filePath, fileName, width, stream);
                            FileHelper.GeneratorFileByDay(FileStype.Log, $"thành công file {fileName}", "SaveImage");
                        }
                        listLinkUploaded.Add(directoryPath + templateUrl);
                        listFileLenght.Add(filelenght);
                        i++;
                    }
                }
                catch (Exception ex)
                {
                    FileHelper.GeneratorFileByDay(FileStype.Log, $"Lỗi rồi : {ex.ToString()}", "SaveImage");
                }
            }

            responseData.status = "SUCCESS";
            responseData.data = JsonConvert.SerializeObject(listLinkUploaded);
            responseData.size = JsonConvert.SerializeObject(listFileLenght);
            responseData.message = $"uploaded {file.Count} files successful.";
            result = JsonConvert.SerializeObject(responseData);

            return Ok(result);
        }

    }
}
