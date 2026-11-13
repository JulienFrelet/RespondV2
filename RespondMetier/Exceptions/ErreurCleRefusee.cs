namespace RespondMetier.Exceptions
{
    /// <summary>
    /// Raised when the provider rejects the API key
    /// </summary>
    /// <remarks>
    /// Distinct from a network failure on purpose: a key that cannot be
    /// checked because the network is down must never be reported to the user
    /// as invalid. Telling someone their key is wrong when it is not sends
    /// them hunting for a problem that does not exist.
    /// </remarks>
    public class ErreurCleRefusee : ErreurFournisseurIA
    {
        /// <summary>
        /// Initializes the error with the reason shown to the user
        /// </summary>
        /// <param name="message">the reason the key was refused</param>
        public ErreurCleRefusee(string message) : base(message)
        {
        }
    }
}
