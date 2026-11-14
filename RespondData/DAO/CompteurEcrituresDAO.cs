using Microsoft.EntityFrameworkCore;
using RespondMetier.Data.Interfaces;
using RespondMetier.Models;

namespace RespondData.DAO
{
    /// <summary>
    /// Reads and writes the write counters in the database
    /// </summary>
    public class CompteurEcrituresDAO : ICompteurEcrituresDAO
    {
        #region Attributs

        private readonly AppDbContext context;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the CompteurEcrituresDAO class
        /// </summary>
        /// <param name="context">the database context</param>
        public CompteurEcrituresDAO(AppDbContext context)
        {
            this.context = context;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Retrieves the counter of one client address
        /// </summary>
        /// <param name="cle">the client address</param>
        /// <returns>the counter, or null when none exists yet</returns>
        public CompteurEcritures? Charger(string cle)
        {
            return this.context.Set<CompteurEcritures>()
                .FirstOrDefault(compteur => compteur.Cle == cle);
        }

        /// <summary>
        /// Saves a counter, inserting it when it is new
        /// </summary>
        /// <param name="compteur">the counter to persist</param>
        public void Enregistrer(CompteurEcritures compteur)
        {
            if (compteur.Id == 0)
            {
                this.context.Set<CompteurEcritures>().Add(compteur);
            }

            this.context.SaveChanges();
        }

        /// <summary>
        /// Deletes the counters whose window ended before the given instant
        /// </summary>
        /// <param name="avant">the instant before which counters are stale</param>
        public void Purger(DateTime avant)
        {
            this.context.Set<CompteurEcritures>()
                .Where(compteur => compteur.DebutFenetre < avant)
                .ExecuteDelete();
        }

        #endregion
    }
}
