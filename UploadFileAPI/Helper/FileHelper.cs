using Microsoft.VisualBasic.FileIO;
using System.Text;

namespace UploadFileAPI.Helper
{
    public class FileHelper
    {
        public static bool isStopped = false;
        public const string Utf8 = "utf-8";
        private const string SystemFolder = "System";
        private static object objWrite = true;
        public static int Version = 0;
        public static int countThread = 0;
        public static bool isWriteLogfile = false;
        /// <summary>
        /// Ghi một nội dung ra file
        /// Kiểm tra xem thư mục đã có chưa, thư mục chưa có thì tạo thư mục
        /// Kiểm tra file đã tồn tại chưa, chưa tồn tại thì tạo file
        /// </summary>
        public static bool WriteFile(string pathWrite, string content)
        {
            var result = false;

            lock (objWrite)
            {
                // Tạo thư mục nếu như thư mục chưa tồn tại
                var folder = Path.GetDirectoryName(pathWrite);
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                // Tạo file nếu như file chưa tồn tại
                if (!File.Exists(pathWrite))
                    using (var fs = new FileStream(pathWrite, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite)) fs.Close();

                // Thực hiện ghi file
                using (var sw = new StreamWriter(pathWrite, true, Encoding.GetEncoding(Utf8)))
                {
                    sw.WriteLine(content);
                    result = true;
                }
            }
            return result;
        }

        /// <summary>
        /// Ghi tổng hợp
        /// </summary>
        /// <param name="fileStype"></param>
        /// <param name="content"></param>
        /// <param name="folderName"></param>
        public static void GeneratorFileByDay(FileStype fileStype, string content, string folderName = "")
        {
            if (string.IsNullOrEmpty(folderName))
                folderName = SystemFolder;
            DateTime time = DateTime.Now;
            var fullPath = AppDomain.CurrentDomain.BaseDirectory + string.Format(@"{0}\{1}\{2}\{3}\{4}", fileStype.GetEnumDescription(), folderName, time.Year, time.Month, time.Day + ".txt");
            content = $"[{time.ToString("HH:mm:ss")}] " + content;
            WriteFile(fullPath, content);
        }
    }
}
