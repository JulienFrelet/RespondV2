using RespondMetier.Models;

namespace RespondMetier.Data.Interfaces
{
    /// <summary>
    /// Defines the data access operations for company profiles.
    /// </summary>
    /// <remarks>
    /// This interface is declared by the business layer and implemented by the
    /// data layer. The business layer therefore never knows whether profiles
    /// live in SQLite, in a file or in memory, which is what lets it be tested
    /// without a database.
    /// <para>
    /// Every method takes the visitor's session identifier. The application
    /// has no accounts, so that identifier is the only thing separating one
    /// visitor's profiles from another's: making it a required argument rather
    /// than an optional filter means a caller cannot forget it and silently
    /// expose everyone's data.
    /// </para>
    /// </remarks>
    public interface IProfilDAO
    {
        #region Methods

        /// <summary>
        /// Retrieves the demonstration profiles plus those of this visitor
        /// </summary>
        /// <param name="sessionId">the visitor's anonymous identifier</param>
        /// <returns>the visible profiles, sorted by name</returns>
        List<Profil> Lister(string sessionId);

        /// <summary>
        /// Retrieves a profile the visitor is allowed to see
        /// </summary>
        /// <param name="id">the identifier of the profile wanted</param>
        /// <param name="sessionId">the visitor's anonymous identifier</param>
        /// <returns>
        /// the profile when it is a demonstration one or belongs to this
        /// visitor, null otherwise. Another visitor's profile is reported as
        /// absent rather than forbidden, so that its existence stays unknown
        /// </returns>
        Profil? ChargerParId(int id, string sessionId);

        /// <summary>
        /// Inserts a new profile and assigns it an identifier
        /// </summary>
        /// <param name="profil">the profile to insert, with its SessionId set</param>
        void Inserer(Profil profil);

        /// <summary>
        /// Updates a profile belonging to this visitor
        /// </summary>
        /// <param name="profil">the profile to update, identified by its Id</param>
        /// <param name="sessionId">the visitor's anonymous identifier</param>
        /// <exception cref="Exceptions.ErreurProfilEnLectureSeule">
        /// when the profile is a demonstration one
        /// </exception>
        void MettreAJour(Profil profil, string sessionId);

        /// <summary>
        /// Deletes a profile belonging to this visitor
        /// </summary>
        /// <param name="id">the identifier of the profile to delete</param>
        /// <param name="sessionId">the visitor's anonymous identifier</param>
        /// <exception cref="Exceptions.ErreurProfilEnLectureSeule">
        /// when the profile is a demonstration one
        /// </exception>
        void Supprimer(int id, string sessionId);

        #endregion
    }
}
