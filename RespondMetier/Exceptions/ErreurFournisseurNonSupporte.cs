namespace RespondMetier.Exceptions
{
    /// <summary>
    /// Raised when no client exists yet for a provider
    /// </summary>
    /// <remarks>
    /// The Fournisseur enumeration lists the providers the application knows
    /// about as a domain concept; clients are written one at a time. Saying so
    /// explicitly is better than a silent fallback to another provider, which
    /// would send the user's data somewhere they did not choose.
    /// </remarks>
    public class ErreurFournisseurNonSupporte : ErreurFournisseurIA
    {
        /// <summary>
        /// Initializes the error with the reason shown to the user
        /// </summary>
        /// <param name="message">the provider that is not available</param>
        public ErreurFournisseurNonSupporte(string message) : base(message)
        {
        }
    }
}
