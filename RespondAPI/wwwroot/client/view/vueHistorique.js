/**
 * Vue de l'historique : liste, recherche, detail, suppression.
 *
 * L'historique vient du serveur et ne contient que les reponses de ce
 * visiteur, filtrees sur son cookie de session. Le client n'a rien a filtrer.
 */
export class VueHistorique {
  constructor(service) {
    this.service = service;
    this.entrees = [];
    this.idSelectionne = null;
    this.minuterieRecherche = null;

    this.champRecherche = document.getElementById('recherche-historique');
    this.erreur = document.getElementById('err-historique');
    this.vide = document.getElementById('historique-vide');
    this.split = document.getElementById('historique-split');
    this.liste = document.getElementById('liste-historique');

    this.detail = document.getElementById('detail-historique');
    this.detailObjet = document.getElementById('detail-objet');
    this.detailMeta = document.getElementById('detail-meta');
    this.detailCorps = document.getElementById('detail-corps');
    this.btnCopier = document.getElementById('btn-detail-copier');
    this.btnSupprimer = document.getElementById('btn-detail-supprimer');
  }

  /**
   * Branche les evenements.
   */
  demarrer() {
    // La frappe est temporisee : sans cela chaque caractere declencherait une
    // requete, soit une dizaine d'appels pour un mot.
    this.champRecherche.addEventListener('input', () => {
      clearTimeout(this.minuterieRecherche);
      this.minuterieRecherche = setTimeout(() => this.rafraichir(), 250);
    });

    this.btnCopier.addEventListener('click', () => this.#copier());
    this.btnSupprimer.addEventListener('click', () => this.#supprimer());
  }

  /**
   * Recharge l'historique.
   * @returns {Promise<void>}
   */
  async rafraichir() {
    this.erreur.textContent = '';

    try {
      this.entrees = await this.service.lister(this.champRecherche.value);
      this.#afficher();
    } catch (erreur) {
      this.erreur.textContent = erreur.message;
    }
  }

  /**
   * Affiche la liste et, le cas echeant, le detail selectionne.
   */
  #afficher() {
    const aucune = this.entrees.length === 0;

    this.vide.hidden = !aucune;
    this.split.hidden = aucune;

    this.vide.textContent = this.champRecherche.value.trim().length > 0
      ? 'Aucune réponse ne correspond à cette recherche.'
      : "Aucune réponse pour l'instant.";

    // La liste est videe meme quand il n'y a rien a afficher : sans cela les
    // lignes de la recherche precedente restent dans le DOM, masquees, et
    // reapparaissent une fraction de seconde au retour des resultats.
    this.liste.replaceChildren();

    if (aucune) {
      this.idSelectionne = null;
      this.detail.hidden = true;
      return;
    }

    for (const entree of this.entrees) {
      this.liste.appendChild(this.#ligne(entree));
    }

    // La selection precedente peut avoir disparu d'une recherche a l'autre.
    const encorePresente = this.entrees.some((e) => e.id === this.idSelectionne);

    if (!encorePresente) {
      this.idSelectionne = null;
      this.detail.hidden = true;
    }
  }

  /**
   * Construit une ligne de la liste.
   * @param {object} entree l'entree a afficher
   * @returns {HTMLElement} la ligne
   */
  #ligne(entree) {
    const ligne = document.createElement('button');
    ligne.type = 'button';
    ligne.className = 'ligne-historique';
    ligne.classList.toggle('selectionnee', entree.id === this.idSelectionne);

    const objet = document.createElement('span');
    objet.className = 'ligne-objet';
    objet.textContent = entree.objetLisible();

    const meta = document.createElement('span');
    meta.className = 'ligne-meta';
    meta.textContent = `${entree.nomProfil} · ${entree.dateLisible()}`;

    const apercu = document.createElement('span');
    apercu.className = 'ligne-apercu';
    apercu.textContent = entree.apercu();

    ligne.append(objet, meta, apercu);
    ligne.addEventListener('click', () => this.#selectionner(entree));
    return ligne;
  }

  /**
   * Affiche le detail d'une entree.
   * @param {object} entree l'entree choisie
   */
  #selectionner(entree) {
    this.idSelectionne = entree.id;

    this.detailObjet.textContent = entree.objetLisible();
    this.detailMeta.textContent =
      `${entree.nomProfil} · ${entree.nomCleApi} · ${entree.dateLisible()}`;
    this.detailCorps.value = entree.texte;
    this.detail.hidden = false;

    for (const ligne of this.liste.children) {
      ligne.classList.remove('selectionnee');
    }

    this.#afficher();
  }

  /**
   * Copie le corps affiche.
   * @returns {Promise<void>}
   */
  async #copier() {
    try {
      await navigator.clipboard.writeText(this.detailCorps.value);
      this.btnCopier.textContent = 'Copié';
      this.btnCopier.classList.add('confirme');

      setTimeout(() => {
        this.btnCopier.textContent = 'Copier';
        this.btnCopier.classList.remove('confirme');
      }, 1600);
    } catch {
      this.erreur.textContent =
        'Copie refusée par le navigateur. Sélectionnez le texte à la main.';
    }
  }

  /**
   * Supprime l'entree selectionnee, apres confirmation.
   * @returns {Promise<void>}
   */
  async #supprimer() {
    if (this.idSelectionne === null) {
      return;
    }

    if (!window.confirm('Supprimer définitivement cette réponse ?')) {
      return;
    }

    try {
      await this.service.supprimer(this.idSelectionne);
      this.idSelectionne = null;
      this.detail.hidden = true;
      await this.rafraichir();
    } catch (erreur) {
      this.erreur.textContent = erreur.message;
    }
  }
}
