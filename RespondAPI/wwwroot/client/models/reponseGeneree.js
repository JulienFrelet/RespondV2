/**
 * Une reponse generee, telle que l'API la renvoie.
 */
export class ReponseGeneree {
  /**
   * @param {object} donnees objet JSON renvoye par l'API
   */
  constructor(donnees) {
    this.id = donnees.id;
    this.date = new Date(donnees.date);
    this.nomProfil = donnees.nomProfil ?? '';
    this.nomCleApi = donnees.nomCleApi ?? '';
    this.objet = donnees.objet ?? '';
    this.texte = donnees.texte ?? '';
  }

  /**
   * Date lisible, dans le fuseau et le format du visiteur.
   * @returns {string} la date formatee
   */
  dateLisible() {
    return this.date.toLocaleString('fr-FR', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  }

  /**
   * Objet affichable, meme quand le modele n'en a pas produit.
   * @returns {string} l'objet ou une mention de remplacement
   */
  objetLisible() {
    return this.objet.trim().length > 0 ? this.objet : 'Sans objet';
  }

  /**
   * Debut du corps, pour l'apercu dans la liste.
   * @param {number} taille nombre de caracteres voulus
   * @returns {string} l'apercu
   */
  apercu(taille = 110) {
    const propre = this.texte.replace(/\s+/g, ' ').trim();

    return propre.length > taille ? `${propre.slice(0, taille)}…` : propre;
  }
}
