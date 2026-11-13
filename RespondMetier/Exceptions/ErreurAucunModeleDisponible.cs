namespace RespondMetier.Exceptions
{
    /// <summary>
    /// Raised when every model configured for a provider was refused as unknown
    /// </summary>
    /// <remarks>
    /// Providers retire models without notice. The first version of this
    /// application had a single model identifier written in the code, and the
    /// day the provider removed it the application simply stopped working with
    /// an error nobody could act on. A list of models tried in order fixes the
    /// common case; this exception covers the one where none of them is left,
    /// and says so clearly instead of blaming the key or the network.
    /// </remarks>
    public class ErreurAucunModeleDisponible : ErreurFournisseurIA
    {
        /// <summary>
        /// Initializes the error with the reason shown to the user
        /// </summary>
        /// <param name="message">the reason no model could answer</param>
        public ErreurAucunModeleDisponible(string message) : base(message)
        {
        }
    }
}
