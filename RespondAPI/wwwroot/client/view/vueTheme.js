/**
 * Bascule entre le mode clair et le mode sombre.
 *
 * Tant que l'utilisateur n'a rien choisi, c'est la preference du systeme qui
 * s'applique. Des qu'il clique, son choix est memorise et prend le dessus :
 * on ne force jamais un theme a quelqu'un qui n'a rien demande, et on
 * n'ignore jamais quelqu'un qui a demande.
 */
export class VueTheme {
  static CLE_STOCKAGE = 'respond-theme';

  constructor() {
    this.bouton = document.getElementById('btn-theme');
    this.libelle = document.getElementById('btn-theme-libelle');
  }

  /**
   * Branche le bouton et met son libelle a jour.
   */
  demarrer() {
    if (this.bouton === null) {
      return;
    }

    this.bouton.addEventListener('click', () => this.#basculer());
    this.#majLibelle();

    // Si l'utilisateur n'a rien choisi, suivre le systeme quand il change
    // de theme en cours de route.
    window.matchMedia('(prefers-color-scheme: dark)')
      .addEventListener('change', () => {
        if (this.#choixMemorise() === null) {
          this.#majLibelle();
        }
      });
  }

  /**
   * Passe d'un theme a l'autre et memorise le choix.
   */
  #basculer() {
    const sombreMaintenant = this.#sombreActif();
    const nouveau = sombreMaintenant ? 'clair' : 'sombre';

    document.documentElement.setAttribute(
      'data-theme', nouveau === 'clair' ? 'light' : 'dark');

    try {
      localStorage.setItem(VueTheme.CLE_STOCKAGE, nouveau);
    } catch {
      // Stockage refuse : le theme s'applique quand meme pour cette visite.
    }

    this.#majLibelle();
  }

  /**
   * Indique si le mode sombre est actuellement affiche.
   * @returns {boolean} vrai si la page est en sombre
   */
  #sombreActif() {
    const force = document.documentElement.getAttribute('data-theme');

    if (force === 'dark') {
      return true;
    }

    if (force === 'light') {
      return false;
    }

    return window.matchMedia('(prefers-color-scheme: dark)').matches;
  }

  /**
   * Lit le choix memorise.
   * @returns {string|null} clair, sombre, ou null si rien n'a ete choisi
   */
  #choixMemorise() {
    try {
      return localStorage.getItem(VueTheme.CLE_STOCKAGE);
    } catch {
      return null;
    }
  }

  /**
   * Le libelle annonce ce vers quoi le clic emmene, pas l'etat courant :
   * un bouton decrit son action.
   */
  #majLibelle() {
    const sombre = this.#sombreActif();

    if (this.libelle !== null) {
      this.libelle.textContent = sombre ? 'Mode clair' : 'Mode sombre';
    }

    this.bouton.setAttribute(
      'aria-label', sombre ? 'Passer en mode clair' : 'Passer en mode sombre');

    this.bouton.setAttribute('aria-pressed', String(sombre));
  }
}
