namespace RespondMetier.Data.Interfaces
{
    /// <summary>
    /// Protects a secret before it is written to disk, and reads it back.
    /// </summary>
    /// <remarks>
    /// Encryption is kept out of the DAO on purpose. Persisting rows and
    /// protecting secrets are two different responsibilities, and separating
    /// them means the credential DAO can be tested with a fake protector
    /// instead of requiring a real operating system keychain.
    /// <para>
    /// The concrete implementation uses the Windows DPAPI, which ties the
    /// ciphertext to the current user account: a copied file is unreadable on
    /// another machine or under another account.
    /// </para>
    /// </remarks>
    public interface IProtecteurSecret
    {
        #region Methods

        /// <summary>
        /// Gets whether the operating system can actually protect a secret
        /// </summary>
        /// <remarks>
        /// When this returns false the application must refuse to store the
        /// key rather than silently falling back to writing it in clear.
        /// </remarks>
        /// <returns>true when encryption is available</returns>
        bool EstDisponible();

        /// <summary>
        /// Encrypts a secret so that it can be written to disk
        /// </summary>
        /// <param name="valeurEnClair">the secret to protect</param>
        /// <returns>the encrypted form, safe to persist</returns>
        string Proteger(string valeurEnClair);

        /// <summary>
        /// Decrypts a secret read from disk
        /// </summary>
        /// <param name="valeurProtegee">the encrypted form read from storage</param>
        /// <returns>the secret in clear, to be kept in memory only</returns>
        string Lire(string valeurProtegee);

        #endregion
    }
}
