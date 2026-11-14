using RespondMetier.Models;

namespace RespondMetier.Data.Interfaces
{
    /// <summary>
    /// Defines the data access operations for the write counters
    /// </summary>
    public interface ICompteurEcrituresDAO
    {
        #region Methods

        /// <summary>
        /// Retrieves the counter of one client address
        /// </summary>
        /// <param name="cle">the client address</param>
        /// <returns>the counter, or null when none exists yet</returns>
        CompteurEcritures? Charger(string cle);

        /// <summary>
        /// Saves a counter, inserting it when it is new
        /// </summary>
        /// <param name="compteur">the counter to persist</param>
        void Enregistrer(CompteurEcritures compteur);

        /// <summary>
        /// Deletes the counters whose window ended before the given instant
        /// </summary>
        /// <remarks>
        /// Sans ce menage, la table garderait une ligne par adresse ayant
        /// ecrit un jour, pour toujours.
        /// </remarks>
        /// <param name="avant">the instant before which counters are stale</param>
        void Purger(DateTime avant);

        #endregion
    }
}
