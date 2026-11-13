using RespondMetier.Data.Interfaces;
using RespondMetier.Models;

namespace RespondTests.Fakes
{
    /// <summary>
    /// History storage replaced by an in-memory list
    /// </summary>
    internal class FauxHistoriqueDAO : IHistoriqueDAO
    {
        private readonly List<ReponseGeneree> entrees;
        private int prochainId;
        private int derniereLimiteAppliquee;

        /// <summary>
        /// Initializes the fake with an empty history
        /// </summary>
        public FauxHistoriqueDAO()
        {
            this.entrees = new List<ReponseGeneree>();
            this.prochainId = 1;
            this.derniereLimiteAppliquee = -1;
        }

        /// <summary>
        /// Gets the entries currently stored
        /// </summary>
        public List<ReponseGeneree> Entrees
        {
            get { return this.entrees; }
        }

        /// <summary>
        /// Gets the last limit the service asked for, -1 when never asked.
        /// Lets a test check that trimming was requested at all.
        /// </summary>
        public int DerniereLimiteAppliquee
        {
            get { return this.derniereLimiteAppliquee; }
        }

        /// <summary>
        /// Returns every entry, most recent first
        /// </summary>
        /// <returns>the stored entries</returns>
        public List<ReponseGeneree> Lister()
        {
            return this.entrees.OrderByDescending(entree => entree.Date).ToList();
        }

        /// <summary>
        /// Returns the most recent entries
        /// </summary>
        /// <param name="nbMax">the maximum number wanted</param>
        /// <returns>the requested entries</returns>
        public List<ReponseGeneree> ListerPremiers(int nbMax)
        {
            return nbMax <= 0 ? new List<ReponseGeneree>() : this.Lister().Take(nbMax).ToList();
        }

        /// <summary>
        /// Searches entries on subject and body
        /// </summary>
        /// <param name="motCle">the keyword searched</param>
        /// <returns>the matching entries</returns>
        public List<ReponseGeneree> Rechercher(string motCle)
        {
            if (string.IsNullOrWhiteSpace(motCle))
            {
                return this.Lister();
            }

            return this.Lister()
                .Where(entree => entree.Objet.Contains(motCle, StringComparison.OrdinalIgnoreCase)
                              || entree.Texte.Contains(motCle, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        /// <summary>
        /// Adds an entry and assigns it an identifier
        /// </summary>
        /// <param name="reponse">the entry to add</param>
        public void Inserer(ReponseGeneree reponse)
        {
            reponse.Id = this.prochainId;
            this.prochainId++;
            this.entrees.Add(reponse);
        }

        /// <summary>
        /// Removes a single entry
        /// </summary>
        /// <param name="id">the identifier of the entry to remove</param>
        public void Supprimer(int id)
        {
            ReponseGeneree? existante = this.entrees.FirstOrDefault(entree => entree.Id == id);

            if (existante == null)
            {
                throw new InvalidOperationException(
                    "Impossible de supprimer une entree d'historique qui n'existe pas.");
            }

            this.entrees.Remove(existante);
        }

        /// <summary>
        /// Keeps only the most recent entries, and records the limit asked for
        /// </summary>
        /// <param name="nbMax">the number of entries to keep</param>
        public void LimiterA(int nbMax)
        {
            this.derniereLimiteAppliquee = nbMax;

            if (nbMax < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(nbMax));
            }

            List<ReponseGeneree> aGarder = this.Lister().Take(nbMax).ToList();
            this.entrees.Clear();
            this.entrees.AddRange(aGarder);
        }
    }
}
