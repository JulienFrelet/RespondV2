/**
 * Vue des profils : liste, creation, edition, suppression.
 *
 * Les profils de demonstration sont affiches sans bouton d'action. Le serveur
 * refuse de toute facon de les modifier : masquer les boutons est une aide a
 * l'utilisateur, jamais la protection elle-meme.
 */
export class VueProfils {
  /**
   * @param {import('../service/serviceProfils.js').ServiceProfils} service
   * @param {Function} surChangement appele quand la liste change, pour que la
   *                                 vue Generer recharge son selecteur
   */
  constructor(service, surChangement) {
    this.service = service;
    this.surChangement = surChangement;
    this.idEnCours = null;

    this.liste = document.getElementById('liste-profils');
    this.erreur = document.getElementById('err-profils');
    this.formulaire = document.getElementById('formulaire-profil');
    this.titreFormulaire = document.getElementById('titre-formulaire-profil');
    this.erreurFormulaire = document.getElementById('err-formulaire-profil');

    this.btnNouveau = document.getElementById('btn-nouveau-profil');
    this.btnAnnuler = document.getElementById('btn-annuler-profil');
    this.btnEnregistrer = document.getElementById('btn-enregistrer-profil');
  }

  /**
   * Branche les evenements.
   */
  demarrer() {
    this.btnNouveau.addEventListener('click', () => this.#ouvrirFormulaire(null));
    this.btnAnnuler.addEventListener('click', () => this.#fermerFormulaire());
    this.btnEnregistrer.addEventListener('click', () => this.#enregistrer());
  }

  /**
   * Recharge et affiche la liste.
   * @returns {Promise<void>}
   */
  async rafraichir() {
    this.erreur.textContent = '';

    try {
      this.profils = await this.service.lister();
      this.#afficherListe();
    } catch (erreur) {
      this.erreur.textContent = erreur.message;
    }
  }

  /**
   * Construit les cartes de profils, sans jamais passer par innerHTML.
   */
  #afficherListe() {
    this.liste.replaceChildren();

    for (const profil of this.profils) {
      this.liste.appendChild(this.#carte(profil));
    }
  }

  /**
   * Construit la carte d'un profil.
   * @param {object} profil le profil a afficher
   * @returns {HTMLElement} la carte
   */
  #carte(profil) {
    const carte = document.createElement('article');
    carte.className = 'carte';

    const nom = document.createElement('h3');
    nom.textContent = profil.nom;
    carte.appendChild(nom);

    if (profil.estProfilDeDemonstration) {
      const etiquette = document.createElement('span');
      etiquette.className = 'etiquette';
      etiquette.textContent = 'Démonstration';
      carte.appendChild(etiquette);
    }

    const resume = document.createElement('p');
    resume.className = 'carte-resume';
    resume.textContent = profil.resume();
    carte.appendChild(resume);

    const detail = this.#detail(profil);
    const actions = document.createElement('div');
    actions.className = 'carte-actions';

    // Le detail se replie, parce qu'une liste de profils tous deplies
    // redeviendrait illisible. Un bouton plutot qu'un clic sur la carte
    // entiere : le clavier l'atteint, et rien ne se declenche par megarde en
    // visant le bouton Supprimer juste a cote.
    const bascule = this.#bouton(
      'Voir le détail',
      () => {
        const ouvert = detail.hidden;
        detail.hidden = !ouvert;
        bascule.setAttribute('aria-expanded', String(ouvert));
        bascule.textContent = ouvert ? 'Masquer le détail' : 'Voir le détail';
      });

    bascule.setAttribute('aria-expanded', 'false');
    actions.appendChild(bascule);

    if (profil.estProfilDeDemonstration) {
      const note = document.createElement('span');
      note.className = 'indice';
      note.textContent = 'Lecture seule';
      actions.appendChild(note);
    } else {
      actions.appendChild(this.#bouton('Modifier', () => this.#ouvrirFormulaire(profil)));
      actions.appendChild(
        this.#bouton('Supprimer', () => this.#supprimer(profil), 'danger'));
    }

    carte.appendChild(actions);
    carte.appendChild(detail);
    return carte;
  }

  /**
   * Construit le bloc deplie d'une carte, qui montre tout le contenu du
   * profil.
   *
   * Sans lui, le seul moyen de relire ce qu'on avait saisi etait d'ouvrir le
   * formulaire de modification, donc de risquer un enregistrement non voulu ;
   * et les profils de demonstration, en lecture seule, n'etaient consultables
   * par aucun moyen.
   * @param {object} profil le profil a detailler
   * @returns {HTMLElement} le bloc, masque au depart
   */
  #detail(profil) {
    const bloc = document.createElement('dl');
    bloc.className = 'carte-detail';
    bloc.hidden = true;

    const signature = profil.signature ?? {};
    const inclus = [
      signature.inclureNom === false ? null : signature.nom,
      signature.inclureService === false ? null : profil.service,
      signature.inclureEntreprise === false ? null : profil.entreprise,
      signature.inclureTelephone === false ? null : signature.telephone,
      signature.inclureEmail === false ? null : signature.email,
    ].filter((ligne) => typeof ligne === 'string' && ligne.length > 0);

    const lignes = [
      ['Service', profil.service],
      ['Entreprise', profil.entreprise],
      ['Domaines', profil.domaines],
      ['Description', profil.description],
      ['Ton', profil.ton],
      ['Langue', profil.langue],
      // La signature est montree telle qu'elle apparaitra dans la reponse,
      // cases decochees comprises : c'est le resultat qui interesse, pas la
      // liste des reglages qui y menent.
      ['Signature', inclus.join('\n')],
    ];

    for (const [intitule, valeur] of lignes) {
      if (!valeur) {
        continue;
      }

      const terme = document.createElement('dt');
      terme.textContent = intitule;
      bloc.appendChild(terme);

      const definition = document.createElement('dd');
      definition.textContent = valeur;
      bloc.appendChild(definition);
    }

    return bloc;
  }

  /**
   * Fabrique un bouton d'action de carte.
   * @param {string} libelle le texte du bouton
   * @param {Function} action ce que fait le clic
   * @param {string} modificateur classe supplementaire, optionnelle
   * @returns {HTMLButtonElement} le bouton
   */
  #bouton(libelle, action, modificateur = '') {
    const bouton = document.createElement('button');
    bouton.type = 'button';
    bouton.className = `bouton-secondaire petit ${modificateur}`.trim();
    bouton.textContent = libelle;
    bouton.addEventListener('click', action);
    return bouton;
  }

  /**
   * Ouvre le formulaire, vide pour une creation, rempli pour une edition.
   * @param {object|null} profil le profil a modifier, ou null
   */
  #ouvrirFormulaire(profil) {
    this.idEnCours = profil === null ? null : profil.id;
    this.erreurFormulaire.textContent = '';
    this.titreFormulaire.textContent = profil === null
      ? 'Nouveau profil'
      : `Modifier « ${profil.nom} »`;

    const complet = profil === null ? null : (profil.brut ?? profil);
    const signature = complet?.signature ?? {};

    this.#valeur('pf-nom', complet?.nom ?? '');
    this.#valeur('pf-service', complet?.service ?? '');
    this.#valeur('pf-entreprise', complet?.entreprise ?? '');
    this.#valeur('pf-domaines', complet?.domaines ?? '');
    this.#valeur('pf-description', complet?.description ?? '');
    this.#valeur('pf-ton', complet?.ton || 'professionnel et chaleureux');
    this.#valeur('pf-langue', complet?.langue || 'français');
    this.#valeur('pf-sig-nom', signature.nom ?? '');
    this.#valeur('pf-sig-tel', signature.telephone ?? '');
    this.#valeur('pf-sig-email', signature.email ?? '');

    this.#coche('pf-inc-nom', signature.inclureNom ?? true);
    this.#coche('pf-inc-service', signature.inclureService ?? true);
    this.#coche('pf-inc-entreprise', signature.inclureEntreprise ?? true);
    this.#coche('pf-inc-tel', signature.inclureTelephone ?? true);
    this.#coche('pf-inc-email', signature.inclureEmail ?? true);

    this.formulaire.hidden = false;
    this.formulaire.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
  }

