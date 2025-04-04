using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sotoped.Models
{
    [Table("Spectator")]
    public class Spectator
    {
        [Key]
        public int Id { get; set; }
        public string? FullName { get; set; }

        public string? Email { get; set; }

        public string? Code { get; set; }
        public bool NephrologicParticipation { get; set; }

        public bool PainParticipation { get; set; }

        public bool CongressParticipation { get; set; }

        public string? FirstCommunication { get; set; }

        public string? SecondCommunication { get; set; }

        public string? ThirdCommunication { get; set; }

        public Spectator()
        {

        }
    }
}
