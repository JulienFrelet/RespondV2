# Construction en plusieurs étapes : l'image finale ne contient que le
# résultat de la compilation, pas le SDK ni le code source. On passe d'environ
# 800 Mo à une centaine, et surtout le compilateur n'est pas embarqué dans ce
# qui tourne en production.

# ── Étape 1 : compilation ──────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS compilation
WORKDIR /source

# Les fichiers projet sont copiés seuls d'abord. Docker met en cache chaque
# étape : tant qu'aucune dépendance ne change, la restauration n'est pas
# refaite, même quand le code change. Une modification de code passe ainsi de
# plusieurs minutes à quelques secondes.
COPY Respond.slnx ./
COPY RespondMetier/RespondMetier.csproj RespondMetier/
COPY RespondData/RespondData.csproj RespondData/
COPY RespondAPI/RespondAPI.csproj RespondAPI/
COPY RespondTests/RespondTests.csproj RespondTests/
RUN dotnet restore

COPY . .

# Les tests tournent pendant la construction : une image ne doit pas pouvoir
# être produite à partir d'un code dont les tests échouent.
RUN dotnet test --configuration Release --no-restore

RUN dotnet publish RespondAPI/RespondAPI.csproj \
    --configuration Release \
    --no-restore \
    --output /application

# ── Étape 2 : exécution ────────────────────────────────────────────────────
# L'image d'exécution seule, sans SDK.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS execution
WORKDIR /application

# L'application tourne sous un compte sans privilège, déjà présent dans
# l'image. Un processus compromis qui tourne en root est un processus qui
# contrôle le conteneur entier.
USER $APP_UID

COPY --from=compilation /application .

# La chaîne de connexion par défaut vise un volume, pour qui lance l'image
# telle quelle avec SQLite. En production elle est remplacée par une chaîne
# PostgreSQL, et aucun volume n'est alors nécessaire : les clés qui signent
# les cookies de session sont rangées en base avec le reste des données.
ENV ConnectionStrings__Default="Data Source=/donnees/respond.db" \
    ASPNETCORE_HTTP_PORTS=8080

EXPOSE 8080

# La clé de démonstration n'est pas dans l'image : elle est fournie au
# démarrage par une variable d'environnement Respond__CleDemo. Une clé
# inscrite dans une image serait lisible par quiconque obtient cette image.

ENTRYPOINT ["dotnet", "RespondAPI.dll"]
