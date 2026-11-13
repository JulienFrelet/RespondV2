using Microsoft.EntityFrameworkCore;
using RespondMetier.Data.Interfaces;
using RespondMetier.Models;

namespace RespondData.DAO
{
    /// <summary>
    /// Provides data access operations for company profiles
    /// </summary>
    public class ProfilDAO : IProfilDAO
    {
        #region Properties

        private readonly AppDbContext context;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the ProfilDAO class
        /// </summary>
        /// <param name="context">The database context</param>
        public ProfilDAO(AppDbContext context)
        {
            this.context = context;
        }

        #endregion

        #region Methods

        public List<Profil> Lister()
        {
            return this.context.Set<Profil>()
                .OrderBy(profil => profil.Nom)
                .ToList();
        }

        public Profil? ChargerParId(int id)
        {
            return this.context.Set<Profil>()
                .FirstOrDefault(profil => profil.Id == id);
        }

        public void Inserer(Profil profil)
        {
            this.context.Set<Profil>().Add(profil);
            this.context.SaveChanges();
        }

        public void MettreAJour(Profil profil)
        {
            Profil? existant = this.context.Set<Profil>()
                .FirstOrDefault(element => element.Id == profil.Id);

            if (existant == null)
            {
                throw new InvalidOperationException(
                    "Impossible de modifier un profil qui n'existe pas.");
            }

            // The tracked instance is updated field by field rather than
            // replaced. Attaching the incoming object would make EF treat the
            // owned signature as a different one and fail on the key.
            existant.Nom = profil.Nom;
            existant.Service = profil.Service;
            existant.Entreprise = profil.Entreprise;
            existant.Domaines = profil.Domaines;
            existant.Description = profil.Description;
            existant.Ton = profil.Ton;
            existant.Langue = profil.Langue;
            existant.Signature = profil.Signature;

            this.context.SaveChanges();
        }

        public void Supprimer(int id)
        {
            Profil? existant = this.context.Set<Profil>()
                .FirstOrDefault(profil => profil.Id == id);

            if (existant == null)
            {
                throw new InvalidOperationException(
                    "Impossible de supprimer un profil qui n'existe pas.");
            }

            this.context.Set<Profil>().Remove(existant);
            this.context.SaveChanges();
        }

        #endregion
    }
}
