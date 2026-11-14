using RespondMetier.Data.Interfaces;
using RespondMetier.Models;

namespace RespondTests.Fakes
{
    /// <summary>
    /// History storage replaced by an in-memory list.
    /// </summary>
    /// <remarks>
    /// Scoping by session is reproduced faithfully rather than ignored: a fake
    /// that returns everything would let a test pass while the real isolation
    /// was broken.
    /// </remarks>
    internal class FauxHistoriqueDAO : IHistoriqueDAO
    {
        private readonly List<ReponseGeneree> entrees;
        private int prochainId;
        private int derniereLimiteAppliquee;
        private string derniereSessionLimitee;

        /// <summary>
        /// Initializes the fake with an empty history
        /// </summary>
        public FauxHistoriqueDAO()
        {
            this.entrees = new List<ReponseGeneree>();
            this.prochainId = 1;
            this.derniereLimiteAppliquee = -1;
            this.derniereSessionLimitee = string.Empty;
        }

        /// <summary>
        /// Gets every entry stored, all sessions mixed. Used by tests to check
        /// what was written, never to simulate a read.
        /// </summary>
        public List<ReponseGeneree> Entrees
        {
            get { return this.entrees; }
        }

        /// <summary>
        /// Gets the last limit the service asked for, -1 when never asked
        /// </summary>
        public int DerniereLimiteAppliquee
        {
            get { return this.derniereLimiteAppliquee; }
        }

        /// <summary>
        /// Gets the session the last trim was applied to
        /// </summary>
        public string DerniereSessionLimitee
        {
            get { return this.derniereSessionLimitee; }
        }

        /// <summary>
        /// Returns this visitor's entries, most recent first
        /// </summary>
        /// <param name="sessionId">the visitor's identifier</param>
        /// <returns>the stored entries</returns>
        public List<ReponseGeneree> Lister(string sessionId)
        {
            return this.entrees
                .Where(entree => entree.SessionId == sessionId)
                .OrderByDescending(entree => entree.Date)
                .ToList();
        }

        /// <summary>
        /// Returns this visitor's most recent entries
        /// </summary>
        /// <param name="sessionId">the visitor's identifier</param>
        /// <param name="nbMax">the maximum number wanted</param>
        /// <returns>the requested entries</returns>
        public List<ReponseGeneree> ListerPremiers(string sessionId, int nbMax)
        {
            return nbMax <= 0
                ? new List<ReponseGeneree>()
                : this.Lister(sessionId).Take(nbMax).ToList();
        }

        /// <summary>
        /// Searches this visitor's entries on subject and body
        /// </summary>
        /// <param name="sessionId">the visitor's identifier</param>
        /// <param name="motCle">the keyword searched</param>
        /// <returns>the matching entries</returns>
        public List<ReponseGeneree> Rechercher(string sessionId, string motCle)
        {
            if (string.IsNullOrWhiteSpace(motCle))
            {
                return this.Lister(sessionId);
            }

            return this.Lister(sessionId)
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
        /// Removes one of this visitor's entries
        /// </summary>
        /// <param name="id">the identifier of the entry to remove</param>
        /// <param name="sessionId">the visitor's identifier</param>
        public void Supprimer(int id, string sessionId)
        {
            ReponseGeneree? existante = this.entrees.FirstOrDefault(
                entree => entree.Id == id && entree.SessionId == sessionId);

            if (existante == null)
            {
                throw new InvalidOperationException(
                    "Cette entree d'historique n'existe pas.");
            }

            this.entrees.Remove(existante);
        }

        /// <summary>
        /// Keeps only this visitor's most recent entries, and records the call
        /// </summary>
        /// <param name="sessionId">the visitor's identifier</param>
        /// <param name="nbMax">the number of entries to keep</param>
        public void LimiterA(string sessionId, int nbMax)
        {
            this.derniereLimiteAppliquee = nbMax;
            this.derniereSessionLimitee = sessionId;

            if (nbMax < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(nbMax));
            }

            List<ReponseGeneree> aGarder = this.Lister(sessionId).Take(nbMax).ToList();

            this.entrees.RemoveAll(entree => entree.SessionId == sessionId);
            this.entrees.AddRange(aGarder);
        }
    }
}
