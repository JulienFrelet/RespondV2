namespace RespondAPI.Services
{
    /// <summary>
    /// Supplies the anonymous identifier separating one visitor's data from another's
    /// </summary>
    public interface ISessionVisiteur
    {
        #region Methods

        /// <summary>
        /// Returns the visitor's identifier, creating one when needed
        /// </summary>
        /// <remarks>
        /// Never returns an empty value: an empty identifier marks the shared
        /// demonstration profiles, and returning one would grant write access
        /// to them.
        /// </remarks>
        /// <param name="contexte">the current HTTP context</param>
        /// <returns>the visitor's identifier</returns>
        string ObtenirOuCreer(HttpContext contexte);

        #endregion
    }
}
