using Microsoft.EntityFrameworkCore;
using RespondMetier.Data.Interfaces;
using RespondMetier.Exceptions;
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

        public List<Profil> Lister(string sessionId)
        {
            string session = Normaliser(sessionId);

            // An empty SessionId marks a demonstration profile, visible to all.
            return this.context.Set<Profil>()
                .Where(profil => profil.SessionId == string.Empty || profil.SessionId == session)
                .OrderBy(profil => profil.SessionId)
                .ThenBy(profil => profil.Nom)
                .ToList();
        }

        public Profil? ChargerParId(int id, string sessionId)
        {
            string session = Normaliser(sessionId);

            return this.context.Set<Profil>()
                .FirstOrDefault(profil => profil.Id == id
                    && (profil.SessionId == string.Empty || profil.SessionId == session));
        }

        public void Inserer(Profil profil)
        {
            this.context.Set<Profil>().Add(profil);
            this.context.SaveChanges();
        }

        public void MettreAJour(Profil profil, string sessionId)
        {
            Profil existant = this.ChargerPourEcriture(profil.Id, sessionId);

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

        public void Supprimer(int id, string sessionId)
        {
            Profil existant = this.ChargerPourEcriture(id, sessionId);

            this.context.Set<Profil>().Remove(existant);
            this.context.SaveChanges();
        }

        /// <summary>
        /// Loads a profile the visitor is allowed to modify
        /// </summary>
        /// <param name="id">the identifier of the profile</param>
        /// <param name="sessionId">the visitor's anonymous identifier</param>
        /// <returns>the profile, tracked by the context</returns>
        /// <exception cref="InvalidOperationException">the profile does not exist</exception>
        /// <exception cref="ErreurProfilEnLectureSeule">the profile is a demonstration one</exception>
        private Profil ChargerPourEcriture(int id, string sessionId)
        {
            Profil? existant = this.ChargerParId(id, sessionId);

            if (existant == null)
            {
                throw new InvalidOperationException(
                    "Ce profil n'existe pas.");
            }

            if (existant.EstProfilDeDemonstration)
            {
                throw new ErreurProfilEnLectureSeule(
                    "Les profils de démonstration ne peuvent pas être modifiés. "
                    + "Dupliquez-en un pour créer le vôtre.");
            }

            return existant;
        }

        /// <summary>
        /// Guards against a null or blank session being taken for a demonstration one
        /// </summary>
        /// <param name="sessionId">the identifier received</param>
        /// <returns>the identifier, or a value that matches nothing</returns>
        private static string Normaliser(string sessionId)
        {
            // A blank identifier must never equal the empty SessionId of the
            // demonstration profiles, or a caller that forgot to pass one
            // would silently gain write access to them.
            return string.IsNullOrWhiteSpace(sessionId) ? "\u0000-aucune-session" : sessionId;
        }

        #endregion
    }
}
