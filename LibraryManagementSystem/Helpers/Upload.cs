namespace LibraryManagementSystem.Helpers
{
    public static class Upload
    {
        public static string UploadFile(string FolderName, IFormFile File)
        {
            try
            {
                string FolderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", FolderName);


                if (!Directory.Exists(FolderPath))
                {
                    Directory.CreateDirectory(FolderPath);
                }

                string FileName = Guid.NewGuid().ToString() + Path.GetExtension(File.FileName);

                string FinalPath = Path.Combine(FolderPath, FileName);

                using (var Stream = new FileStream(FinalPath, FileMode.Create))
                {
                    File.CopyTo(Stream);
                }

                return FileName;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public static string RemoveFile(string FolderName, string fileName)
        {
            try
            {
                var directory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", FolderName, fileName);

                if (File.Exists(directory))
                {
                    File.Delete(directory);
                    return "File Deleted";
                }

                return "File Not Deleted";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
    }
}