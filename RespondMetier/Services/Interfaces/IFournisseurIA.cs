using RespondMetier.Models;

namespace RespondMetier.Services.Interfaces
{
    /// <summary>
    /// Sends a prompt to an AI provider and returns the text it produced.
    /// </summary>
    /// <remarks>
    /// One implementation per provider. Adding a provider means adding a class
    /// that implements this interface and registering it: no existing file is
    /// modified, and in particular there is no growing switch on the provider
    /// name anywhere in the business layer.
    /// <para>
    /// Every implementation shares the same error contract, the exceptions
    /// derived from <see cref="Exceptions.ErreurFournisseurIA"/>. A caller must
    /// never have to know which implementation it holds in order to handle a
    /// failure, and no implementation may invent an exception of its own.
    /// </para>
    /// </remarks>
    public interface IFournisseurIA
    {
        #region Methods

        /// <summary>
        /// Generates a reply from a prompt
        /// </summary>
        /// <param name="prompt">the instructions and the text to analyse</param>
        /// <param name="jeton">token used to abandon a request that takes too long</param>
        /// <returns>the text produced by the model, never empty</returns>
        /// <exception cref="Exceptions.ErreurCleRefusee">the provider rejected the key</exception>
        /// <exception cref="Exceptions.ErreurQuotaDepasse">the key is valid but out of quota</exception>
        /// <exception cref="Exceptions.ErreurAucunModeleDisponible">no configured model exists any more</exception>
        /// <exception cref="Exceptions.ErreurFournisseurIndisponible">the provider could not be reached in time</exception>
        /// <exception cref="Exceptions.ErreurReponseVide">the provider answered without usable text</exception>
        Task<string> GenererAsync(PromptGeneration prompt, CancellationToken jeton);

        #endregion
    }
}
