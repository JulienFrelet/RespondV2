using RespondMetier.Data.Interfaces;
using RespondMetier.Models;
using RespondMetier.Services.Interfaces;

namespace RespondMetier.Services.Realisations
{
    /// <summary>
    /// Limits how many writes one client address may make per time window,
    /// counting in the database rather than in memory
    /// </summary>
    /// <remarks>
    /// Le nombre de profils est deja plafonne par visiteur, mais un visiteur
    /// n'est qu'un cookie : il suffit de ne pas le renvoyer pour repartir a
    /// zero. Le plafond par session ne borne donc rien face a un script, et la
    /// base, sur une offre gratuite limitee en taille, se remplirait jusqu'a
    /// refuser d'ecrire.
    ///
    /// Le compteur est range en base et non en memoire, parce que
    /// l'hebergement gratuit redemarre le processus des que le service a dormi
    /// quelques minutes : des compteurs en memoire repartiraient alors a zero.
    /// </remarks>
    public class LimiteurEcritures : ILimiteurEcritures
    {
        #region Attributs

        private readonly ICompteurEcrituresDAO compteurs;
        private readonly int limite;
        private readonly TimeSpan fenetre;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the LimiteurEcritures class
        /// </summary>
        /// <param name="compteurs">the counter storage</param>
        /// <param name="limite">how many writes a window allows</param>
        /// <param name="fenetre">how long a window lasts</param>
        public LimiteurEcritures(ICompteurEcrituresDAO compteurs, int limite, TimeSpan fenetre)
        {
            this.compteurs = compteurs;
            this.limite = limite;
            this.fenetre = fenetre;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Counts one write and tells whether it is allowed
        /// </summary>
        /// <param name="cle">the client address the write comes from</param>
        /// <returns>true when the write may proceed, false when the limit is reached</returns>
        public bool Autoriser(string cle)
        {
            // Une cle vide viendrait d'une adresse que le serveur n'a pas su
            // lire. Toutes ces requetes partagent alors le meme compteur, ce
            // qui est volontairement severe : mieux vaut brider un cas rare
            // que d'ouvrir une voie ou il suffirait de masquer son adresse.
            string cleReelle = string.IsNullOrWhiteSpace(cle) ? "adresse-inconnue" : cle;

            DateTime maintenant = DateTime.UtcNow;
            CompteurEcritures? compteur = this.compteurs.Charger(cleReelle);

            if (compteur == null)
            {
                compteur = new CompteurEcritures();
                compteur.Cle = cleReelle;
                compteur.DebutFenetre = maintenant;
                compteur.Nombre = 0;

                // Le menage se fait a la creation d'un compteur, pas a chaque
                // ecriture : une requete de suppression par ecriture couterait
                // plus cher que ce qu'elle economise.
                this.compteurs.Purger(maintenant - this.fenetre);
            }
            else if (compteur.FenetreExpiree(maintenant, this.fenetre))
            {
                compteur.DebutFenetre = maintenant;
                compteur.Nombre = 0;
            }

            if (compteur.Nombre >= this.limite)
            {
                return false;
            }

            compteur.Nombre = compteur.Nombre + 1;
            this.compteurs.Enregistrer(compteur);
            return true;
        }

        #endregion
    }
}
