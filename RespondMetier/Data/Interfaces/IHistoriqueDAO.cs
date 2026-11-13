using RespondMetier.Models;

namespace RespondMetier.Data.Interfaces
{
    /// <summary>
    /// Defines the data access operations for the history of generated replies.
    /// </summary>
    public interface IHistoriqueDAO
    {
        #region Methods

        /// <summary>
        /// Retrieves every entry, most recent first
        /// </summary>
        /// <returns>the list of entries, empty when none exists</returns>
        List<ReponseGeneree> Lister();

        /// <summary>
        /// Retrieves the most recent entries
        /// </summary>
        /// <param name="nbMax">the maximum number of entries wanted</param>
        /// <returns>the requested entries, most recent first</returns>
        List<ReponseGeneree> ListerPremiers(int nbMax);

        /// <summary>
        /// Searches entries by subject, body, profile name or credential name
        /// </summary>
        /// <param name="motCle">the keyword searched, case insensitive</param>
        /// <returns>the matching entries, most recent first</returns>
        List<ReponseGeneree> Rechercher(string motCle);

        /// <summary>
        /// Inserts a new entry and assigns it an identifier
        /// </summary>
        /// <param name="reponse">the entry to insert</param>
        void Inserer(ReponseGeneree reponse);

        /// <summary>
        /// Deletes a single entry
        /// </summary>
        /// <param name="id">the identifier of the entry to delete</param>
        void Supprimer(int id);

        /// <summary>
        /// Deletes the oldest entries so that at most the given number remains
        /// </summary>
        /// <remarks>
        /// The history has a user configurable size limit. Trimming belongs to
        /// the data layer because doing it in a single statement is far cheaper
        /// than loading every entry into memory to count them.
        /// </remarks>
        /// <param name="nbMax">the number of entries to keep</param>
        void LimiterA(int nbMax);

        #endregion
    }
}
