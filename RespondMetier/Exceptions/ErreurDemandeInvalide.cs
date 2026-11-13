namespace RespondMetier.Exceptions
{
    /// <summary>
    /// Raised when a generation request cannot be used as it stands
    /// </summary>
    public class ErreurDemandeInvalide : Exception
    {
        /// <summary>
        /// Initializes the error with the reason shown to the user
        /// </summary>
        /// <param name="message">the reason the request cannot be used</param>
        public ErreurDemandeInvalide(string message) : base(message)
        {
        }
    }
}
