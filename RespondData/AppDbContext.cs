using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RespondMetier.Models;

namespace RespondData
{
    /// <summary>
    /// Represents the connection and configuration of the database
    /// </summary>
    /// <remarks>
    /// Le contexte porte aussi les cles qui signent les cookies de session.
    /// Elles etaient auparavant ecrites dans un dossier, ce qui imposait un
    /// volume persistant et interdisait plusieurs instances : chacune aurait
    /// eu ses propres cles, et un visiteur aurait change d'identite a chaque
    /// requete. En base, les cles survivent au redeploiement et sont partagees
    /// par toutes les instances, sans rien a monter.
    /// </remarks>
    public class AppDbContext : DbContext, IDataProtectionKeyContext
    {
        #region DbSets

        /// <summary>
        /// Represents the company profiles table
        /// </summary>
        public DbSet<Profil> Profils
        {
            get { return Set<Profil>(); }
        }

        /// <summary>
        /// Represents the generated replies history table
        /// </summary>
        public DbSet<ReponseGeneree> Historique
        {
            get { return Set<ReponseGeneree>(); }
        }

        /// <summary>
        /// Represents the keys signing the session cookies
        /// </summary>
        /// <remarks>
        /// Ces lignes sont un secret : qui les lit peut forger un cookie de
        /// session et se faire passer pour n'importe quel visiteur. Elles ne
        /// sont exposees par aucune route.
        /// </remarks>
        public DbSet<DataProtectionKey> DataProtectionKeys
        {
            get { return Set<DataProtectionKey>(); }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes the database context
        /// </summary>
        /// <param name="options">The context configuration options</param>
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        #endregion

        #region Configuration

        /// <summary>
        /// Configures the database entities
        /// </summary>
        /// <param name="modelBuilder">The entity configuration tool</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configures company profiles
            modelBuilder.Entity<Profil>(entity =>
            {
                entity.ToTable("Profil");
                entity.HasKey(profil => profil.Id);
                entity.Property(profil => profil.Id).ValueGeneratedOnAdd();
                entity.Property(profil => profil.Nom).IsRequired();
                entity.Property(profil => profil.SessionId).IsRequired();

                // Le nom est unique par visiteur, pas globalement : deux
                // visiteurs ont le droit d'appeler leur profil « SAV ».
                entity.HasIndex(profil => new { profil.SessionId, profil.Nom }).IsUnique();
                entity.Property(profil => profil.Service).IsRequired();
                entity.Property(profil => profil.Entreprise).IsRequired();
                entity.Property(profil => profil.Langue).IsRequired();

                // The signature has no identity of its own: it only exists as
                // part of a profile, and is deleted with it. Mapping it as an
                // owned type keeps its columns in the Profil table instead of
                // creating a second table joined on a meaningless key.
                entity.OwnsOne(profil => profil.Signature, signature =>
                {
                    signature.Property(valeur => valeur.Nom).HasColumnName("SignatureNom");
                    signature.Property(valeur => valeur.Telephone).HasColumnName("SignatureTelephone");
                    signature.Property(valeur => valeur.Email).HasColumnName("SignatureEmail");
                    signature.Property(valeur => valeur.InclureNom).HasColumnName("SignatureInclureNom");
                    signature.Property(valeur => valeur.InclureService).HasColumnName("SignatureInclureService");
                    signature.Property(valeur => valeur.InclureEntreprise).HasColumnName("SignatureInclureEntreprise");
                    signature.Property(valeur => valeur.InclureTelephone).HasColumnName("SignatureInclureTelephone");
                    signature.Property(valeur => valeur.InclureEmail).HasColumnName("SignatureInclureEmail");
                });
            });

            // Configures the history of generated replies
            modelBuilder.Entity<ReponseGeneree>(entity =>
            {
                entity.ToTable("ReponseGeneree");
                entity.HasKey(reponse => reponse.Id);
                entity.Property(reponse => reponse.Id).ValueGeneratedOnAdd();
                entity.Property(reponse => reponse.Date).IsRequired();
                entity.Property(reponse => reponse.Texte).IsRequired();
                entity.Property(reponse => reponse.SessionId).IsRequired();

                // Chaque lecture filtre sur la session puis trie par date :
                // l'index composite couvre les deux d'un coup.
                entity.HasIndex(reponse => new { reponse.SessionId, reponse.Date });
            });
        }

        #endregion
    }
}
