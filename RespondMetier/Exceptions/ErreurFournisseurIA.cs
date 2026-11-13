namespace RespondMetier.Exceptions
{
    /// <summary>
    /// Base of every failure coming from an AI provider.
    /// </summary>
    /// <remarks>
    /// The business layer must not know that providers are reached over HTTP.
    /// Implementations therefore translate transport failures into these
    /// exceptions, and callers catch this base type to handle any provider
    /// failure at once without enumerating status codes.
    /// </remarks>
    public abstract class ErreurFournisseurIA : Exception
    {
        /// <summary>
        /// Initializes the error with the reason shown to the user
        /// </summary>
        /// <param name="message">the reason the generation failed</param>
        protected ErreurFournisseurIA(string message) : base(message)
        {
        }
    }
}
