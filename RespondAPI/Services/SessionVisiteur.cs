using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;

namespace RespondAPI.Services
{
    /// <summary>
    /// Identifies a visitor across requests, without any account.
    /// </summary>
    /// <remarks>
    /// The application stores the emails people paste, so one visitor must
    /// never reach another's data. With no accounts, the only thing separating
    /// them is this identifier, which makes the cookie carrying it a security
    /// boundary rather than a convenience.
    /// <para>
    /// The cookie value is therefore signed with the ASP.NET data protection
    /// stack. A raw identifier would be forgeable: anyone could put someone
    /// else's value in their own cookie and read their history. A tampered or
    /// fabricated value fails to unprotect and is treated as a new visitor.
    /// </para>
    /// </remarks>
    public class SessionVisiteur : ISessionVisiteur
    {
        #region Constants

        /// <summary>Name of the cookie carrying the signed identifier</summary>
        public const string NomCookie = "respond_session";

        /// <summary>
        /// Number of random bytes in an identifier.
        /// </summary>
        /// <remarks>
        /// 32 bytes, drawn from the cryptographic generator rather than from
        /// Guid.NewGuid: a GUID carries only 122 random bits and is not
        /// specified to be unpredictable.
        /// </remarks>
        private const int OctetsIdentifiant = 32;

        /// <summary>How long a visitor keeps the same identifier</summary>
        private static readonly TimeSpan DureeCookie = TimeSpan.FromDays(365);

        #endregion

        #region Properties

        private readonly IDataProtector protecteur;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the SessionVisiteur class
        /// </summary>
        /// <param name="fournisseur">supplies the key used to sign the cookie</param>
        public SessionVisiteur(IDataProtectionProvider fournisseur)
        {
            // The purpose string binds the key to this use: a value signed for
            // another purpose cannot be replayed as a session identifier.
            this.protecteur = fournisseur.CreateProtector("Respond.SessionVisiteur.v1");
        }

        #endregion

        #region Methods

        public string ObtenirOuCreer(HttpContext contexte)
        {
            string? existant = this.LireCookie(contexte);

            if (existant != null)
            {
                return existant;
            }

            string identifiant = GenererIdentifiant();
            this.EcrireCookie(contexte, identifiant);
            return identifiant;
        }

        /// <summary>
        /// Reads and verifies the identifier carried by the request
        /// </summary>
        /// <param name="contexte">the current HTTP context</param>
        /// <returns>the identifier, or null when absent, forged or expired</returns>
        private string? LireCookie(HttpContext contexte)
        {
            if (!contexte.Request.Cookies.TryGetValue(NomCookie, out string? valeur)
                || string.IsNullOrWhiteSpace(valeur))
            {
                return null;
            }

            try
            {
                return this.protecteur.Unprotect(valeur);
            }
            catch (CryptographicException)
            {
                // Forged, tampered with, or signed by a key this server no
                // longer holds. Either way the visitor simply starts fresh;
                // failing loudly would tell an attacker their attempt was
                // detected, and failing open would hand them another session.
                return null;
            }
        }

        /// <summary>
        /// Writes the signed identifier in a cookie
        /// </summary>
        /// <param name="contexte">the current HTTP context</param>
        /// <param name="identifiant">the identifier to store</param>
        private void EcrireCookie(HttpContext contexte, string identifiant)
        {
            CookieOptions options = new CookieOptions();

            // Unreadable from JavaScript: a cross site scripting flaw could
            // otherwise be turned into the theft of every visitor's history.
            options.HttpOnly = true;

            // Sent over HTTPS only, except on a development machine where
            // there is no certificate and nothing real to protect.
            options.Secure = !contexte.Request.Host.Host.Equals(
                "localhost", StringComparison.OrdinalIgnoreCase);

            // Not sent on cross site requests, which blocks another site from
            // acting in the visitor's name.
            options.SameSite = SameSiteMode.Lax;

            options.IsEssential = true;
            options.Expires = DateTimeOffset.UtcNow.Add(DureeCookie);
            options.Path = "/";

            contexte.Response.Cookies.Append(
                NomCookie, this.protecteur.Protect(identifiant), options);
        }

        /// <summary>
        /// Draws a new unpredictable identifier
        /// </summary>
        /// <returns>the identifier, in URL safe base 64</returns>
        private static string GenererIdentifiant()
        {
            byte[] octets = RandomNumberGenerator.GetBytes(OctetsIdentifiant);
            return Convert.ToBase64String(octets)
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');
        }

        #endregion
    }
}
