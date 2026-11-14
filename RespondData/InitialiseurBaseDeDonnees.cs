using Microsoft.EntityFrameworkCore;
using RespondMetier.Models;

namespace RespondData
{
    /// <summary>
    /// Creates the database if needed and puts the demo profiles in it
    /// </summary>
    /// <remarks>
    /// The demo profiles exist so a visitor can click Generate and see a
    /// result without filling anything in first. Three contrasting ones rather
    /// than a single generic one, because the point is to show that the tone
    /// and the signature really change with the profile.
    /// </remarks>
    public class InitialiseurBaseDeDonnees
    {
        #region Properties

        private readonly AppDbContext context;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the InitialiseurBaseDeDonnees class
        /// </summary>
        /// <param name="context">The database context</param>
        public InitialiseurBaseDeDonnees(AppDbContext context)
        {
            this.context = context;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates the schema when absent, then inserts the demo profiles
        /// when the table is empty
        /// </summary>
        public void Initialiser()
        {
            this.context.Database.EnsureCreated();

            // Only seeded on an empty table. Re-inserting on every start would
            // duplicate the profiles, and overwriting them would erase what a
            // visitor changed.
            if (this.context.Set<Profil>().Any())
            {
                return;
            }

            this.context.Set<Profil>().AddRange(ProfilsDeDemonstration());
            this.context.SaveChanges();
        }

        /// <summary>
        /// Builds the profiles inserted on a fresh database
        /// </summary>
        /// <returns>the demo profiles</returns>
        private static List<Profil> ProfilsDeDemonstration()
        {
            return
            [
                Construire(
                    nom: "SAV electromenager",
                    service: "Service apres-vente",
                    entreprise: "Electromenager Plus",
                    domaines: "Vente, livraison et reparation d'appareils menagers",
                    description: "Magasin independant, 12 salaries, livraison et pose a domicile "
                        + "dans un rayon de 50 km. Atelier de reparation sur place.",
                    ton: "professionnel et chaleureux",
                    signataire: "Camille Mercier",
                    telephone: "03 80 12 34 56",
                    courriel: "sav@electromenager-plus.fr"),

                Construire(
                    nom: "Cabinet comptable",
                    service: "Assistanat comptable",
                    entreprise: "Cabinet Durand & Associes",
                    domaines: "Comptabilite, paie et conseil fiscal pour TPE et PME",
                    description: "Cabinet de six collaborateurs. Les echanges portent souvent "
                        + "sur des pieces manquantes et des echeances declaratives.",
                    ton: "formel et professionnel",
                    signataire: "Pierre Durand",
                    telephone: "03 80 98 76 54",
                    courriel: "contact@durand-associes.fr"),

                Construire(
                    nom: "Agence immobiliere",
                    service: "Conseil en location",
                    entreprise: "Cles de Bourgogne",
                    domaines: "Location et gestion locative sur Dijon et son agglomeration",
                    description: "Agence de quartier specialisee dans la location etudiante. "
                        + "Beaucoup de demandes de visites et de questions sur les dossiers.",
                    ton: "decontracte et sympa",
                    signataire: "Lea Bonnet",
                    telephone: "03 80 55 44 33",
                    courriel: "location@cles-de-bourgogne.fr")
            ];
        }

        /// <summary>
        /// Assembles one demo profile with its signature
        /// </summary>
        private static Profil Construire(
            string nom,
            string service,
            string entreprise,
            string domaines,
            string description,
            string ton,
            string signataire,
            string telephone,
            string courriel)
        {
            Profil profil = new Profil();
            profil.Nom = nom;
            profil.Service = service;
            profil.Entreprise = entreprise;
            profil.Domaines = domaines;
            profil.Description = description;
            profil.Ton = ton;
            profil.Langue = "français";
            profil.Signature.Nom = signataire;
            profil.Signature.Telephone = telephone;
            profil.Signature.Email = courriel;
            return profil;
        }

        #endregion
    }
}
