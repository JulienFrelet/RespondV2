import { lireRegles, ecrireRegles } from '../service/stockageLocal.js';

/**
 * Vue des regles de redaction.
 *
 * Ces regles ne quittent le navigateur qu'au moment d'une generation, ou elles
 * accompagnent la demande. Les conserver cote client evite de rattacher des
 * preferences a une session anonyme sur le serveur, pour un gain nul.
 */
export class VueStyle {
  constructor() {
    this.caseOuverture = document.getElementById('rg-ouverture');
    this.selOuverture = document.getElementById('rg-formule-ouverture');
    this.libreOuverture = document.getElementById('rg-ouverture-libre');

    this.caseCloture = document.getElementById('rg-cloture');
    this.selCloture = document.getElementById('rg-formule-cloture');
    this.libreCloture = document.getElementById('rg-cloture-libre');

    this.personnalisees = document.getElementById('rg-personnalisees');
    this.bouton = document.getElementById('btn-enregistrer-style');
    this.confirmation = document.getElementById('confirmation-style');
  }

  /**
   * Branche les evenements et remplit le formulaire.
   */
  demarrer() {
    this.selOuverture.addEventListener('change', () => this.#majChampsLibres());
    this.selCloture.addEventListener('change', () => this.#majChampsLibres());
    this.bouton.addEventListener('click', () => this.#enregistrer());

    this.remplir();
  }

  /**
   * Remplit le formulaire avec les regles memorisees.
   */
  remplir() {
    const regles = lireRegles();

    this.caseOuverture.checked = regles.utiliserFormuleOuverture;
    this.caseCloture.checked = regles.utiliserFormuleCloture;
    this.personnalisees.value = regles.reglesPersonnalisees;

    this.#choisir(this.selOuverture, this.libreOuverture, regles.formuleOuverture);
    this.#choisir(this.selCloture, this.libreCloture, regles.formuleCloture);
    this.#majChampsLibres();
  }

  /**
   * Selectionne une formule dans une liste, ou bascule sur le champ libre
   * quand la valeur memorisee n'y figure pas.
   * @param {HTMLSelectElement} liste la liste deroulante
   * @param {HTMLInputElement} champLibre le champ de saisie libre
   * @param {string} valeur la formule memorisee
   */
  #choisir(liste, champLibre, valeur) {
    const existe = Array.from(liste.options)
      .some((option) => option.value !== 'autre' && option.textContent === valeur);

    if (existe) {
      liste.value = valeur;
      champLibre.value = '';
    } else {
      liste.value = 'autre';
      champLibre.value = valeur;
    }
  }

  /**
   * Affiche les champs libres seulement quand « Autre… » est choisi.
   */
  #majChampsLibres() {
    this.libreOuverture.hidden = this.selOuverture.value !== 'autre';
    this.libreCloture.hidden = this.selCloture.value !== 'autre';
  }

  /**
   * Lit la formule choisie, qu'elle vienne de la liste ou du champ libre.
   * @param {HTMLSelectElement} liste la liste deroulante
   * @param {HTMLInputElement} champLibre le champ de saisie libre
   * @param {string} defaut la valeur de repli
   * @returns {string} la formule
   */
  #formule(liste, champLibre, defaut) {
    if (liste.value !== 'autre') {
      return liste.value;
    }

    const saisie = champLibre.value.trim();
    return saisie.length > 0 ? saisie : defaut;
  }

  /**
   * Enregistre les regles.
   */
  #enregistrer() {
    const regles = {
      utiliserFormuleOuverture: this.caseOuverture.checked,
      formuleOuverture: this.#formule(this.selOuverture, this.libreOuverture, 'Bonjour,'),
      utiliserFormuleCloture: this.caseCloture.checked,
      formuleCloture: this.#formule(this.selCloture, this.libreCloture, 'Cordialement,'),
      reglesPersonnalisees: this.personnalisees.value.trim(),
    };

    const ok = ecrireRegles(regles);

    this.confirmation.textContent = ok
      ? '✓ Règles enregistrées.'
      : "Votre navigateur refuse le stockage local : les règles s'appliqueront "
        + 'à cette session seulement.';

    setTimeout(() => {
      this.confirmation.textContent = '';
    }, 3000);
  }
}
