using RespondMetier.Models;

namespace RespondMetier.Data.Interfaces
{
    /// <summary>
    /// Defines the data access operations for the history of generated replies.
    /// </summary>
    /// <remarks>
    /// Every method is scoped to one visitor. The history holds the emails
    /// people pasted into the application, which is the most sensitive data it
    /// stores; a shared history on a public demonstration would let anyone
    /// read someone else's correspondence.
    /// </remarks>
    public interface IHistoriqueDAO
    {
        #region Methods

        /// <summary>
        /// Retrieves this visitor's entries, most recent first
        /// </summary>
        /// <param name="sessionId">the visitor's anonymous identifier</param>
        /// <returns>the entries, empty when none exists</returns>
        List<ReponseGeneree> Lister(string sessionId);

        /// <summary>
        /// Retrieves this visitor's most recent entries
        /// </summary>
        /// <param name="sessionId">the visitor's anonymous identifier</param>
        /// <param name="nbMax">the maximum number of entries wanted</param>
        /// <returns>the requested entries, most recent first</returns>
        List<ReponseGeneree> ListerPremiers(string sessionId, int nbMax);

        /// <summary>
        /// Searches this visitor's entries by subject, body, profile or credential name
        /// </summary>
        /// <param name="sessionId">the visitor's anonymous identifier</param>
        /// <param name="motCle">the keyword searched, case insensitive</param>
        /// <returns>the matching entries, most recent first</returns>
        List<ReponseGeneree> Rechercher(string sessionId, string motCle);

        /// <summary>
        /// Inserts a new entry and assigns it an identifier
        /// </summary>
        /// <param name="reponse">the entry to insert, with its SessionId set</param>
        void Inserer(ReponseGeneree reponse);

        /// <summary>
        /// Deletes one of this visitor's entries
        /// </summary>
        /// <param name="id">the identifier of the entry to delete</param>
        /// <param name="sessionId">the visitor's anonymous identifier</param>
        void Supprimer(int id, string sessionId);

        /// <summary>
        /// Deletes this visitor's oldest entries beyond the given number
        /// </summary>
        /// <remarks>
        /// The limit applies per visitor, not to the whole table: one busy
        /// visitor must not push another's entries out of their own history.
        /// </remarks>
        /// <param name="sessionId">the visitor's anonymous identifier</param>
        /// <param name="nbMax">the number of entries to keep</param>
        void LimiterA(string sessionId, int nbMax);

        #endregion
    }
}
