import {
  lireCleApi, ecrireCleApi, effacerCleApi, toutEffacer,
} from '../service/stockageLocal.js';

/**
 * Vue des parametres : clé personnelle et effacement des reglages locaux.
 */
export class VueParametres {
  /**
   * @param {Function} surChangementCle appele quand la cle change, pour que la
   *                                    vue Generer mette a jour son bandeau
   */
  constructor(surChangementCle) {
    this.surChangementCle = surChangementCle;

    this.champCle = document.getElementById('pm-cle');
    this.btnEnregistrer = document.getElementById('btn-enregistrer-cle');
    this.btnOublier = document.getElementById('btn-oublier-cle');
    this.confirmationCle = document.getElementById('confirmation-cle');

    this.btnEffacer = document.getElementById('btn-effacer-local');
    this.confirmationDonnees = document.getElementById('confirmation-donnees');
  }

  /**
   * Branche les evenements.
   */
  demarrer() {
    this.btnEnregistrer.addEventListener('click', () => this.#enregistrerCle());
    this.btnOublier.addEventListener('click', () => this.#oublierCle());
    this.btnEffacer.addEventListener('click', () => this.#effacerLocal());

    this.rafraichir();
  }

  /**
   * Met l'ecran a jour sans jamais reafficher la cle.
   */
  rafraichir() {
    const cle = lireCleApi();

    // La cle enregistree n'est jamais reaffichee, meme masquee : un champ
    // mot de passe rempli se lit en une ligne dans la console du navigateur.
    // Seule sa presence est indiquee.
    this.champCle.value = '';
    this.champCle.placeholder = cle.length > 0
      ? 'Une clé est enregistrée pour cette session'
      : 'gsk_…';
  }

  /**
   * Enregistre la cle saisie.
   */
  #enregistrerCle() {
    const cle = this.champCle.value.trim();

    if (cle.length === 0) {
      this.#dire(this.confirmationCle, 'Saisissez une clé avant d\'enregistrer.');
      return;
    }

    // Controle de forme identique a celui du serveur : caracteres ASCII
    // visibles uniquement. Un retour a la ligne colle par accident serait
    // refuse par le serveur, autant le dire tout de suite.
    if (!/^[\x21-\x7E]{1,200}$/.test(cle)) {
      this.#dire(
        this.confirmationCle,
        'Cette clé contient des caractères interdits ou est trop longue.');
      return;
    }

    const ok = ecrireCleApi(cle);
    this.champCle.value = '';
    this.rafraichir();
    this.surChangementCle();

    this.#dire(
      this.confirmationCle,
      ok
        ? '✓ Clé enregistrée pour cet onglet. Elle disparaîtra à sa fermeture.'
        : 'Votre navigateur refuse le stockage : la clé ne sera pas conservée.');
  }

  /**
   * Oublie la cle enregistree.
   */
  #oublierCle() {
    effacerCleApi();
    this.rafraichir();
    this.surChangementCle();
    this.#dire(this.confirmationCle, '✓ Clé oubliée. Le mode démonstration reprend.');
  }

  /**
   * Efface les reglages locaux.
   */
  #effacerLocal() {
    if (!window.confirm('Effacer vos règles et votre clé de ce navigateur ?')) {
      return;
    }

    toutEffacer();
    this.rafraichir();
    this.surChangementCle();

    this.#dire(
      this.confirmationDonnees,
      '✓ Réglages effacés. Vos profils et votre historique, eux, restent sur le serveur.');
  }

  /**
   * Affiche un message temporaire.
   * @param {HTMLElement} cible l'element qui porte le message
   * @param {string} message le texte a afficher
   */
  #dire(cible, message) {
    cible.textContent = message;

    setTimeout(() => {
      cible.textContent = '';
    }, 4000);
  }
}
