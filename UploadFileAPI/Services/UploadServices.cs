using System.Drawing;
using System.IO;
using System.Drawing.Imaging;
using Microsoft.AspNetCore.Http;

namespace UploadFileAPI.Services
{
    public class UploadServices : IUploadServices
    {
        public int ThayDoiKichThuocAnh(string ImageSavePath, string fileName, int MaxWidthSideSize, FileStream Buffer)
        {
            int size;
            int intNewWidth;
            int intNewHeight;
            System.Drawing.Image imgInput = System.Drawing.Image.FromStream(Buffer);
            ImageCodecInfo myImageCodecInfo;
            myImageCodecInfo = GetEncoderInfo("image/jpeg");
            //
            System.Drawing.Imaging.Encoder myEncoder = System.Drawing.Imaging.Encoder.Quality;
            EncoderParameters myEncoderParameters = new EncoderParameters(1);
            EncoderParameter myEncoderParameter;
            //Giá trị width và height nguyên thủy của ảnh;
            int intOldWidth = imgInput.Width;
            int intOldHeight = imgInput.Height;

            //Kiểm tra xem ảnh ngang hay dọc;
            int intMaxSide;
            /*if (intOldWidth >= intOldHeight)
            {
            intMaxSide = intOldWidth;
            }
            else
            {
            intMaxSide = intOldHeight;
            }*/
            //Để xác định xử lý ảnh theo width hay height thì bạn bỏ note phần trên;
            //Ở đây mình chỉ sử dụng theo width nên gán luôn intMaxSide= intOldWidth; ^^;
            intMaxSide = intOldWidth;
            if (intMaxSide > MaxWidthSideSize)
            {
                //Gán width và height mới.
                double dblCoef = MaxWidthSideSize / (double)intMaxSide;
                intNewWidth = Convert.ToInt32(dblCoef * intOldWidth);
                intNewHeight = Convert.ToInt32(dblCoef * intOldHeight);
            }
            else
            {
                //Nếu kích thước width/height (intMaxSide) cũ ảnh nhỏ hơn MaxWidthSideSize thì giữ nguyên //kích thước cũ;
                intNewWidth = intOldWidth;
                intNewHeight = intOldHeight;
            }

            //Tạo một ảnh bitmap mới;
            Bitmap bmpResized = new Bitmap(imgInput, intNewWidth, intNewHeight);
            Buffer.Close();
            //Phần EncoderParameter cho phép bạn chỉnh chất lượng hình ảnh ở đây mình để chất lượng tốt //nhất là 100L;
            myEncoderParameter = new EncoderParameter(myEncoder, 75L);
            myEncoderParameters.Param[0] = myEncoderParameter;
            //Lưu ảnh;
            //bmpResized.CopyTo(Buffer);
            //File.WriteAllBytes(ImageSavePath, picBytes);
            bmpResized.Save(ImageSavePath, myImageCodecInfo, myEncoderParameters);
            FileInfo f_info = new FileInfo(ImageSavePath);
            int FileLength = (int)f_info.Length;
            //Giải phóng tài nguyên;
            //Buffer.Close();
            imgInput.Dispose();
            bmpResized.Dispose();
            return FileLength;
        }
        private ImageCodecInfo GetEncoderInfo(String mimeType)
        {
            int j;
            ImageCodecInfo[] encoders;
            encoders = ImageCodecInfo.GetImageEncoders();
            for (j = 0; j < encoders.Length; ++j)
            {
                if (encoders[j].MimeType == mimeType)
                    return encoders[j];
            }
            return null;
        }
    }
}
