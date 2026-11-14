using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RespondMetier.Services.Interfaces;

namespace RespondAPI.Filtres
{
    /// <summary>
    /// Refuses a write once the client address has reached its quota
    /// </summary>
    /// <remarks>
    /// Un filtre plutot que la limitation fournie par le serveur, qui garde
    /// ses compteurs dans le processus : l'hebergement gratuit redemarre le
    /// service des qu'il a dormi quelques minutes, et les compteurs
    /// repartaient alors a zero.
    /// </remarks>
    public class LimiteEcritureAttribute : TypeFilterAttribute
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the LimiteEcritureAttribute class
        /// </summary>
        public LimiteEcritureAttribute() : base(typeof(FiltreLimiteEcriture))
        {
        }

        #endregion
    }

    /// <summary>
    /// Runs the write quota check before the action
    /// </summary>
    public class FiltreLimiteEcriture : IActionFilter
    {
        #region Constantes

        /// <summary>
        /// The status code returned once the quota is reached
        /// </summary>
        public const int CodeTropDeRequetes = 429;

        #endregion

        #region Attributs

        private readonly ILimiteurEcritures limiteur;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the FiltreLimiteEcriture class
        /// </summary>
        /// <param name="limiteur">the service counting the writes</param>
        public FiltreLimiteEcriture(ILimiteurEcritures limiteur)
        {
            this.limiteur = limiteur;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Checks the quota, and stops the request when it is reached
        /// </summary>
        /// <param name="contexte">the action being executed</param>
        public void OnActionExecuting(ActionExecutingContext contexte)
        {
            // L'adresse est lue apres le traitement des en-tetes du proxy :
            // sans cela, toutes les requetes partageraient l'adresse du proxy
            // et un seul visiteur bloquerait tous les autres.
            string adresse = contexte.HttpContext.Connection.RemoteIpAddress?.ToString()
                ?? string.Empty;

            if (this.limiteur.Autoriser(adresse))
            {
                return;
            }

            contexte.Result = new ObjectResult(new
            {
                erreur = "Trop d'enregistrements depuis cette adresse. Réessayez dans une heure.",
            })
            {
                StatusCode = CodeTropDeRequetes,
            };
        }

        /// <summary>
        /// Does nothing once the action has run
        /// </summary>
        /// <param name="contexte">the action that has executed</param>
        public void OnActionExecuted(ActionExecutedContext contexte)
        {
        }

        #endregion
    }
}
