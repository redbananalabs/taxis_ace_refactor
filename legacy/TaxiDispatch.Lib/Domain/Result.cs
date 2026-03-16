
using System.ComponentModel;
using System.Runtime.CompilerServices;


namespace TaxiDispatch.Domain
{
    /// <summary>
    /// A generic class used to return the result of an operation
    /// 
    /// Example Usage
    /// <code>
    /// public <c>Result</c>><List<Example> GetExamples()
    /// {
    ///     try
    ///     {
    ///         var result = _db.GetValues();
    ///         
    ///         return result is null ? <c>Result</c>.Fail<List<Example>>("No Items found") : <c>Result</c>.Ok(result);
    ///     }
    ///     catch // without logging and reporting the exception
    ///     {
    ///         return <c>Result</c>.Fail<List<Example>>("Error completing operation",)     
    ///     }
    ///     catch(Exception ex)
    ///     {
    ///         return <c>Result</c>.Fail<List<Example>>("Error completing operation",ex)
    ///     }
    /// }
    /// </code>
    /// </summary>
    /// <typeparam name="T">Expected return value if operation completes successfully</typeparam>
    public class Result<T> : Result, IResult<T>
    {
        /// <summary>
        /// Creates a class holding the result or failure of an operation
        /// </summary>
        /// <param name="value">The value that will be returned</param>
        /// <param name="success">If the operation was successfull</param>
        /// <param name="error">The reason the operation failed</param>
        protected internal Result(T value, bool success, string error)
            : base(success, error)
        {
            Value = value;
        }

        /// <summary>
        /// Creates a class holding the result or failure of an operation
        /// </summary>
        /// <param name="value">The value that will be returned</param>
        /// <param name="success">If the operation was successfull</param>
        /// <param name="error">The reason the operation failed</param>
        /// <param name="exception">The Exception thrown if any</param>
        protected internal Result(T value, bool success, string error, Exception exception)
          : base(success, error)
        {
            Value = value;

            if (exception != null)
            {
                Exceptions.Add(exception);
            }
        }

        /// <summary>
        /// Creates a class holding the result or failure of an operation
        /// </summary>
        /// <param name="value">The value that will be returned</param>
        /// <param name="success">If the operation was successfull</param>
        /// <param name="error">The reason the operation failed</param>
        /// <param name="exceptions">The Exceptions thrown if any</param>
        protected internal Result(T value, bool success, string error, List<Exception> exceptions)
          : base(success, error)
        {
            Value = value;

            if (exceptions != null)
            {
                Exceptions = exceptions;
            }
        }

        /// <summary>
        /// Creates a class holding the failure of an operation
        /// </summary>
        /// <param name="value">The value that will be returned</param>
        /// <param name="exceptions">The Exceptions thrown if any</param>
        protected internal Result(T value, Exception exception)
          : base(exception)
        {
            Value = value;

            if (exception != null)
            {
                Exceptions.Add(exception);
            }
        }

        public T Value { get; }
    }

    /// <summary>
    /// A non-generic class used to return the result of an operation
    /// </summary>
    public class Result : INotifyPropertyChanged, IResult
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private bool _success;

