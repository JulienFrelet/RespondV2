namespace RespondMetier.Exceptions
{
    /// <summary>
    /// Raised when the provider accepted the key but refuses more requests for now
    /// </summary>
    /// <remarks>
    /// A quota being reached proves the key is valid. On the public demo this
    /// is the expected end of the free allowance, so the message must tell the
    /// user to come back later or bring their own key, not that something is
    /// broken.
    /// </remarks>
    public class ErreurQuotaDepasse : ErreurFournisseurIA
    {
        /// <summary>
        /// Initializes the error with the reason shown to the user
        /// </summary>
        /// <param name="message">the reason no more requests are accepted</param>
        public ErreurQuotaDepasse(string message) : base(message)
        {
        }
    }
}
