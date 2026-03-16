using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using System.Text.Json.Serialization;

namespace TaxiDispatch.Domain
{
    public interface IModelValidator
    {
        [JsonIgnore]
        List<ValidationResult> Errors { get; set; }

        [JsonIgnore]
        bool IsValid { get; }

        bool Validate();
    }


    public class ModelValidator : IModelValidator
    {
        //[Write(false)]
        [NotMapped]
        [JsonIgnore]
        public virtual List<ValidationResult> Errors { get; set; }

        public ModelValidator()
        {
            Errors = new List<ValidationResult>();
        }

        [JsonIgnore]
        public string Error
        {
            get
            {
                var str = string.Empty;

                if (Errors.Any())
                {
                    foreach (var error in Errors)
                    {
                        str += error.ErrorMessage;
                    }
                }

                return str;
            }
        }

        /// <summary>
        /// Validate the model and return a response if the data is valid
        /// </summary>
        public bool Validate()
        {
            var context = new ValidationContext(this);

            var isValid = Validator.TryValidateObject(this, context, Errors, true);

            return isValid;
        }

        /// <summary>
        /// Validate the model and return a bit indicating whether the model is valid or not.
        /// </summary>
        //[Write(false)]
        [NotMapped]
        [JsonIgnore]
        public bool IsValid
        {
            get
            {
                var response = Validate();
                return response;
            }
        }
    }

    public class ValidationException : Exception
    {
        public string? Errors { get; set; }

        public ValidationException()
        {

        }

        public ValidationException(string message) : base(message)
        {

        }

        public ValidationException(string message, Exception exception) : base(message, exception)
        {

        }

        public ValidationException(IList<ValidationResult> results, string message = "The model is invalid") : this(message)
        {
            var sb = new StringBuilder();

            foreach (var item in results)
            {
                sb.AppendLine(item.ErrorMessage);
            }

            Errors = sb.ToString();
        }
    }
}

