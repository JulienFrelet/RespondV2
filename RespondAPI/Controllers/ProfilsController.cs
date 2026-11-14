using Microsoft.AspNetCore.Mvc;
using RespondAPI.Services;
using RespondMetier.Data.Interfaces;
using RespondMetier.Models;

namespace RespondAPI.Controllers
{
    /// <summary>
    /// Exposes the company profiles a reply can be written from
    /// </summary>
    [ApiController]
    [Route("api/profils")]
    public class ProfilsController : ControllerBase
    {
        #region Properties

        private readonly IProfilDAO profils;
        private readonly ISessionVisiteur session;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the ProfilsController class
        /// </summary>
        /// <param name="profils">the profile data access</param>
        /// <param name="session">supplies the visitor's anonymous identifier</param>
        public ProfilsController(IProfilDAO profils, ISessionVisiteur session)
        {
            this.profils = profils;
            this.session = session;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Lists every profile
        /// </summary>
        /// <returns>the profiles, sorted by name</returns>
        [HttpGet]
        public ActionResult<List<Profil>> Lister()
        {
            return this.Ok(this.profils.Lister(this.session.ObtenirOuCreer(this.HttpContext)));
        }

        /// <summary>
        /// Returns a single profile
        /// </summary>
        /// <param name="id">the identifier of the profile wanted</param>
        /// <returns>the profile, or 404 when it does not exist</returns>
        [HttpGet("{id:int}")]
        public ActionResult<Profil> Charger(int id)
        {
            Profil? profil = this.profils.ChargerParId(
                id, this.session.ObtenirOuCreer(this.HttpContext));

            return profil == null ? this.NotFound() : this.Ok(profil);
        }

        #endregion
    }
}
