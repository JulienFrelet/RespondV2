using RespondMetier.Models;

namespace RespondMetier.Services.Interfaces
{
    /// <summary>
    /// Produces a reply to an email and records it in the history.
    /// </summary>
    /// <remarks>
    /// The single entry point the presentation layer calls. It owns the order
    /// of operations, nothing else: building the prompt, calling the provider,
    /// splitting the answer and storing it are each delegated.
    /// </remarks>
    public interface IGenerationService
    {
        #region Methods

        /// <summary>
        /// Generates a reply, stores it, and returns it
        /// </summary>
        /// <param name="demande">the profile, rules and message to answer</param>
        /// <param name="nomCleApi">
        /// the label of the credential used, kept in the history so the user
        /// can tell which provider produced which reply
        /// </param>
        /// <param name="jeton">token used to abandon a request that takes too long</param>
        /// <returns>the subject and body of the generated reply</returns>
        /// <exception cref="Exceptions.ErreurDemandeInvalide">the request cannot be used</exception>
        /// <exception cref="Exceptions.ErreurFournisseurIA">the provider failed</exception>
        Task<ResultatGeneration> GenererAsync(
            DemandeGeneration demande, string nomCleApi, CancellationToken jeton);

        #endregion
    }
}
