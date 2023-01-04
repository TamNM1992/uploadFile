using System.Drawing;
using System.IO;
using System.Drawing.Imaging;


namespace UploadFileAPI.Services
{
    public interface IUploadServices
    {
        public int ThayDoiKichThuocAnh(string ImageSavePath, string fileName, int MaxWidthSideSize, FileStream Buffer);

    }
}
