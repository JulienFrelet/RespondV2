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

        public List<ReponseGeneree> Lister()
        {
            return this.context.Set<ReponseGeneree>()
                .OrderByDescending(reponse => reponse.Date)
                .ToList();
        }

        public List<ReponseGeneree> ListerPremiers(int nbMax)
        {
            if (nbMax <= 0)
            {
                return new List<ReponseGeneree>();
            }

            return this.context.Set<ReponseGeneree>()
                .OrderByDescending(reponse => reponse.Date)
                .Take(nbMax)
                .ToList();
        }

        public List<ReponseGeneree> Rechercher(string motCle)
        {
            if (string.IsNullOrWhiteSpace(motCle))
            {
                return this.Lister();
            }

            string recherche = motCle.Trim();

            // EF.Functions.Like maps to the SQL LIKE operator, which SQLite
            // applies case insensitively on ASCII. Doing the filtering in SQL
            // rather than in memory matters once the history grows.
            string motif = "%" + recherche + "%";

            return this.context.Set<ReponseGeneree>()
                .Where(reponse => EF.Functions.Like(reponse.Objet, motif)
                               || EF.Functions.Like(reponse.Texte, motif)
                               || EF.Functions.Like(reponse.NomProfil, motif)
                               || EF.Functions.Like(reponse.NomCleApi, motif))
                .OrderByDescending(reponse => reponse.Date)
                .ToList();
        }

        public void Inserer(ReponseGeneree reponse)
        {
            this.context.Set<ReponseGeneree>().Add(reponse);
            this.context.SaveChanges();
        }

        public void Supprimer(int id)
        {
            ReponseGeneree? existante = this.context.Set<ReponseGeneree>()
                .FirstOrDefault(reponse => reponse.Id == id);

            if (existante == null)
            {
                throw new InvalidOperationException(
                    "Impossible de supprimer une entree d'historique qui n'existe pas.");
            }

            this.context.Set<ReponseGeneree>().Remove(existante);
            this.context.SaveChanges();
        }

        public void LimiterA(int nbMax)
        {
            if (nbMax < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(nbMax), "La taille maximale de l'historique ne peut pas etre negative.");
            }

            // Only the entries beyond the limit are loaded, not the whole
            // history: Skip is translated to SQL, so a history of ten thousand
            // entries trimmed to twenty reads twenty rows, not ten thousand.
            List<ReponseGeneree> aSupprimer = this.context.Set<ReponseGeneree>()
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

        #endregion
    }
}

