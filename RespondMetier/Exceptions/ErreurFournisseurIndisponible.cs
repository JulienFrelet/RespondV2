namespace RespondMetier.Exceptions
{
    /// <summary>
    /// Raised when the provider could not be reached or did not answer in time
    /// </summary>
    /// <remarks>
    /// Covers network failures, timeouts and provider side errors. All three
    /// share the same meaning for the user: nothing is wrong with their input
    /// or their key, and retrying later may work.
    /// </remarks>
    public class ErreurFournisseurIndisponible : ErreurFournisseurIA
    {
        /// <summary>
        /// Initializes the error with the reason shown to the user
        /// </summary>
        /// <param name="message">the reason the provider could not answer</param>
        public ErreurFournisseurIndisponible(string message) : base(message)
        {
        }
    }
}
