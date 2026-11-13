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
    /// </remarks>
    public interface IProfilDAO
    {
        #region Methods

        /// <summary>
        /// Retrieves every profile
        /// </summary>
        /// <returns>the list of profiles, empty when none exists</returns>
        List<Profil> Lister();

        /// <summary>
        /// Retrieves a profile using its identifier
        /// </summary>
        /// <param name="id">the identifier of the profile wanted</param>
        /// <returns>the matching profile, or null when none exists</returns>
        Profil? ChargerParId(int id);

        /// <summary>
        /// Inserts a new profile and assigns it an identifier
        /// </summary>
        /// <param name="profil">the profile to insert</param>
        void Inserer(Profil profil);

        /// <summary>
        /// Updates an existing profile
        /// </summary>
        /// <param name="profil">the profile to update, identified by its Id</param>
        void MettreAJour(Profil profil);

        /// <summary>
        /// Deletes a profile
        /// </summary>
        /// <param name="id">the identifier of the profile to delete</param>
        void Supprimer(int id);

        #endregion
    }
}
