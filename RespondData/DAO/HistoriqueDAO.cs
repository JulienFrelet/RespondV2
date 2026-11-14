using Microsoft.EntityFrameworkCore;
using RespondMetier.Data.Interfaces;
using RespondMetier.Models;

namespace RespondData.DAO
{
    /// <summary>
    /// Provides data access operations for the history of generated replies
    /// </summary>
    public class HistoriqueDAO : IHistoriqueDAO
    {
        #region Properties

        private readonly AppDbContext context;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the HistoriqueDAO class
        /// </summary>
        /// <param name="context">The database context</param>
        public HistoriqueDAO(AppDbContext context)
        {
            this.context = context;
        }

        #endregion

        #region Methods

        public List<ReponseGeneree> Lister(string sessionId)
        {
            return this.PourSession(sessionId)
                .OrderByDescending(reponse => reponse.Date)
                .ToList();
        }

        public List<ReponseGeneree> ListerPremiers(string sessionId, int nbMax)
        {
            if (nbMax <= 0)
            {
                return new List<ReponseGeneree>();
            }

            return this.PourSession(sessionId)
                .OrderByDescending(reponse => reponse.Date)
                .Take(nbMax)
                .ToList();
        }

        public List<ReponseGeneree> Rechercher(string sessionId, string motCle)
        {
            if (string.IsNullOrWhiteSpace(motCle))
            {
                return this.Lister(sessionId);
            }

            // EF.Functions.Like maps to the SQL LIKE operator, which SQLite
            // applies case insensitively on ASCII. Doing the filtering in SQL
            // rather than in memory matters once the history grows.
            // Les jokers de LIKE sont echappes : sans cela, une recherche sur
            // « % » remonterait tout l'historique, et « _ » ferait correspondre
            // n'importe quel caractere. Ce n'est pas une injection, EF
            // parametre la requete, mais le resultat serait faux.
            string motif = "%" + EchapperJokers(motCle.Trim()) + "%";

            return this.PourSession(sessionId)
                .Where(reponse => EF.Functions.Like(reponse.Objet, motif, "\\")
                               || EF.Functions.Like(reponse.Texte, motif, "\\")
                               || EF.Functions.Like(reponse.NomProfil, motif, "\\")
                               || EF.Functions.Like(reponse.NomCleApi, motif, "\\"))
                .OrderByDescending(reponse => reponse.Date)
                .ToList();
        }

        public void Inserer(ReponseGeneree reponse)
        {
            this.context.Set<ReponseGeneree>().Add(reponse);
            this.context.SaveChanges();
        }

        public void Supprimer(int id, string sessionId)
        {
            ReponseGeneree? existante = this.PourSession(sessionId)
                .FirstOrDefault(reponse => reponse.Id == id);

            if (existante == null)
            {
                // Another visitor's entry is reported as absent rather than
                // forbidden: its existence must not be observable.
                throw new InvalidOperationException(
                    "Cette entrée d'historique n'existe pas.");
            }

            this.context.Set<ReponseGeneree>().Remove(existante);
            this.context.SaveChanges();
        }

        public void LimiterA(string sessionId, int nbMax)
        {
            if (nbMax < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(nbMax), "La taille maximale de l'historique ne peut pas être négative.");
            }

            // Only the entries beyond the limit are loaded, not the whole
            // history, and only this visitor's: one busy visitor must never
            // push another's entries out.
            List<ReponseGeneree> aSupprimer = this.PourSession(sessionId)
                .OrderByDescending(reponse => reponse.Date)
                .Skip(nbMax)
                .ToList();

            if (aSupprimer.Count == 0)
            {
                return;
            }

            this.context.Set<ReponseGeneree>().RemoveRange(aSupprimer);
            this.context.SaveChanges();
        }

        /// <summary>
        /// Escapes the characters LIKE treats as wildcards
        /// </summary>
        /// <param name="valeur">the keyword typed by the user</param>
        /// <returns>the keyword with its wildcards neutralised</returns>
        private static string EchapperJokers(string valeur)
        {
            return valeur
                .Replace("\\", "\\\\")
                .Replace("%", "\\%")
                .Replace("_", "\\_");
        }

        /// <summary>
        /// Restricts every query to the entries of one visitor
        /// </summary>
        /// <remarks>
        /// Going through this single method means no query can accidentally
        /// omit the filter: the scoping is written once instead of being
        /// repeated in six places where one could be forgotten.
        /// </remarks>
        /// <param name="sessionId">the visitor's anonymous identifier</param>
        /// <returns>the query restricted to that visitor</returns>
        private IQueryable<ReponseGeneree> PourSession(string sessionId)
        {
            // A blank identifier matches nothing rather than everything.
            string session = string.IsNullOrWhiteSpace(sessionId)
                ? "\u0000-aucune-session"
                : sessionId;

            return this.context.Set<ReponseGeneree>()
                .Where(reponse => reponse.SessionId == session);
        }

        #endregion
    }
}
