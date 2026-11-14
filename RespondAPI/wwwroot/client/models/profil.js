/**
 * Un profil d'entreprise, tel que l'API le renvoie.
 *
 * Le modele existe pour donner un nom aux champs cote client plutot que de
 * promener des objets JSON anonymes dans les vues. Si l'API renomme un champ,
 * seul ce fichier change.
 */
export class Profil {
  /**
   * @param {object} donnees objet JSON renvoye par l'API
   */
  constructor(donnees) {
    this.id = donnees.id;
    this.nom = donnees.nom ?? '';
    this.service = donnees.service ?? '';
    this.entreprise = donnees.entreprise ?? '';
    this.domaines = donnees.domaines ?? '';
    this.ton = donnees.ton ?? '';
  }

  /**
   * Resume affiche sous le selecteur, pour que l'utilisateur voie d'un coup
   * d'oeil le contexte dans lequel la reponse sera redigee.
   * @returns {string} le resume du profil
   */
  resume() {
    return [this.service, this.entreprise, this.ton]
      .filter((partie) => partie.length > 0)
      .join(' · ');
  }
}
