namespace RespondMetier.Services.Interfaces
{
    /// <summary>
    /// Decides whether a write coming from one client address is allowed
    /// </summary>
    public interface ILimiteurEcritures
    {
        #region Methods

        /// <summary>
        /// Counts one write and tells whether it is allowed
        /// </summary>
        /// <param name="cle">the client address the write comes from</param>
        /// <returns>true when the write may proceed, false when the limit is reached</returns>
        bool Autoriser(string cle);

        #endregion
    }
}
