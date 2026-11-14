using Microsoft.EntityFrameworkCore;
using RespondMetier.Models;

namespace RespondData
{
    /// <summary>
    /// Creates the database if needed and puts the demo profiles in it
    /// </summary>
    /// <remarks>
    /// The demo profiles exist so a visitor can click Generate and see a
    /// result without filling anything in first. Four contrasting ones rather
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
        /// that are missing
        /// </summary>
        /// <remarks>
        /// L'insertion porte sur les profils manquants, un par un, et non sur
        /// la seule condition que la table soit vide. Avec cette derniere, un
        /// profil de demonstration ajoute apres coup n'apparaissait jamais
        /// sur une base deja remplie : il fallait vider la production pour le
        /// voir, ce qui effacerait l'historique de tout le monde.
        ///
        /// Le rapprochement se fait sur le nom parmi les profils sans
        /// session. C'est fiable parce qu'un profil de demonstration est en
        /// lecture seule : personne ne peut le renommer ni le supprimer, donc
        /// un nom absent signifie bien qu'il n'a jamais ete insere.
        /// </remarks>
        public void Initialiser()
        {
            this.context.Database.EnsureCreated();

            List<string> dejaPresents = this.context.Set<Profil>()
                .Where(profil => profil.SessionId == string.Empty)
                .Select(profil => profil.Nom)
                .ToList();

            List<Profil> aInserer = ProfilsDeDemonstration()
                .Where(profil => !dejaPresents.Contains(profil.Nom))
                .ToList();

            if (aInserer.Count == 0)
            {
                return;
            }

            this.context.Set<Profil>().AddRange(aInserer);
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
                    courriel: "location@cles-de-bourgogne.fr"),

                // Un profil sans entreprise, volontairement : l'outil sert
                // aussi aux courriers qu'un etudiant redige mal a l'aise,
                // candidatures de stage et messages a l'administration. Le
                // ton y est tres different des trois autres, ce qui montre
                // mieux que le profil change reellement la reponse.
                Construire(
                    nom: "Etudiant",
                    service: "BUT Informatique, 2e annee",
                    entreprise: "IUT",
                    domaines: "Candidatures de stage, echanges avec les enseignants et "
                        + "l'administration, demandes de rendez-vous",
                    description: "Etudiant en deuxieme annee de BUT. Ecrit a des enseignants, des "
                        + "services administratifs et des entreprises pour des stages. "
                        + "Doit rester poli et direct, sans tournures ampoulees ni "
                        + "familiarite.",
                    ton: "poli, clair et respectueux",
                    signataire: "Theo Marchand",
                    telephone: "",
                    courriel: "theo.marchand@etu.exemple.fr")
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
