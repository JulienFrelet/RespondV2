using RespondMetier.Models;

namespace RespondMetier.Services.Interfaces
{
    /// <summary>
    /// Builds the client able to talk to a given provider with a given key.
    /// </summary>
    /// <remarks>
    /// A provider client cannot be injected directly, because which provider
    /// to use and which key to use are only known when the request arrives:
    /// the shared demo key for a visitor, or a key the visitor supplied for
    /// their own request. The factory keeps that choice out of the business
    /// service, which goes on depending only on the interface.
    /// </remarks>
    public interface IFabriqueFournisseurIA
    {
        #region Methods

        /// <summary>
        /// Builds a client for a provider
        /// </summary>
        /// <param name="fournisseur">the provider to reach</param>
        /// <param name="cle">the API key authorising the request</param>
        /// <returns>a client ready to generate</returns>
        /// <exception cref="Exceptions.ErreurFournisseurNonSupporte">
        /// when no client exists yet for this provider
        /// </exception>
        IFournisseurIA Creer(Fournisseur fournisseur, string cle);

        #endregion
    }
}
