/**
 * Bascule entre les vues de l'application.
 *
 * Les vues sont toutes presentes dans la page et masquees par l'attribut
 * hidden, plutot que creees a la volee. L'application en compte cinq, et la
 * difference de poids est negligeable face a la complexite qu'un routeur
 * ajouterait.
 */
export class Navigation {
  /**
   * @param {object} actions fonctions a appeler quand une vue devient visible,
   *                         indexees par nom de vue
   */
  constructor(actions) {
    this.actions = actions;
    this.onglets = Array.from(document.querySelectorAll('.onglet'));
  }

  /**
   * Branche les onglets.
   */
  demarrer() {
    for (const onglet of this.onglets) {
      onglet.addEventListener('click', () => this.afficher(onglet.dataset.vue));
    }
  }

  /**
   * Affiche une vue et masque les autres.
   * @param {string} nom le nom de la vue, tel qu'ecrit dans data-vue
   */
  afficher(nom) {
    for (const onglet of this.onglets) {
      const actif = onglet.dataset.vue === nom;
      onglet.classList.toggle('actif', actif);
      onglet.setAttribute('aria-current', actif ? 'page' : 'false');
    }

    for (const vue of document.querySelectorAll('.vue')) {
      vue.hidden = vue.id !== `vue-${nom}`;
    }

    // La vue se rafraichit a l'affichage plutot qu'au demarrage : inutile de
    // charger l'historique de quelqu'un qui ne l'ouvrira jamais.
    const action = this.actions[nom];

    if (typeof action === 'function') {
      action();
    }
  }
}