        /// <summary>
        /// Indicates if the operation completed successfully or not
        /// </summary>
        public bool Success
        {
            get => _success;
            set
            {
                if (value != _success)
                {
                    _success = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// The reason the operation failed
        /// </summary>
        public string ErrorMessage { get; private set; }

        /// <summary>
        /// The Exceptions raised that caused the operation to fail
        /// </summary>
        public List<Exception> Exceptions { get; set; } = new List<Exception>();

        /// <summary>
        /// Whether the operation was successfull
        /// </summary>
        public bool IsFailure => !Success;

        protected Result(bool success, string error)
        {
            if (success && error != string.Empty)
                throw new InvalidOperationException();

            if (!success && error == string.Empty)
                throw new InvalidOperationException();

            Success = success;
            ErrorMessage = error;
        }

        protected Result(Exception exception)
        {
            if (exception == null)
                throw new ArgumentNullException("exception");

            Exceptions.Add(exception);
            ErrorMessage = "";
            Success = false;
        }

        protected Result(bool success, string error, Exception? exception = null)
        {
            if (success && error != string.Empty)
                throw new InvalidOperationException();

            if (!success && error == string.Empty)
                throw new InvalidOperationException();

            if (exception != null)
                Exceptions.Add(exception);

            Success = success;
            ErrorMessage = error;
        }

        protected Result(bool success, string error, List<Exception> exceptions = null)
        {
            if (success && error != string.Empty)
                throw new InvalidOperationException();

            if (!success && error == string.Empty)
                throw new InvalidOperationException();

            if (exceptions != null)
                Exceptions = exceptions;

            Success = success;
            ErrorMessage = error;
        }

        /// <summary>
        /// Creates a Result object containing the reason the operation failed
        /// </summary>
        /// <param name="message">The reason the operation failed</param>
        /// <returns></returns>
        public static Result Fail(string message)
        {
            return new Result(false, message);
        }

        /// <summary>
        /// Creates a Result object containing the reason the operation failed
        /// </summary>
        /// <param name="message">The reason the operation failed</param>
        /// <param name="exception">The Exception that caused the operation to fail</param>
        /// <returns></returns>
        public static Result Fail(string message, Exception exception)
        {
            return new Result(false, message, exception);
        }

        /// <summary>
        /// Creates a Result object containing the Exception that caused the operation to fail
        /// </summary>
        /// <param name="exception">The Exception that caused the operation to fail</param>
        /// <returns></returns>
        public static Result Fail(Exception exception)
        {
            return new Result(exception);
        }

        /// <summary>
        /// Creates a Result object containing the reason the operation failed
        /// </summary>
        /// <param name="message">The reason the operation failed</param>
        /// <param name="exceptions">The List<Exception> that caused the operation to fail</param>
        /// <returns></returns>
        public static Result Fail(string message, List<Exception> exceptions)
        {
            return new Result(false, message, exceptions);
        }

        /// <summary>
        /// Creates a Result object with the specified return type for the calling method with the reason the operation failed
        /// </summary>
        /// <param name="message">The reason the operation failed</param>
        /// <returns></returns>
        public static Result<T> Fail<T>(string message)
        {
            return new Result<T>(default(T), false, message);
        }

        /// <summary>
        /// Creates a Result object with the specified return type for the calling method with the reason the operation failed
        /// </summary>
        /// <param name="exception">The exception that caused the operation to fail</param>
        /// <returns></returns>
        public static Result<T> Fail<T>(Exception exception)
        {
            return new Result<T>(default(T), exception);
        }

        /// <summary>
        /// Creates a Result object with the specified return type for the calling method with the reason the operation failed and the exception that was thrown
        /// </summary>
        /// <param name="message">The reason the operation failed</param>
        /// <param name="exception">The Exception that caused the operation to fail</param>
        /// <returns></returns>
        public static Result<T> Fail<T>(string message, Exception exception)
        {
            return new Result<T>(default(T), false, message, exception);
        }

        /// <summary>
        /// Creates a Result object with the specified return type for the calling method with the reason the operation failed and the exception that was thrown
        /// </summary>
        /// <param name="message">The reason the operation failed</param>
        /// <param name="exceptions">The List of Exception that caused the operation to fail</param>
        /// <returns></returns>
        public static Result<T> Fail<T>(string message, List<Exception> exceptions)
        {
            return new Result<T>(default(T), false, message, exceptions);
        }

        /// <summary>
        /// Creates a Return object with the Success property set to true
        /// </summary>
        /// <returns></returns>
        public static Result Ok()
        {
            return new Result(true, string.Empty);
        }

        /// <summary>
        /// Creates a Return object with the specified return type for the calling method and the Success property set to true
        /// </summary>
        /// <returns></returns>
        public static Result<T> Ok<T>(T value)
        {
            return new Result<T>(value, true, string.Empty);
        }

        /// <summary>
        /// Returns the first Exception from the list if Any() or null
        /// </summary>
        /// <returns></returns>
        public Exception FirstException()
        {
            if (Exceptions.Any())
                return Exceptions.First();

            return null;
        }

        #region Private Methods
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }


        #endregion
    }

    public interface IResult<T>
    {
        T Value { get; }
    }

    public interface IResult
    {
        string ErrorMessage { get; }
        List<Exception> Exceptions { get; }
        bool IsFailure { get; }
        bool Success { get; set; }

        Exception FirstException();

        event PropertyChangedEventHandler? PropertyChanged;
    }
}

