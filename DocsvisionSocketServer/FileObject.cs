using System;
using System.IO;
using DocsVision.Platform.ObjectManager;

namespace DocsvisionSocketServer
{
    class FileObject
    {
        private static UserSession Session => SessionManager.Session;

        private readonly FileData fileData = null;

        public FileObject(string fileId)
        {
            this.fileData = Session.FileManager.GetFile(new Guid(fileId));
        }
       
        public byte[] AsByteArray()
        {           
            using (Stream input = this.fileData.OpenReadStream())
            {
                using (var memoryStream = new MemoryStream())
                {
                    input.CopyTo(memoryStream);
                    return memoryStream.ToArray();
                }
            }  
        }
    }
}
