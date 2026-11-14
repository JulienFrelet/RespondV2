/**
 * Vue de generation : relie le formulaire au service.
 *
 * Regle absolue heritee de la version Electron : aucune operation asynchrone
 * ne doit pouvoir laisser l'interface bloquee. Chaque appel est entoure d'un
 * try / catch / finally, et l'etat du bouton est restaure dans le finally.
 * Un bouton coince sur « Generation... » sans message d'erreur est le pire
 * defaut possible, parce que l'utilisateur n'a rien a quoi se raccrocher.
 */
export class VueGenerer {
  /**
   * @param {import('../service/serviceReponses.js').ServiceReponses} service
   */
  constructor(service) {
    this.service = service;
    this.profils = [];

    this.selProfil = document.getElementById('sel-profil');
    this.indiceProfil = document.getElementById('indice-profil');
    this.txtEmail = document.getElementById('txt-email');
    this.txtContexte = document.getElementById('txt-contexte');
    this.compteurEmail = document.getElementById('compteur-email');
    this.btnGenerer = document.getElementById('btn-generer');
    this.errDemande = document.getElementById('err-demande');

    this.attente = document.getElementById('attente');
    this.resultat = document.getElementById('resultat');
    this.txtObjet = document.getElementById('txt-objet');
    this.txtCorps = document.getElementById('txt-corps');
    this.btnCopier = document.getElementById('btn-copier');
    this.btnRegenerer = document.getElementById('btn-regenerer');
    this.errResultat = document.getElementById('err-resultat');
  }

  /**
   * Branche les evenements et charge les profils.
   * @returns {Promise<void>}
   */
  async demarrer() {
    this.selProfil.addEventListener('change', () => this.#afficherResumeProfil());
    this.txtEmail.addEventListener('input', () => this.#majCompteur());
    this.btnGenerer.addEventListener('click', () => this.#generer());
    this.btnRegenerer.addEventListener('click', () => this.#generer());
    this.btnCopier.addEventListener('click', () => this.#copier());

    document.addEventListener('keydown', (evenement) => {
      if ((evenement.ctrlKey || evenement.metaKey) && evenement.key === 'Enter') {
        this.#generer();
      }
    });

    await this.#chargerProfils();
  }

  /**
   * Charge les profils et remplit le selecteur.
   * @returns {Promise<void>}
   */
  async #chargerProfils() {
    try {
      this.profils = await this.service.listerProfils();

      this.selProfil.replaceChildren();

      for (const profil of this.profils) {
        const option = document.createElement('option');
        option.value = String(profil.id);
        option.textContent = profil.nom;
        this.selProfil.appendChild(option);
      }

      this.#afficherResumeProfil();
    } catch (erreur) {
      this.errDemande.textContent = erreur.message;
    }
  }

  /**
   * Affiche le resume du profil choisi.
   */
  #afficherResumeProfil() {
    const profil = this.profils.find(
      (candidat) => String(candidat.id) === this.selProfil.value,
    );

    this.indiceProfil.textContent = profil ? profil.resume() : '';
  }

  /**
   * Met a jour le compteur de caracteres.
   */
  #majCompteur() {
    this.compteurEmail.textContent = String(this.txtEmail.value.length);
  }

  /**
   * Lit la longueur choisie dans les boutons radio.
   * @returns {string} Courte ou Complete
   */
  #longueurChoisie() {
    const coche = document.querySelector('input[name="longueur"]:checked');
    return coche ? coche.value : 'Courte';
  }

  /**
   * Lance une generation et affiche le resultat.
   * @returns {Promise<void>}
   */
  async #generer() {
    this.errDemande.textContent = '';
    this.errResultat.textContent = '';

    if (this.txtEmail.value.trim().length === 0) {
      this.errDemande.textContent = 'Collez le message auquel vous souhaitez repondre.';
      return;
    }

    if (this.selProfil.value.length === 0) {
      this.errDemande.textContent = 'Aucun profil disponible.';
      return;
    }

    const libelleOrigine = this.btnGenerer.textContent;
    this.btnGenerer.disabled = true;
    this.btnGenerer.textContent = 'Generation...';

    try {
      const reponse = await this.service.generer({
        profilId: Number(this.selProfil.value),
        emailRecu: this.txtEmail.value,
        contexte: this.txtContexte.value,
        longueur: this.#longueurChoisie(),
      });

      this.#afficherResultat(reponse);
    } catch (erreur) {
      this.errResultat.textContent = erreur.message;
    } finally {
      // Quoi qu'il arrive, le bouton redevient cliquable.
      this.btnGenerer.disabled = false;
      this.btnGenerer.textContent = libelleOrigine;
    }
  }

  /**
   * Affiche la reponse generee.
   * @param {{objet: string, corps: string}} reponse la reponse de l'API
   */
  #afficherResultat(reponse) {
    this.txtObjet.value = reponse.objet;
    this.txtCorps.value = reponse.corps;
    this.attente.hidden = true;
    this.resultat.hidden = false;
  }

  /**
   * Copie le corps dans le presse-papier.
   * @returns {Promise<void>}
   */
  async #copier() {
    try {
      await navigator.clipboard.writeText(this.txtCorps.value);
      this.btnCopier.textContent = 'Copie';
      this.btnCopier.classList.add('confirme');

      setTimeout(() => {
        this.btnCopier.textContent = 'Copier';
        this.btnCopier.classList.remove('confirme');
      }, 1600);
    } catch {
      // Presse-papier refuse par le navigateur : le texte reste selectionnable.
      this.errResultat.textContent = 'Copie refusee par le navigateur. Selectionnez le texte.';
    }
  }
}
