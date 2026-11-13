namespace RespondMetier.Exceptions
{
    /// <summary>
    /// Raised when the provider answered successfully but with no usable text
    /// </summary>
    /// <remarks>
    /// This happens when a reasoning model fills only its reasoning field, or
    /// when a safety filter empties the answer. The call succeeded at the
    /// transport level, so without this check the user would be handed an
    /// empty reply and left to wonder why.
    /// </remarks>
    public class ErreurReponseVide : ErreurFournisseurIA
    {
        /// <summary>
        /// Initializes the error with the reason shown to the user
        /// </summary>
        /// <param name="message">the reason the answer was unusable</param>
        public ErreurReponseVide(string message) : base(message)
        {
        }
    }
}
