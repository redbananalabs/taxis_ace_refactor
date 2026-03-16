using TaxiDispatch.Domain;
using System.ComponentModel.DataAnnotations.Schema;


namespace TaxiDispatch.Data.Models
{
    public abstract class ModelBase : ModelValidator
    {
        public ModelBase()
        {
            //CreatedDate = DateTime.Now;
            //UpdatedDate = DateTime.Now;
        }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public virtual int Id { get; set; }
#if WINDOWS
        [SwaggerIgnoreProperty]
#endif
        public DateTime DateCreated { get; set; } = DateTime.Now.ToUKTime();
#if WINDOWS
        [SwaggerIgnoreProperty]
#endif
        public DateTime? DateUpdated { get; set; }
    }
}