  /**
   * Ferme le formulaire.
   */
  #fermerFormulaire() {
    this.formulaire.hidden = true;
    this.idEnCours = null;
  }

  /**
   * Ecrit une valeur dans un champ.
   * @param {string} id l'identifiant du champ
   * @param {string} valeur la valeur a ecrire
   */
  #valeur(id, valeur) {
    const champ = document.getElementById(id);

    // Donner a un <select> une valeur absente de ses options le laisse vide,
    // sans erreur. Le ton d'un profil se vidait ainsi a l'ouverture du
    // formulaire, et un enregistrement l'aurait efface sans que personne ne
    // l'ait demande. La valeur reelle est donc ajoutee a la liste quand elle
    // n'y figure pas : mieux vaut une option inattendue qu'une donnee perdue.
    if (champ.tagName === 'SELECT' && valeur !== '') {
      const connue = [...champ.options].some((option) => option.value === valeur);

      if (!connue) {
        const option = document.createElement('option');
        option.value = valeur;
        option.textContent = valeur;
        champ.appendChild(option);
      }
    }

    champ.value = valeur;
  }

  /**
   * Coche ou decoche une case.
   * @param {string} id l'identifiant de la case
   * @param {boolean} etat l'etat voulu
   */
  #coche(id, etat) {
    document.getElementById(id).checked = etat;
  }

  /**
   * Lit le formulaire.
   * @returns {object} le profil saisi
   */
  #lireFormulaire() {
    const lire = (id) => document.getElementById(id).value.trim();
    const coche = (id) => document.getElementById(id).checked;

    return {
      nom: lire('pf-nom'),
      service: lire('pf-service'),
      entreprise: lire('pf-entreprise'),
      domaines: lire('pf-domaines'),
      description: lire('pf-description'),
      ton: lire('pf-ton'),
      langue: lire('pf-langue'),
      signature: {
        nom: lire('pf-sig-nom'),
        telephone: lire('pf-sig-tel'),
        email: lire('pf-sig-email'),
        inclureNom: coche('pf-inc-nom'),
        inclureService: coche('pf-inc-service'),
        inclureEntreprise: coche('pf-inc-entreprise'),
        inclureTelephone: coche('pf-inc-tel'),
        inclureEmail: coche('pf-inc-email'),
      },
    };
  }

  /**
   * Enregistre le profil saisi.
   * @returns {Promise<void>}
   */
  async #enregistrer() {
    const profil = this.#lireFormulaire();
    this.erreurFormulaire.textContent = '';

    // Validation cote client pour un retour immediat. Le serveur revalide de
    // toute facon : ce controle-ci est du confort, pas une protection.
    if (profil.nom.length === 0 || profil.service.length === 0
        || profil.entreprise.length === 0) {
      this.erreurFormulaire.textContent =
        'Le nom, le service et l\'entreprise sont obligatoires.';
      return;
    }

    const libelle = this.btnEnregistrer.textContent;
    this.btnEnregistrer.disabled = true;
    this.btnEnregistrer.textContent = 'Enregistrement…';

    try {
      if (this.idEnCours === null) {
        await this.service.creer(profil);
      } else {
        await this.service.modifier(this.idEnCours, profil);
      }

      this.#fermerFormulaire();
      await this.rafraichir();
      this.surChangement();
    } catch (erreur) {
      this.erreurFormulaire.textContent = erreur.message;
    } finally {
      // Quoi qu'il arrive, le bouton redevient cliquable.
      this.btnEnregistrer.disabled = false;
      this.btnEnregistrer.textContent = libelle;
    }
  }

  /**
   * Supprime un profil, apres confirmation.
   * @param {object} profil le profil a supprimer
   * @returns {Promise<void>}
   */
  async #supprimer(profil) {
    if (!window.confirm(`Supprimer le profil « ${profil.nom} » ?`)) {
      return;
    }

    try {
      await this.service.supprimer(profil.id);
      await this.rafraichir();
      this.surChangement();
    } catch (erreur) {
      this.erreur.textContent = erreur.message;
    }
  }
}
