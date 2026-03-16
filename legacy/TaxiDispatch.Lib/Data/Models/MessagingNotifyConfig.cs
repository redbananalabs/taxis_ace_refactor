using TaxiDispatch.Domain;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaxiDispatch.Data.Models
{
    public class MessagingNotifyConfig
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public SendMessageOfType DriverOnAllocate { get; set; } = SendMessageOfType.None;

        [Required]
        public SendMessageOfType DriverOnUnAllocate { get; set; } = SendMessageOfType.None;

        [Required]
        public SendMessageOfType CustomerOnAllocate { get; set; } = SendMessageOfType.None;

        [Required]
        public SendMessageOfType CustomerOnUnAllocate { get; set; } = SendMessageOfType.None;

        [Required]
        public SendMessageOfType DriverOnAmend { get; set; } = SendMessageOfType.None;

        [Required]
        public SendMessageOfType CustomerOnAmend { get; set; } = SendMessageOfType.None;

        [Required]
        public SendMessageOfType DriverOnCancel { get; set; } = SendMessageOfType.None;

        [Required]
        public SendMessageOfType CustomerOnCancel { get; set; } = SendMessageOfType.None;

        [Required]
        public SendMessageOfType CustomerOnComplete { get; set; } = SendMessageOfType.None;

        public string IgnoreAccountNos { get; set; }

        public DateTime? SmsPhoneHeartBeat { get; set; }

    }
}

