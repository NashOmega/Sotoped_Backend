using System.ComponentModel.DataAnnotations.Schema;

namespace Sotoped.Models
{
    [NotMapped]
    public class CertificateRequest
    {
        public string? Code { get; set; }

        public bool NephrologicParticipation { get; set; }

        public bool PainParticipation { get; set; }

        public bool CongressParticipation { get; set; }

        public bool FirstCommunication { get; set; }

        public bool SecondCommunication { get; set; }

        public bool ThirdCommunication { get; set; }
        
        public CertificateRequest()
        {

        }
    }
}
