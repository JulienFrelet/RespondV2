# Respond

Générateur de réponses à des emails professionnels, propulsé par une API d'IA.
Application web ASP.NET Core avec un client JavaScript, architecture en quatre
projets.

> 🚧 **En construction.** Une démonstration en ligne arrivera quand le
> déploiement sera en place.

## Ce que ça fait

Vous choisissez un profil d'entreprise — métier, ton, signature — vous collez
l'email auquel répondre, et l'application rédige une réponse prête à relire.
Rien n'est jamais envoyé automatiquement : vous relisez toujours avant.

## Pile technique

| Couche | Technologie |
|---|---|
| Langage | C# / .NET 10 |
| API | ASP.NET Core, contrôleurs |
| Persistance | SQLite via Entity Framework Core |
| Client | JavaScript natif, structure MVC, aucune dépendance |
| Tests | xUnit, doublures écrites à la main |

## Architecture

```
RespondMetier/   ← métier pur, ne référence aucun projet
RespondData/     ← persistance, implémente les interfaces du métier
RespondAPI/      ← contrôleurs, clients des fournisseurs, client web
RespondTests/    ← tests unitaires et tests d'architecture
```

La règle centrale : **`RespondMetier` ne référence rien.** Il déclare ce dont il
a besoin sous forme d'interfaces, les autres couches les implémentent. C'est ce
qui l'a laissé intact le jour où la présentation est passée d'une application
de bureau WPF à une API web : un seul projet sur quatre a changé.

## Sécurité

Chaque point ci-dessous est une décision, pas un réglage par défaut.

- **Aucun compte, et pourtant des données cloisonnées.** Un cookie de session
  anonyme sépare les visiteurs. Sa valeur est signée cryptographiquement : un
  identifiant brut serait forgeable, il suffirait de recopier celui d'un autre
  pour lire son historique. Cookie `HttpOnly`, `SameSite=Lax`, `Secure` hors
  développement, identifiant de 32 octets tirés du générateur cryptographique.
- **Injection de prompt.** L'email reçu est une donnée non fiable. Instructions
  et message voyagent comme deux messages distincts, jamais concaténés, et le
  modèle est explicitement instruit de ne pas obéir au contenu analysé. Aucune
  de ces défenses n'étant absolue, rien n'est jamais envoyé automatiquement.
- **Clés API.** Jamais dans le dépôt, jamais dans un log, jamais dans un
  message d'erreur. `CleApi.ToString()` est redéfini pour ne pas pouvoir
  révéler le secret par accident. Un test le vérifie.
- **Limitation de débit** par adresse sur la génération, et plafond sur la
  taille des messages : la démonstration tourne sur une clé partagée.
- **En-têtes** : politique de sécurité du contenu stricte envoyée en en-tête
  HTTP et non en balise `meta`, `nosniff`, `Referrer-Policy`, HSTS en
  production, en-tête `Server` supprimé.
- **Aucune ressource tierce.** Pas de polices distantes, donc aucune adresse IP
  de visiteur transmise à qui que ce soit.
- **Aucune dépendance JavaScript.** Le client est écrit à la main.

## Démarrage

Prérequis : [SDK .NET 10](https://dotnet.microsoft.com/download).

```
dotnet restore
dotnet test
dotnet run --project RespondAPI
```

Pour activer le mode démonstration, enregistrez une clé
[Groq](https://console.groq.com/keys) hors du dépôt :

```
dotnet user-secrets set "Respond:CleDemo" "votre_cle" --project RespondAPI
```

## Déploiement

L'application se construit en image Docker et attend deux volumes persistants.

| Variable | Rôle |
|---|---|
| `Respond__CleDemo` | Clé Groq du mode démonstration. **Jamais dans l'image ni dans le dépôt.** |
| `ConnectionStrings__Default` | Chemin de la base SQLite, sur un volume persistant |
| `Respond__DossierCles` | Dossier des clés signant les cookies de session |

```
docker build -t respond .
docker run -p 8080:8080   -e Respond__CleDemo="votre_cle"   -v respond-donnees:/donnees   -v respond-cles:/cles   respond
```

**Les deux volumes sont obligatoires.** Sans `/donnees`, la base disparaît à
chaque redéploiement. Sans `/cles`, les clés de signature sont régénérées,
tous les cookies de session deviennent invalides et chaque visiteur perd son
historique. Le dossier des clés est un secret : qui le lit peut forger des
cookies de session.

## Intégration continue

Trois contrôles tournent à chaque poussée et chaque pull request :

- compilation et tests sur Linux, avec rapport de couverture ;
- recherche de paquets vulnérables, dépendances transitives comprises ;
- recherche de secrets avec `gitleaks` sur **tout l'historique**, pas seulement
  sur le dernier commit — une clé supprimée plus tard reste lisible dans le
  commit qui l'a introduite. Cette analyse tourne aussi chaque lundi, parce
  qu'une clé publiée aujourd'hui peut n'être reconnue comme telle que demain.

L'image Docker est construite en CI, tests inclus : aucune image ne peut être
produite à partir d'un code dont les tests échouent.

## Méthode de travail

Projet développé avec l'assistance d'un agent IA. Les choix d'architecture, les
conventions de code et les arbitrages techniques sont documentés dans les
messages de commit, qui expliquent systématiquement le *pourquoi* d'une
décision et pas seulement ce qu'elle change.

## Licence

MIT
