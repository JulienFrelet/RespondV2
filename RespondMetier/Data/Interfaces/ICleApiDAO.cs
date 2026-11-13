using RespondMetier.Models;

namespace RespondMetier.Data.Interfaces
{
    /// <summary>
    /// Defines the data access operations for API credentials.
    /// </summary>
    /// <remarks>
    /// Implementations receive and return credentials whose secret is in
    /// clear: encrypting before writing and decrypting after reading is their
    /// responsibility, not the caller's. The business layer never manipulates
    /// ciphertext, and no caller can forget to encrypt.
    /// </remarks>
    public interface ICleApiDAO
    {
        #region Methods

        /// <summary>
        /// Retrieves every registered credential
        /// </summary>
        /// <returns>the list of credentials, empty when none exists</returns>
        List<CleApi> Lister();

        /// <summary>
        /// Retrieves a credential using its identifier
        /// </summary>
        /// <param name="id">the identifier of the credential wanted</param>
        /// <returns>the matching credential, or null when none exists</returns>
        CleApi? ChargerParId(int id);

        /// <summary>
        /// Inserts a new credential and assigns it an identifier
        /// </summary>
        /// <param name="cleApi">the credential to insert, secret in clear</param>
        void Inserer(CleApi cleApi);

        /// <summary>
        /// Updates an existing credential
        /// </summary>
        /// <param name="cleApi">the credential to update, identified by its Id</param>
        void MettreAJour(CleApi cleApi);

        /// <summary>
        /// Deletes a credential
        /// </summary>
        /// <param name="id">the identifier of the credential to delete</param>
        void Supprimer(int id);

        #endregion
    }
}
