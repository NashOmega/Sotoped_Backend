using System.ComponentModel.DataAnnotations.Schema;

namespace Sotoped.Models
{
    [NotMapped]
    public class FileResponse
    {
        public bool OperationSucess { get; set; }

        public byte[] FileBytes { get; set; }

        public string? FileName { get; set; }

        
        public FileResponse()
        {

        }
        
        public FileResponse(bool operationSucess, byte[] fileBytes, string fileName)
        {
            OperationSucess = operationSucess;
            FileBytes = fileBytes;
            FileName = fileName;
        }
    }
}
